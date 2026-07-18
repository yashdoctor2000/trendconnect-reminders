import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateReminderRequest, PagedResult, Reminder, ReminderEvent, RemindersQuery } from './reminder.model';

@Injectable({ providedIn: 'root' })
export class ReminderService {
  private readonly http = inject(HttpClient);
  private readonly base = 'http://localhost:5082/reminders';

  list(query: RemindersQuery): Observable<PagedResult<Reminder>> {
    let params = new HttpParams();
    if (query.status)      params = params.set('status', query.status);
    if (query.dueDateFrom) params = params.set('dueDateFrom', query.dueDateFrom);
    if (query.dueDateTo)   params = params.set('dueDateTo', query.dueDateTo);
    if (query.sortBy)      params = params.set('sortBy', query.sortBy);
    if (query.sortDir)     params = params.set('sortDir', query.sortDir);
    if (query.page)        params = params.set('page', query.page);
    if (query.pageSize)    params = params.set('pageSize', query.pageSize);
    return this.http.get<PagedResult<Reminder>>(this.base, { params });
  }

  create(req: CreateReminderRequest): Observable<Reminder> {
    return this.http.post<Reminder>(this.base, req);
  }

  complete(id: string): Observable<{ completed: Reminder; next: Reminder | null }> {
    return this.http.post<{ completed: Reminder; next: Reminder | null }>(`${this.base}/${id}/complete`, {});
  }

  getHistory(id: string): Observable<ReminderEvent[]> {
    return this.http.get<ReminderEvent[]>(`${this.base}/${id}/history`);
  }
}
