namespace PatientReminders.Api.Models;

public enum EventType { Created, Completed, NextOccurrenceGenerated }

public class ReminderEvent
{
    public Guid Id { get; set; }
    public Guid ReminderId { get; set; }
    public EventType Type { get; set; }
    public DateTime OccurredAt { get; set; }

    /// <summary>Optional context (e.g. next occurrence id). Never contains sensitive data.</summary>
    public string? Metadata { get; set; }
}
