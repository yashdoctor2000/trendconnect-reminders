# Patient Reminders

Take-home exercise for TrendConnect 2.0. A .NET 8 Web API + Angular app for managing patient reminders.

---

## Project structure

```
trendconnect-reminders/
├── backend/              # .NET 8 Web API
├── frontend/             # Angular UI
├── PatientReminders.Tests/  # xUnit tests
└── TrendConnectReminders.sln
```

---

## Running the project

### Prerequisites
- .NET 8 SDK
- Node.js (v18+)

### 1. API

```bash
cd backend
dotnet watch
```

API listens on `http://localhost:5082`.

> Use `dotnet watch` — it rebuilds and restarts automatically on file changes. If you use `dotnet run`, stop it with `Ctrl+C` before rebuilding to avoid a file lock error.

### 2. Frontend

```bash
cd frontend
npm install      # first time only
npx ng serve
```

UI available at `http://localhost:4200`. Start the API first.

### 3. Tests

```bash
dotnet test TrendConnectReminders.sln
```

Or to run tests without stopping the API:

```bash
dotnet test PatientReminders.Tests
```

---

## What's built

### Backend endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/reminders` | List with pagination, filtering, sorting |
| `POST` | `/reminders` | Create a reminder |
| `GET` | `/reminders/{id}` | Fetch a single reminder |
| `POST` | `/reminders/{id}/complete` | Mark done, auto-creates next if recurring |
| `GET` | `/reminders/{id}/history` | Audit trail for a reminder |

**Filtering:** `status`, `dueDateFrom`, `dueDateTo`  
**Sorting:** `dueDate` or `createdAt`, `asc` or `desc`  
**Pagination:** `page` + `pageSize`, returns `totalCount`

### Frontend

- List view with status filter, due date range filter, sort controls, and pagination
- Create form with client-side validation, loading and error states
- Complete button on Open reminders — triggers next occurrence for recurring ones
- History modal showing the full audit trail per reminder

---

## Key decisions and trade-offs

### SQLite via EF Core
Chose SQLite over in-memory so data survives API restarts and the demo is more realistic.

### Enums stored as strings
`HasConversion<string>()` means the DB stores `"Open"` / `"Done"` rather than integers. Human-readable and won't silently break if enum order ever changes.

### Audit trail as append-only rows
`ReminderEvent` records are never updated or deleted — only inserted. `Complete` appends `Completed` and `NextOccurrenceGenerated` to the original reminder, and the new occurrence gets its own `Created` event. Gives a clear, inspectable trail without a full event-sourcing framework.

### Recurrence anchored to original `dueDate`
Next occurrence = `dueDate + interval`, not `now + interval`. If a reminder is completed late, the schedule doesn't drift forward. Documented assumption — the right behaviour depends on the business need.

### Single Angular component
Everything lives in one `App` component with signals. Appropriate for this scope; at larger scale it would be split into list, form, and modal components.

### `notes` field handling
Never logged — all logging emits IDs only. Not included in error responses. Returned in normal API responses since the care team needs to read it; in production it would be encrypted at rest behind an auth layer.

### `/reminders/{id}` Never called from frontend
Created the api endpoint but never being called from frontend due lack of time

---

## What I'd do next with more time

- **Auth**: bearer token to gate access, especially to the `notes` field
- **Update/delete endpoints**: the brief listed these as out of scope but they'd be straightforward to add
- **Overdue handling**: decide whether completing a past-due recurring reminder should anchor to `dueDate` or `now`
