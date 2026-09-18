using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ec2Manager.Api.Data;
using Ec2Manager.Api.DTOs;
using Ec2Manager.Api.Models;
using Ec2Manager.Api.Services;

namespace Ec2Manager.Api.Controllers;

[ApiController]
[Route("instances")]
[Authorize]
public class InstanceActionsController : ControllerBase
{
    private readonly CloudServiceClient _cloud;
    private readonly AppDbContext _db;

    public InstanceActionsController(CloudServiceClient cloud, AppDbContext db)
    {
        _cloud = cloud;
        _db = db;
    }

    private static List<SkippedInstanceDto> ExtractSkipped(JsonElement result, string skipField)
    {
        if (!result.TryGetProperty(skipField, out var skipArr))
            return new List<SkippedInstanceDto>();

        return skipArr.EnumerateArray()
            .Select(s => new SkippedInstanceDto(
                s.GetProperty("instanceId").GetString()!,
                s.GetProperty("reason").GetString()!))
            .ToList();
    }

    private static List<string> ExtractStringList(JsonElement result, string field)
    {
        if (!result.TryGetProperty(field, out var arr))
            return new List<string>();
        return arr.EnumerateArray().Select(x => x.GetString()!).ToList();
    }

    private async Task LogAction(string actionType, string accountKey, string region, List<string> instanceIds, bool dryRun, string result, string message, string? error)
    {
        var log = new AuditLog
        {
            UserName = User.Identity?.Name ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value,
            ActionType = actionType,
            AccountKey = accountKey,
            Region = region,
            InstanceIdsJson = JsonSerializer.Serialize(instanceIds),
            DryRun = dryRun,
            Result = result,
            Message = message,
            Error = error,
        };
        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    [HttpPost("start")]
    public async Task<ActionResult<InstanceActionResultDto>> Start(InstanceActionRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.AccountKey) || string.IsNullOrWhiteSpace(req.Region) || req.InstanceIds.Count == 0)
            return BadRequest("accountKey, region, and instanceIds are required.");

        var actionType = req.DryRun ? "ManualStart" : "ManualStart";
        JsonElement resultJson;
        try
        {
            resultJson = await _cloud.StartInstances(req.AccountKey, req.Region, req.InstanceIds, req.DryRun);
        }
        catch (Exception ex)
        {
            await LogAction(actionType, req.AccountKey, req.Region, req.InstanceIds, req.DryRun, "Failed", "Cloud service call failed.", ex.Message);
            return StatusCode(502, "Failed to reach cloud service.");
        }

        var affected = ExtractStringList(resultJson, req.DryRun ? "wouldStart" : "started");
        var skipped = ExtractSkipped(resultJson, req.DryRun ? "wouldSkip" : "skipped");
        var errors = ExtractStringList(resultJson, "errors");

        var overallResult = errors.Count > 0 ? (affected.Count > 0 ? "Partial" : "Failed") : "Success";
        var message = req.DryRun
            ? $"Dry run: would start {affected.Count}, skip {skipped.Count}."
            : $"Started {affected.Count}, skipped {skipped.Count}.";

        await LogAction(actionType, req.AccountKey, req.Region, req.InstanceIds, req.DryRun, overallResult, message, errors.Count > 0 ? string.Join("; ", errors) : null);

        return Ok(new InstanceActionResultDto(affected, skipped, errors, req.DryRun));
    }

    [HttpPost("stop")]
    public async Task<ActionResult<InstanceActionResultDto>> Stop(InstanceActionRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.AccountKey) || string.IsNullOrWhiteSpace(req.Region) || req.InstanceIds.Count == 0)
            return BadRequest("accountKey, region, and instanceIds are required.");

        var actionType = "ManualStop";
        JsonElement resultJson;
        try
        {
            resultJson = await _cloud.StopInstances(req.AccountKey, req.Region, req.InstanceIds, req.DryRun);
        }
        catch (Exception ex)
        {
            await LogAction(actionType, req.AccountKey, req.Region, req.InstanceIds, req.DryRun, "Failed", "Cloud service call failed.", ex.Message);
            return StatusCode(502, "Failed to reach cloud service.");
        }

        var affected = ExtractStringList(resultJson, req.DryRun ? "wouldStop" : "stopped");
        var skipped = ExtractSkipped(resultJson, req.DryRun ? "wouldSkip" : "skipped");
        var errors = ExtractStringList(resultJson, "errors");

        var overallResult = errors.Count > 0 ? (affected.Count > 0 ? "Partial" : "Failed") : "Success";
        var message = req.DryRun
            ? $"Dry run: would stop {affected.Count}, skip {skipped.Count}."
            : $"Stopped {affected.Count}, skipped {skipped.Count}.";

        await LogAction(actionType, req.AccountKey, req.Region, req.InstanceIds, req.DryRun, overallResult, message, errors.Count > 0 ? string.Join("; ", errors) : null);

        return Ok(new InstanceActionResultDto(affected, skipped, errors, req.DryRun));
    }
}
