using PatientReminders.Api.Models;

namespace PatientReminders.Api.Services;

public class ReminderService(ReminderStore store, ILogger<ReminderService> logger)
{
    public Reminder Create(CreateReminderRequest req)
    {
        var reminder = new Reminder
        {
            PatientName = req.PatientName,
            Title       = req.Title,
            Notes       = req.Notes,   // sensitive — never logged below
            DueDate     = req.DueDate,
            Recurrence  = req.Recurrence,
        };

        store.Add(reminder);
        store.AppendEvent(reminder.Id, EventType.Created);

        // Log only the id — notes and full record are intentionally omitted.
        logger.LogInformation("Reminder created. Id={ReminderId}", reminder.Id);

        return reminder;
    }

    public Reminder? GetById(Guid id) => store.GetById(id);

    public PagedResult<ReminderResponse> List(RemindersQuery q)
    {
        var (items, total) = store.Query(q);
        return new PagedResult<ReminderResponse>
        {
            Items      = items.Select(ToResponse),
            TotalCount = total,
            Page       = q.Page,
            PageSize   = q.PageSize,
        };
    }

    /// <summary>
    /// Marks the reminder Done. If it recurs, creates the next occurrence.
    /// Returns (completedReminder, nextOccurrence?).
    /// </summary>
    public (Reminder completed, Reminder? next) Complete(Guid id)
    {
        var reminder = store.GetById(id)
            ?? throw new KeyNotFoundException($"Reminder {id} not found.");

        if (reminder.Status == ReminderStatus.Done)
            throw new InvalidOperationException($"Reminder {id} is already Done.");

        reminder.Status = ReminderStatus.Done;
        store.Update(reminder);
        store.AppendEvent(reminder.Id, EventType.Completed);

        logger.LogInformation("Reminder completed. Id={ReminderId}", reminder.Id);

        Reminder? next = null;

        if (reminder.Recurrence != Recurrence.None)
        {
            var nextDue = reminder.Recurrence == Recurrence.Daily
                ? reminder.DueDate.AddDays(1)
                : reminder.DueDate.AddDays(7);

            next = new Reminder
            {
                PatientName = reminder.PatientName,
                Title       = reminder.Title,
                Notes       = reminder.Notes,
                DueDate     = nextDue,
                Recurrence  = reminder.Recurrence,
            };

            store.Add(next);
            store.AppendEvent(next.Id, EventType.Created);

            // Cross-link: record on the original reminder that a new one was spawned.
            store.AppendEvent(reminder.Id, EventType.NextOccurrenceGenerated, $"nextId={next.Id}");

            logger.LogInformation(
                "Next occurrence generated. OriginalId={OriginalId} NewId={NewId}",
                reminder.Id, next.Id);
        }

        return (reminder, next);
    }

    public IEnumerable<ReminderEventResponse> GetHistory(Guid id)
    {
        // Verify the reminder exists before returning history.
        if (store.GetById(id) is null)
            throw new KeyNotFoundException($"Reminder {id} not found.");

        return store.GetHistory(id).Select(e => new ReminderEventResponse
        {
            Id         = e.Id,
            ReminderId = e.ReminderId,
            Type       = e.Type.ToString(),
            OccurredAt = e.OccurredAt,
            Metadata   = e.Metadata,
        });
    }

    public static ReminderResponse ToResponse(Reminder r) => new()
    {
        Id          = r.Id,
        PatientName = r.PatientName,
        Title       = r.Title,
        Notes       = r.Notes,
        DueDate     = r.DueDate,
        Status      = r.Status.ToString(),
        Recurrence  = r.Recurrence.ToString(),
        CreatedAt   = r.CreatedAt,
    };
}
