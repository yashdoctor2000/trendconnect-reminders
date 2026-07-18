import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { ReminderService } from './reminders/reminder.service';
import { Reminder, RemindersQuery, CreateReminderRequest, ReminderEvent } from './reminders/reminder.model';

@Component({
  selector: 'app-root',
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private readonly svc = inject(ReminderService);

  // ── List state ───────────────────────────────────────────────────────────
  reminders = signal<Reminder[]>([]);
  totalCount = signal(0);
  loadError = signal('');
  listLoading = signal(false);

  filterStatus = signal('');
  dueDateFrom = signal('');
  dueDateTo = signal('');
  sortBy = signal('dueDate');
  sortDir = signal('asc');
  page = signal(1);
  readonly pageSize = 10;

  totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize));

  // ── History modal state ──────────────────────────────────────────────────
  historyReminder = signal<Reminder | null>(null);
  historyEvents = signal<ReminderEvent[]>([]);
  historyLoading = signal(false);
  historyError = signal('');

  openHistory(r: Reminder) {
    this.historyReminder.set(r);
    this.historyEvents.set([]);
    this.historyError.set('');
    this.historyLoading.set(true);

    this.svc.getHistory(r.id).subscribe({
      next: events => {
        this.historyEvents.set(events);
        this.historyLoading.set(false);
      },
      error: () => {
        this.historyError.set('Failed to load history.');
        this.historyLoading.set(false);
      },
    });
  }

  closeHistory() {
    this.historyReminder.set(null);
  }

  // ── Complete state ───────────────────────────────────────────────────────
  completingId = signal<string | null>(null);
  completeError = signal('');

  complete(id: string) {
    this.completingId.set(id);
    this.completeError.set('');

    this.svc.complete(id).subscribe({
      next: () => {
        this.completingId.set(null);
        this.loadReminders();
      },
      error: err => {
        this.completingId.set(null);
        this.completeError.set(err?.error?.error ?? 'Failed to complete reminder.');
      },
    });
  }

  // ── Create form state ────────────────────────────────────────────────────
  showForm = signal(false);
  createLoading = signal(false);
  createError = signal('');
  createSuccess = signal(false);

  form: CreateReminderRequest = {
    patientName: '',
    title: '',
    notes: '',
    dueDate: '',
    recurrence: 'None',
  };

  ngOnInit() {
    this.loadReminders();
  }

  loadReminders() {
    this.listLoading.set(true);
    this.loadError.set('');

    const query: RemindersQuery = {
      page: this.page(),
      pageSize: this.pageSize,
      sortBy: this.sortBy(),
      sortDir: this.sortDir(),
    };
    if (this.filterStatus()) query.status = this.filterStatus();
    if (this.dueDateFrom()) query.dueDateFrom = new Date(this.dueDateFrom()).toISOString();
    if (this.dueDateTo())   query.dueDateTo   = new Date(this.dueDateTo()).toISOString();

    this.svc.list(query).subscribe({
      next: result => {
        this.reminders.set(result.items);
        this.totalCount.set(result.totalCount);
        this.listLoading.set(false);
      },
      error: () => {
        this.loadError.set('Failed to load reminders. Is the API running?');
        this.listLoading.set(false);
      },
    });
  }

  onFilterChange() {
    this.page.set(1);
    this.loadReminders();
  }

  clearFilters() {
    this.filterStatus.set('');
    this.dueDateFrom.set('');
    this.dueDateTo.set('');
    this.sortBy.set('dueDate');
    this.sortDir.set('asc');
    this.page.set(1);
    this.loadReminders();
  }

  goToPage(p: number) {
    this.page.set(p);
    this.loadReminders();
  }

  onSubmit(ngForm: NgForm) {
    if (ngForm.invalid) return;

    this.createLoading.set(true);
    this.createError.set('');
    this.createSuccess.set(false);

    const req: CreateReminderRequest = {
      patientName: this.form.patientName,
      title: this.form.title,
      notes: this.form.notes || undefined,
      dueDate: new Date(this.form.dueDate).toISOString(),
      recurrence: this.form.recurrence,
    };

    this.svc.create(req).subscribe({
      next: () => {
        this.createLoading.set(false);
        this.createSuccess.set(true);
        this.showForm.set(false);
        this.resetForm(ngForm);
        this.page.set(1);
        this.loadReminders();
      },
      error: err => {
        this.createLoading.set(false);
        const msg = err?.error?.error ?? 'Failed to create reminder. Please try again.';
        this.createError.set(msg);
      },
    });
  }

  resetForm(ngForm: NgForm) {
    ngForm.resetForm();
    this.form = { patientName: '', title: '', notes: '', dueDate: '', recurrence: 'None' };
  }

  toggleForm() {
    this.showForm.update(v => !v);
    this.createError.set('');
    this.createSuccess.set(false);
  }

  minDueDate(): string {
    // datetime-local input needs a local ISO string without the Z suffix
    const d = new Date();
    d.setMinutes(d.getMinutes() + 1);
    return d.toISOString().slice(0, 16);
  }
}
