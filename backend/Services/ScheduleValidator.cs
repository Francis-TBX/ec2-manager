using Ec2Manager.Api.DTOs;

namespace Ec2Manager.Api.Services;

public static class ScheduleValidator
{
    private static readonly HashSet<string> ValidRecurrenceTypes = new(StringComparer.OrdinalIgnoreCase) { "None", "Daily", "Weekly" };
    private static readonly HashSet<string> ValidDays = new(StringComparer.OrdinalIgnoreCase) { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
    private static readonly HashSet<string> ValidActions = new(StringComparer.OrdinalIgnoreCase) { "Start", "Stop" };

    public static List<string> Validate(ScheduleCreateRequest req)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(req.Name))
            errors.Add("Name is required.");

        if (string.IsNullOrWhiteSpace(req.AccountKey))
            errors.Add("AccountKey is required.");

        if (req.Regions is null || req.Regions.Count == 0)
            errors.Add("At least one region is required.");

        if (string.IsNullOrWhiteSpace(req.Action) || !ValidActions.Contains(req.Action))
            errors.Add("Action must be 'Start' or 'Stop'.");

        if (req.ValidTo.HasValue && req.ValidTo.Value < req.ValidFrom)
            errors.Add("ValidTo must be >= ValidFrom.");

        var hasCron = !string.IsNullOrWhiteSpace(req.CronExpression);
        var hasRecurrence = !string.IsNullOrWhiteSpace(req.RecurrenceType) && !req.RecurrenceType!.Equals("None", StringComparison.OrdinalIgnoreCase);

        if (hasCron && hasRecurrence)
            errors.Add("CronExpression and windowed recurrence fields are mutually exclusive.");

        if (hasCron)
        {
            try
            {
                Cronos.CronExpression.Parse(req.CronExpression);
            }
            catch (Exception)
            {
                errors.Add("CronExpression is not a valid cron string.");
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(req.RecurrenceType) && !ValidRecurrenceTypes.Contains(req.RecurrenceType))
                errors.Add("RecurrenceType must be 'None', 'Daily', or 'Weekly'.");

            if (hasRecurrence)
            {
                if (!req.TimeOfDay.HasValue)
                    errors.Add("TimeOfDay is required for Daily or Weekly recurrence.");

                if (req.RecurrenceType!.Equals("Weekly", StringComparison.OrdinalIgnoreCase))
                {
                    if (req.DaysOfWeek is null || req.DaysOfWeek.Count == 0)
                        errors.Add("DaysOfWeek is required for Weekly recurrence.");
                    else if (req.DaysOfWeek.Any(d => !ValidDays.Contains(d)))
                        errors.Add("DaysOfWeek must contain valid values (Mon..Sun).");
                }
            }
        }

        return errors;
    }
}
