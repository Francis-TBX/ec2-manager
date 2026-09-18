namespace Ec2Manager.Api.Models;

public class AuditLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string ActionType { get; set; } = ""; // ManualStart, ManualStop, ScheduleStart, ScheduleStop
    public string AccountKey { get; set; } = "";
    public string Region { get; set; } = "";
    public string InstanceIdsJson { get; set; } = "[]";
    public bool DryRun { get; set; }
    public string Result { get; set; } = ""; // Success, Failed, Partial
    public string Message { get; set; } = "";
    public string? Error { get; set; }
}
