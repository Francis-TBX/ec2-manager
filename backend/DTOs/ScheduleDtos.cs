namespace Ec2Manager.Api.DTOs;

public record ScheduleCreateRequest(
    string Name,
    string AccountKey,
    List<string> Regions,
    List<string>? InstanceIds,
    string Action, // "Start" or "Stop"
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    string? RecurrenceType, // "None", "Daily", "Weekly"
    List<string>? DaysOfWeek,
    TimeSpan? TimeOfDay,
    string? CronExpression,
    bool Enabled
);

public record ScheduleResponse(
    int Id,
    string Name,
    string AccountKey,
    List<string> Regions,
    List<string> InstanceIds,
    string Action,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo,
    string? RecurrenceType,
    List<string>? DaysOfWeek,
    TimeSpan? TimeOfDay,
    string? CronExpression,
    bool Enabled,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string CreatedBy
);
