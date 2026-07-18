using Microsoft.EntityFrameworkCore;
using PatientReminders.Api.Models;

namespace PatientReminders.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<ReminderEvent> ReminderEvents => Set<ReminderEvent>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Reminder>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.PatientName).IsRequired();
            e.Property(r => r.Title).IsRequired();
            e.Property(r => r.Status).HasConversion<string>();
            e.Property(r => r.Recurrence).HasConversion<string>();
        });

        model.Entity<ReminderEvent>(e =>
        {
            e.HasKey(ev => ev.Id);
            e.Property(ev => ev.Type).HasConversion<string>();
        });
    }
}
