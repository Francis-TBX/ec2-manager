namespace Ec2Manager.Api.DTOs;

public record ScheduleDryRunPreview(DateTimeOffset FireTime, List<string> AffectedInstanceIds, List<SkippedInstanceDto> Skipped);
public record ScheduleDryRunResponse(int ScheduleId, string Action, List<ScheduleDryRunPreview> Previews);
