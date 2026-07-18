# Patient Reminders

Take-home exercise for TrendConnect 2.0. A .NET 10 Web API + Angular 19 app for managing patient reminders.

---

## Running the project

### 1. API

```bash
cd PatientReminders.Api
dotnet run
```

API listens on `http://localhost:5000`.

### 2. Frontend

```bash
cd patient-reminders-ui
npm install      # first time only
npx ng serve
```

UI available at `http://localhost:4200`. Start the API first so the UI can reach it.

### 3. Tests

```bash
dotnet test
```

---

## Key decisions and trade-offs

### In-memory store
Used a plain `List<T>` wrapped in a singleton with `Lock` for thread safety. Simple, zero setup, easy to swap — there's a deliberate `Update()` seam in `ReminderStore` that a SQLite/EF implementation can fill in without touching the service layer.

### Audit trail as appended events
`ReminderEvent` records are append-only — nothing is ever deleted or updated. `Complete` writes three events to the original reminder: `Created` (on creation), `Completed`, and `NextOccurrenceGenerated` (carrying the new reminder's id in `Metadata`). The new occurrence gets its own `Created` event. This gives a clear, inspectable trail without a full event-sourcing framework.

### Recurring reminders
When a `Daily` or `Weekly` reminder is completed, the next occurrence is created with `DueDate + 1 day` or `+ 7 days`. The original due date is used as the anchor (not "now"), so drift doesn't accumulate if a reminder is completed late. The new occurrence carries all fields over including `Notes` and `Recurrence`, so it can itself be completed and rolled forward indefinitely.

### Sensitive `notes` field
- Never logged — logging calls only emit `ReminderId`.
- Not included in structured error messages or validation failure responses.
- Returned in normal API responses (the client needs to display it), which is the right call for an internal care-team tool; in a real system it would be encrypted at rest and the API would sit behind auth.

### Angular: single component
The app is one component (`App`) with inline signals. For three screens' worth of UI this keeps it readable without the overhead of a feature module structure that would be appropriate at larger scale.

---

## What I'd do next with more time

- **Auth**: even a simple bearer token to gate the `notes` field.
- **SQLite + EF Core**: swap `ReminderStore` for a proper persistence layer — the service layer wouldn't need to change.
- **Mark-complete in the UI**: add a Complete button in the list so the recurrence flow is visible end-to-end without hitting the API directly.
- **History modal**: click a reminder to see its audit trail in the UI.
- **More test coverage**: integration tests with `WebApplicationFactory` for the controller layer, edge cases like completing a reminder whose `dueDate` is in the past.
- **Validation**: stronger `dueDate` floor (e.g. at least 1 minute in the future is currently only enforced client-side).
