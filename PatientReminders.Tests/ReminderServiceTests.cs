using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PatientReminders.Api.Data;
using PatientReminders.Api.Models;
using PatientReminders.Api.Services;

namespace PatientReminders.Tests;

public class ReminderServiceTests
{
    private static ReminderService MakeService()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ReminderService(new ReminderStore(new TestDbContextFactory(options)), NullLogger<ReminderService>.Instance);
    }

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
    }

    private static CreateReminderRequest BaseRequest(Recurrence rec = Recurrence.None) => new()
    {
        PatientName = "Test Patient",
        Title       = "Test Reminder",
        DueDate     = DateTime.UtcNow.AddDays(1),
        Recurrence  = rec,
    };

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_SetsOpenStatusAndId()
    {
        var svc = MakeService();
        var r = svc.Create(BaseRequest());

        Assert.NotEqual(Guid.Empty, r.Id);
        Assert.Equal(ReminderStatus.Open, r.Status);
    }

    [Fact]
    public void Create_AppendCreatedEvent()
    {
        var svc = MakeService();
        var r = svc.Create(BaseRequest());
        var history = svc.GetHistory(r.Id).ToList();

        Assert.Single(history);
        Assert.Equal(EventType.Created.ToString(), history[0].Type);
    }

    // ── Complete (non-recurring) ──────────────────────────────────────────────

    [Fact]
    public void Complete_NonRecurring_SetsDoneAndNoNext()
    {
        var svc = MakeService();
        var r = svc.Create(BaseRequest(Recurrence.None));

        var (completed, next) = svc.Complete(r.Id);

        Assert.Equal(ReminderStatus.Done, completed.Status);
        Assert.Null(next);
    }

    [Fact]
    public void Complete_AlreadyDone_Throws()
    {
        var svc = MakeService();
        var r = svc.Create(BaseRequest());
        svc.Complete(r.Id);

        Assert.Throws<InvalidOperationException>(() => svc.Complete(r.Id));
    }

    // ── Complete (recurring) ─────────────────────────────────────────────────

    [Fact]
    public void Complete_DailyRecurrence_CreatesNextDueDatePlusOneDay()
    {
        var svc = MakeService();
        var r = svc.Create(BaseRequest(Recurrence.Daily));
        var originalDue = r.DueDate;

        var (_, next) = svc.Complete(r.Id);

        Assert.NotNull(next);
        Assert.Equal(originalDue.AddDays(1), next!.DueDate);
        Assert.Equal(ReminderStatus.Open, next.Status);
        Assert.Equal(Recurrence.Daily, next.Recurrence);
    }

    [Fact]
    public void Complete_WeeklyRecurrence_CreatesNextDueDatePlusSevenDays()
    {
        var svc = MakeService();
        var r = svc.Create(BaseRequest(Recurrence.Weekly));
        var originalDue = r.DueDate;

        var (_, next) = svc.Complete(r.Id);

        Assert.NotNull(next);
        Assert.Equal(originalDue.AddDays(7), next!.DueDate);
    }

    [Fact]
    public void Complete_Recurring_CarriesOverPatientAndTitle()
    {
        var svc = MakeService();
        var req = BaseRequest(Recurrence.Weekly);
        req.PatientName = "Jane Doe";
        req.Title = "Weekly Check";
        var r = svc.Create(req);

        var (_, next) = svc.Complete(r.Id);

        Assert.Equal("Jane Doe", next!.PatientName);
        Assert.Equal("Weekly Check", next.Title);
    }

    // ── Audit trail ──────────────────────────────────────────────────────────

    [Fact]
    public void Complete_Recurring_AuditTrailHasCorrectEvents()
    {
        var svc = MakeService();
        var r = svc.Create(BaseRequest(Recurrence.Daily));

        var (completed, next) = svc.Complete(r.Id);

        var originalHistory = svc.GetHistory(completed.Id).ToList();
        // created + completed + nextOccurrenceGenerated
        Assert.Equal(3, originalHistory.Count);
        Assert.Equal(EventType.Created.ToString(),                originalHistory[0].Type);
        Assert.Equal(EventType.Completed.ToString(),              originalHistory[1].Type);
        Assert.Equal(EventType.NextOccurrenceGenerated.ToString(), originalHistory[2].Type);

        // The new occurrence has its own Created event.
        var nextHistory = svc.GetHistory(next!.Id).ToList();
        Assert.Single(nextHistory);
        Assert.Equal(EventType.Created.ToString(), nextHistory[0].Type);
    }

    [Fact]
    public void GetHistory_UnknownId_Throws()
    {
        var svc = MakeService();
        Assert.Throws<KeyNotFoundException>(() => svc.GetHistory(Guid.NewGuid()).ToList());
    }

    // ── Complete not found ────────────────────────────────────────────────────

    [Fact]
    public void Complete_UnknownId_Throws()
    {
        var svc = MakeService();
        Assert.Throws<KeyNotFoundException>(() => svc.Complete(Guid.NewGuid()));
    }
}
