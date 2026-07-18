namespace PatientReminders.Api.Models;

public enum ReminderStatus { Open, Done }
public enum Recurrence { None, Daily, Weekly }

public class Reminder
{
    public Guid Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    /// <summary>Sensitive field — never log, never include in error responses.</summary>
    public string? Notes { get; set; }

    public DateTime DueDate { get; set; }
    public ReminderStatus Status { get; set; } = ReminderStatus.Open;
    public Recurrence Recurrence { get; set; } = Recurrence.None;
    public DateTime CreatedAt { get; set; }
}
