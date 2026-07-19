using PatientReminders.Api.Data;
using PatientReminders.Api.Models;

namespace PatientReminders.Api.Services;

public class ReminderStore(AppDbContext db)
{
    // ── Reminders ────────────────────────────────────────────────────────────

    public Reminder Add(Reminder reminder)
    {
        reminder.Id = Guid.NewGuid();
        reminder.CreatedAt = DateTime.UtcNow;
        reminder.Status = ReminderStatus.Open;
        db.Reminders.Add(reminder);
        db.SaveChanges();
        return reminder;
    }

    public Reminder? GetById(Guid id) => db.Reminders.Find(id);

    public void Update(Reminder reminder)
    {
        db.Reminders.Update(reminder);
        db.SaveChanges();
    }

    public (IEnumerable<Reminder> Items, int TotalCount) Query(RemindersQuery q)
    {
        IQueryable<Reminder> query = db.Reminders;

        if (q.Status.HasValue)
            query = query.Where(r => r.Status == q.Status.Value);

        if (q.DueDateFrom.HasValue)
            query = query.Where(r => r.DueDate >= q.DueDateFrom.Value);

        if (q.DueDateTo.HasValue)
            query = query.Where(r => r.DueDate <= q.DueDateTo.Value);

        query = (q.SortBy?.ToLower() ?? "duedate", q.SortDir?.ToLower() ?? "asc") switch
        {
            ("createdat", "desc") => query.OrderByDescending(r => r.CreatedAt),
            ("createdat", _)      => query.OrderBy(r => r.CreatedAt),
            (_, "desc")           => query.OrderByDescending(r => r.DueDate),
            _                     => query.OrderBy(r => r.DueDate),
        };

        var total = query.Count();
        var items = query.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToList();
        return (items, total);
    }

    // ── Events ───────────────────────────────────────────────────────────────

    public void AppendEvent(Guid reminderId, EventType type, string? metadata = null)
    {
        db.ReminderEvents.Add(new ReminderEvent
        {
            Id = Guid.NewGuid(),
            ReminderId = reminderId,
            Type = type,
            OccurredAt = DateTime.UtcNow,
            Metadata = metadata,
        });
        db.SaveChanges();
    }

    public IEnumerable<ReminderEvent> GetHistory(Guid reminderId) =>
        db.ReminderEvents
            .Where(e => e.ReminderId == reminderId)
            .OrderBy(e => e.OccurredAt)
            .ToList();
}
