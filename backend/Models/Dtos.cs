using System.ComponentModel.DataAnnotations;

namespace PatientReminders.Api.Models;

public class CreateReminderRequest
{
    [Required, MinLength(1)]
    public string PatientName { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Sensitive — handled carefully in logging and error responses.</summary>
    public string? Notes { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    public Recurrence Recurrence { get; set; } = Recurrence.None;
}

public class ReminderResponse
{
    public Guid Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    // Notes intentionally included in response — only excluded from logs.
    public string? Notes { get; set; }

    public DateTime DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Recurrence { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ReminderEventResponse
{
    public Guid Id { get; set; }
    public Guid ReminderId { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string? Metadata { get; set; }
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class RemindersQuery
{
    public ReminderStatus? Status { get; set; }
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }
    public string SortBy { get; set; } = "dueDate";          // dueDate | createdAt
    public string SortDir { get; set; } = "asc";             // asc | desc
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
