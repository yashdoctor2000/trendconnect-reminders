export interface Reminder {
  id: string;
  patientName: string;
  title: string;
  notes?: string;
  dueDate: string;
  status: 'Open' | 'Done';
  recurrence: 'None' | 'Daily' | 'Weekly';
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface RemindersQuery {
  status?: string;
  dueDateFrom?: string;
  dueDateTo?: string;
  sortBy?: string;
  sortDir?: string;
  page?: number;
  pageSize?: number;
}

export interface ReminderEvent {
  id: string;
  reminderId: string;
  type: string;
  occurredAt: string;
  metadata?: string;
}

export interface CreateReminderRequest {
  patientName: string;
  title: string;
  notes?: string;
  dueDate: string;
  recurrence: string;
}
