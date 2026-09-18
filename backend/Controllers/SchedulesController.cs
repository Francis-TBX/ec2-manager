using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ec2Manager.Api.Data;
using Ec2Manager.Api.DTOs;
using Ec2Manager.Api.Models;
using Ec2Manager.Api.Services;

namespace Ec2Manager.Api.Controllers;

[ApiController]
[Route("schedules")]
[Authorize]
public class SchedulesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CloudServiceClient _cloud;

    public SchedulesController(AppDbContext db, CloudServiceClient cloud)
    {
        _db = db;
        _cloud = cloud;
    }

    private static ScheduleResponse ToResponse(Schedule s) => new(
        s.Id, s.Name, s.AccountKey,
        JsonSerializer.Deserialize<List<string>>(s.RegionsJson) ?? new(),
        JsonSerializer.Deserialize<List<string>>(s.InstanceIdsJson) ?? new(),
        s.Action, s.ValidFrom, s.ValidTo, s.RecurrenceType,
        s.DaysOfWeekJson is null ? null : JsonSerializer.Deserialize<List<string>>(s.DaysOfWeekJson),
        s.TimeOfDay, s.CronExpression, s.Enabled, s.CreatedAt, s.UpdatedAt, s.CreatedBy
    );

    [HttpGet]
    public async Task<ActionResult<List<ScheduleResponse>>> List()
    {
        var schedules = await _db.Schedules.OrderByDescending(s => s.CreatedAt).ToListAsync();
        return Ok(schedules.Select(ToResponse).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ScheduleResponse>> Get(int id)
    {
        var s = await _db.Schedules.FindAsync(id);
        if (s is null) return NotFound();
        return Ok(ToResponse(s));
    }

    [HttpPost]
    public async Task<ActionResult<ScheduleResponse>> Create(ScheduleCreateRequest req)
    {
        var errors = ScheduleValidator.Validate(req);
        if (errors.Count > 0)
            return BadRequest(new { errors });

        var schedule = new Schedule
        {
            Name = req.Name,
            AccountKey = req.AccountKey,
            RegionsJson = JsonSerializer.Serialize(req.Regions),
            InstanceIdsJson = JsonSerializer.Serialize(req.InstanceIds ?? new List<string>()),
            Action = req.Action,
            ValidFrom = req.ValidFrom,
            ValidTo = req.ValidTo,
            RecurrenceType = req.RecurrenceType,
            DaysOfWeekJson = req.DaysOfWeek is null ? null : JsonSerializer.Serialize(req.DaysOfWeek),
            TimeOfDay = req.TimeOfDay,
            CronExpression = req.CronExpression,
            Enabled = req.Enabled,
            CreatedBy = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "unknown",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Schedules.Add(schedule);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = schedule.Id }, ToResponse(schedule));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ScheduleResponse>> Update(int id, ScheduleCreateRequest req)
    {
        var schedule = await _db.Schedules.FindAsync(id);
        if (schedule is null) return NotFound();

        var errors = ScheduleValidator.Validate(req);
        if (errors.Count > 0)
            return BadRequest(new { errors });

        schedule.Name = req.Name;
        schedule.AccountKey = req.AccountKey;
        schedule.RegionsJson = JsonSerializer.Serialize(req.Regions);
        schedule.InstanceIdsJson = JsonSerializer.Serialize(req.InstanceIds ?? new List<string>());
        schedule.Action = req.Action;
        schedule.ValidFrom = req.ValidFrom;
        schedule.ValidTo = req.ValidTo;
        schedule.RecurrenceType = req.RecurrenceType;
        schedule.DaysOfWeekJson = req.DaysOfWeek is null ? null : JsonSerializer.Serialize(req.DaysOfWeek);
        schedule.TimeOfDay = req.TimeOfDay;
        schedule.CronExpression = req.CronExpression;
        schedule.Enabled = req.Enabled;
        schedule.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(schedule));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var schedule = await _db.Schedules.FindAsync(id);
        if (schedule is null) return NotFound();

        _db.Schedules.Remove(schedule);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/enable")]
    public async Task<IActionResult> Enable(int id)
    {
        var schedule = await _db.Schedules.FindAsync(id);
        if (schedule is null) return NotFound();
        schedule.Enabled = true;
        schedule.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(ToResponse(schedule));
    }

    [HttpPost("{id}/disable")]
    public async Task<IActionResult> Disable(int id)
    {
        var schedule = await _db.Schedules.FindAsync(id);
        if (schedule is null) return NotFound();
        schedule.Enabled = false;
        schedule.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(ToResponse(schedule));
    }

    [HttpPost("{id}/dryRun")]
    public async Task<ActionResult<ScheduleDryRunResponse>> DryRun(int id, [FromQuery] int count = 3)
    {
        var schedule = await _db.Schedules.FindAsync(id);
        if (schedule is null) return NotFound();

        var fireTimes = ScheduleRunner.GetNextFireTimes(schedule, DateTimeOffset.UtcNow, count);
        var regions = JsonSerializer.Deserialize<List<string>>(schedule.RegionsJson) ?? new();
        var configuredIds = JsonSerializer.Deserialize<List<string>>(schedule.InstanceIdsJson) ?? new();

        var previews = new List<ScheduleDryRunPreview>();

        foreach (var fireTime in fireTimes)
        {
            var affected = new List<string>();
            var skipped = new List<SkippedInstanceDto>();

            foreach (var region in regions)
            {
                try
                {
                    var raw = await _cloud.ListInstancesRaw(schedule.AccountKey, region, null, null);
                    var candidates = configuredIds.Count > 0
                        ? raw.Where(i => configuredIds.Contains(i.GetProperty("instanceId").GetString()))
                        : raw;

                    foreach (var inst in candidates)
                    {
                        var instanceId = inst.GetProperty("instanceId").GetString()!;
                        var tags = inst.GetProperty("tags");
                        var dnsOk = false;
                        foreach (var tag in tags.EnumerateArray())
                        {
                            if (tag.GetProperty("Key").GetString() == "DNS" &&
                                (tag.GetProperty("Value").GetString() ?? "").Equals("Yes", StringComparison.OrdinalIgnoreCase))
                            {
                                dnsOk = true;
                                break;
                            }
                        }

                        if (dnsOk) affected.Add(instanceId);
                        else skipped.Add(new SkippedInstanceDto(instanceId, "DNS tag missing or not Yes"));
                    }
                }
                catch (Exception ex)
                {
                    skipped.Add(new SkippedInstanceDto($"region:{region}", $"Failed to query region: {ex.Message}"));
                }
            }

            previews.Add(new ScheduleDryRunPreview(fireTime, affected, skipped));
        }

        return Ok(new ScheduleDryRunResponse(schedule.Id, schedule.Action, previews));
    }
}
