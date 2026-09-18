namespace Ec2Manager.Api.DTOs;

public record AuditLogDto(
    int Id,
    DateTime Timestamp,
    string? UserName,
    string ActionType,
    string AccountKey,
    string Region,
    List<string> InstanceIds,
    bool DryRun,
    string Result,
    string Message,
    string? Error
);

public record PagedResultDto<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);
