using System.Text.Json;
using Ec2Manager.Api.Data;
using Ec2Manager.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Ec2Manager.Api.Services;

public class ScheduleExecutorService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ScheduleExecutorService> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan FireTolerance = TimeSpan.FromSeconds(15);

    public ScheduleExecutorService(IServiceProvider services, ILogger<ScheduleExecutorService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnce(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while running schedule executor tick.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task RunOnce(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cloud = scope.ServiceProvider.GetRequiredService<CloudServiceClient>();

        var now = DateTimeOffset.UtcNow;
        var enabledSchedules = await db.Schedules.Where(s => s.Enabled).ToListAsync(ct);

        foreach (var schedule in enabledSchedules)
        {
            if (schedule.ValidFrom.HasValue && now < schedule.ValidFrom.Value) continue;
            if (schedule.ValidTo.HasValue && now > schedule.ValidTo.Value) continue;

            if (!ScheduleRunner.ShouldFireNow(schedule, now, FireTolerance)) continue;

            await FireSchedule(schedule, db, cloud);
        }
    }

    private async Task FireSchedule(Schedule schedule, AppDbContext db, CloudServiceClient cloud)
    {
        var regions = JsonSerializer.Deserialize<List<string>>(schedule.RegionsJson) ?? new();
        var configuredIds = JsonSerializer.Deserialize<List<string>>(schedule.InstanceIdsJson) ?? new();
        var actionType = schedule.Action.Equals("Start", StringComparison.OrdinalIgnoreCase) ? "ScheduleStart" : "ScheduleStop";

        foreach (var region in regions)
        {
            List<string> targetIds;
            try
            {
                if (configuredIds.Count > 0)
                {
                    targetIds = configuredIds;
                }
                else
                {
                    var raw = await cloud.ListInstancesRaw(schedule.AccountKey, region, null, null);
                    targetIds = raw.Select(i => i.GetProperty("instanceId").GetString()!).ToList();
                }

                if (targetIds.Count == 0) continue;

                JsonElement result = schedule.Action.Equals("Start", StringComparison.OrdinalIgnoreCase)
                    ? await cloud.StartInstances(schedule.AccountKey, region, targetIds, false)
                    : await cloud.StopInstances(schedule.AccountKey, region, targetIds, false);

                var affectedField = schedule.Action.Equals("Start", StringComparison.OrdinalIgnoreCase) ? "started" : "stopped";
                var affected = result.TryGetProperty(affectedField, out var a) ? a.GetArrayLength() : 0;
                var skipped = result.TryGetProperty("skipped", out var sk) ? sk.GetArrayLength() : 0;
                var errors = result.TryGetProperty("errors", out var e) ? e.GetArrayLength() : 0;

                var overallResult = errors > 0 ? (affected > 0 ? "Partial" : "Failed") : "Success";

                db.AuditLogs.Add(new AuditLog
                {
                    UserName = $"schedule:{schedule.Name}",
                    ActionType = actionType,
                    AccountKey = schedule.AccountKey,
                    Region = region,
                    InstanceIdsJson = JsonSerializer.Serialize(targetIds),
                    DryRun = false,
                    Result = overallResult,
                    Message = $"Scheduled {schedule.Action.ToLower()}: {affected} affected, {skipped} skipped.",
                    Error = null,
                });
            }
            catch (Exception ex)
            {
                db.AuditLogs.Add(new AuditLog
                {
                    UserName = $"schedule:{schedule.Name}",
                    ActionType = actionType,
                    AccountKey = schedule.AccountKey,
                    Region = region,
                    InstanceIdsJson = JsonSerializer.Serialize(configuredIds),
                    DryRun = false,
                    Result = "Failed",
                    Message = "Scheduled execution failed.",
                    Error = ex.Message,
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
