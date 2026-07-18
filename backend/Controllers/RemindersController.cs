using Microsoft.AspNetCore.Mvc;
using PatientReminders.Api.Models;
using PatientReminders.Api.Services;

namespace PatientReminders.Api.Controllers;

[ApiController]
[Route("reminders")]
public class RemindersController(ReminderService service) : ControllerBase
{
    [HttpGet]
    public IActionResult List([FromQuery] RemindersQuery query)
    {
        if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            return BadRequest(new { error = "page must be ≥ 1 and pageSize must be between 1 and 100." });

        var result = service.List(query);
        return Ok(result);
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateReminderRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { errors = ModelState.ToDictionary(
                kv => kv.Key,
                kv => kv.Value?.Errors.Select(e => e.ErrorMessage)) });

        if (req.DueDate <= DateTime.UtcNow)
            return BadRequest(new { error = "dueDate must be in the future." });

        var reminder = service.Create(req);
        return CreatedAtAction(nameof(GetById), new { id = reminder.Id },
            ReminderService.ToResponse(reminder));
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
    {
        var reminder = service.GetById(id);
        if (reminder is null) return NotFound(new { error = $"Reminder {id} not found." });
        return Ok(ReminderService.ToResponse(reminder));
    }

    [HttpPost("{id:guid}/complete")]
    public IActionResult Complete(Guid id)
    {
        try
        {
            var (completed, next) = service.Complete(id);
            return Ok(new
            {
                completed = ReminderService.ToResponse(completed),
                next = next is null ? null : ReminderService.ToResponse(next),
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}/history")]
    public IActionResult History(Guid id)
    {
        try
        {
            var events = service.GetHistory(id);
            return Ok(events);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
