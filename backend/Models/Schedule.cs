using System.ComponentModel.DataAnnotations.Schema;

namespace Ec2Manager.Api.Models;

public class Schedule
{
    public int Id { get; set; }

    public string Name { get; set; } = "";
    public string AccountKey { get; set; } = "";

    [Column(TypeName = "json")]
    public string RegionsJson { get; set; } = "[]";

    [Column(TypeName = "json")]
    public string InstanceIdsJson { get; set; } = "[]";

    public string Action { get; set; } = "Start"; // "Start" or "Stop"

    public DateTimeOffset? ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }

    public string? RecurrenceType { get; set; } // "None", "Daily", "Weekly"

    [Column(TypeName = "json")]
    public string? DaysOfWeekJson { get; set; }

    public TimeSpan? TimeOfDay { get; set; }

    public string? CronExpression { get; set; }

    public bool Enabled { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "";
}
