namespace Ec2Manager.Api.DTOs;

public record InstanceActionRequest(string AccountKey, string Region, List<string> InstanceIds, bool DryRun);

public record SkippedInstanceDto(string InstanceId, string Reason);

public record InstanceActionResultDto(
    List<string> Affected,      // wouldStart/wouldStop or started/stopped
    List<SkippedInstanceDto> Skipped,
    List<string> Errors,
    bool DryRun
);
