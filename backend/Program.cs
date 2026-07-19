using Microsoft.EntityFrameworkCore;
using PatientReminders.Api.Data;
using PatientReminders.Api.Models;
using PatientReminders.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=reminders.db"));

builder.Services.AddScoped<ReminderStore>();
builder.Services.AddScoped<ReminderService>();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200", "http://localhost:4201")
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

// Ensure DB is created and seed initial data if empty.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    SeedData(db);
}

app.UseCors();
app.MapControllers();
app.Run();

static void SeedData(AppDbContext db)
{
    if (db.Reminders.Any()) return;

    var now = DateTime.UtcNow;

    var r1 = new Reminder { Id = Guid.NewGuid(), PatientName = "Alice Johnson",  Title = "Follow-up call",   Notes = "Discuss medication adjustment",  DueDate = now.AddDays(1),  Status = ReminderStatus.Open, Recurrence = Recurrence.Weekly, CreatedAt = now.AddDays(-2) };
    var r2 = new Reminder { Id = Guid.NewGuid(), PatientName = "Bob Martinez",   Title = "Lab results due",  Notes = "Blood panel from 2026-07-10",    DueDate = now.AddDays(3),  Status = ReminderStatus.Open, Recurrence = Recurrence.None,   CreatedAt = now.AddDays(-1) };
    var r3 = new Reminder { Id = Guid.NewGuid(), PatientName = "Carol Lee",      Title = "Daily check-in",   DueDate = now.AddDays(-1), Status = ReminderStatus.Done, Recurrence = Recurrence.Daily,  CreatedAt = now.AddDays(-5) };

    db.Reminders.AddRange(r1, r2, r3);

    db.ReminderEvents.AddRange(
        new() { Id = Guid.NewGuid(), ReminderId = r1.Id, Type = EventType.Created,   OccurredAt = r1.CreatedAt },
        new() { Id = Guid.NewGuid(), ReminderId = r2.Id, Type = EventType.Created,   OccurredAt = r2.CreatedAt },
        new() { Id = Guid.NewGuid(), ReminderId = r3.Id, Type = EventType.Created,   OccurredAt = r3.CreatedAt },
        new() { Id = Guid.NewGuid(), ReminderId = r3.Id, Type = EventType.Completed, OccurredAt = r3.CreatedAt.AddDays(4) }
    );

    db.SaveChanges();
}

// Exposed so the test project can use WebApplicationFactory<Program>.
public partial class Program { }
