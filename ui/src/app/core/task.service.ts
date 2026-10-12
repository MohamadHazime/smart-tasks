import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreateSubTaskRequest,
  SaveTaskRequest,
  SubTask,
  SubTaskSuggestions,
  TaskFilters,
  TaskItem,
} from './models';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/tasks';

  getAll(filters: TaskFilters = {}): Observable<TaskItem[]> {
    let params = new HttpParams();
    if (filters.status) {
      params = params.set('status', filters.status);
    }
    if (filters.priority !== null && filters.priority !== undefined) {
      params = params.set('priority', filters.priority);
    }
    return this.http.get<TaskItem[]>(this.baseUrl, { params });
  }

  getById(id: number): Observable<TaskItem> {
    return this.http.get<TaskItem>(`${this.baseUrl}/${id}`);
  }

  create(request: SaveTaskRequest): Observable<TaskItem> {
    return this.http.post<TaskItem>(this.baseUrl, request);
  }

  update(id: number, request: SaveTaskRequest): Observable<TaskItem> {
    return this.http.put<TaskItem>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  toggle(id: number): Observable<TaskItem> {
    return this.http.patch<TaskItem>(`${this.baseUrl}/${id}/toggle`, null);
  }

  addSubTask(taskId: number, request: CreateSubTaskRequest): Observable<SubTask> {
    return this.http.post<SubTask>(`${this.baseUrl}/${taskId}/subtasks`, request);
  }

  suggestSubTasks(taskId: number): Observable<SubTaskSuggestions> {
    return this.http.post<SubTaskSuggestions>(`${this.baseUrl}/${taskId}/suggest-subtasks`, null);
  }
}
