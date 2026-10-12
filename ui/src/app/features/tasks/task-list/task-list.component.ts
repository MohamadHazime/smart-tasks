import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, finalize, of, switchMap } from 'rxjs';
import { getErrorMessage } from '../../../core/http-error';
import { PRIORITY_LABELS, Priority, TaskItem, TaskStatusFilter } from '../../../core/models';
import { isOverdue, subTaskProgress } from '../../../core/task-utils';
import { TaskService } from '../../../core/task.service';
import { IconComponent } from '../../../shared/icon.component';
import { PriorityBadgeComponent } from '../../../shared/priority-badge.component';

interface StatusOption {
  label: string;
  value: TaskStatusFilter | null;
}

@Component({
  selector: 'app-task-list',
  imports: [RouterLink, DatePipe, IconComponent, PriorityBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './task-list.component.html',
  styleUrl: './task-list.component.scss',
})
export class TaskListComponent {
  private readonly taskService = inject(TaskService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly statusOptions: StatusOption[] = [
    { label: 'All', value: null },
    { label: 'Open', value: 'Open' },
    { label: 'Done', value: 'Done' },
  ];
  protected readonly priorityOptions = [Priority.High, Priority.Medium, Priority.Low].map((value) => ({
    value,
    label: PRIORITY_LABELS[value],
  }));

  protected readonly status = signal<TaskStatusFilter | null>(null);
  protected readonly priority = signal<Priority | null>(null);
  private readonly refresh = signal(0);

  protected readonly tasks = signal<TaskItem[] | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly busyIds = signal<ReadonlySet<number>>(new Set());

  protected readonly hasFilters = computed(() => this.status() !== null || this.priority() !== null);
  protected readonly openCount = computed(() => (this.tasks() ?? []).filter((t) => !t.isDone).length);

  protected readonly isOverdue = isOverdue;
  protected readonly progress = subTaskProgress;

  private readonly query = computed(() => ({
    status: this.status(),
    priority: this.priority(),
    refresh: this.refresh(),
  }));

  constructor() {
    toObservable(this.query)
      .pipe(
        switchMap(({ status, priority }) =>
          this.taskService.getAll({ status, priority }).pipe(
            catchError((err) => {
              this.error.set(getErrorMessage(err));
              return of(null);
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((tasks) => {
        if (tasks) {
          this.tasks.set(tasks);
          this.error.set(null);
        }
      });
  }

  protected onPriorityChange(value: string): void {
    this.priority.set(value === '' ? null : (Number(value) as Priority));
  }

  protected clearFilters(): void {
    this.status.set(null);
    this.priority.set(null);
  }

  protected toggle(task: TaskItem): void {
    this.setBusy(task.id, true);
    this.patchTask(task.id, { isDone: !task.isDone });

    this.taskService
      .toggle(task.id)
      .pipe(
        finalize(() => this.setBusy(task.id, false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => this.refresh.update((n) => n + 1),
        error: (err) => {
          this.patchTask(task.id, { isDone: task.isDone });
          this.error.set(getErrorMessage(err));
        },
      });
  }

  protected delete(task: TaskItem): void {
    if (!confirm(`Delete "${task.title}"? This also removes its subtasks.`)) {
      return;
    }

    this.setBusy(task.id, true);
    this.taskService
      .delete(task.id)
      .pipe(
        finalize(() => this.setBusy(task.id, false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => this.tasks.update((list) => list?.filter((t) => t.id !== task.id) ?? null),
        error: (err) => this.error.set(getErrorMessage(err)),
      });
  }

  private patchTask(id: number, changes: Partial<TaskItem>): void {
    this.tasks.update((list) => list?.map((t) => (t.id === id ? { ...t, ...changes } : t)) ?? null);
  }

  private setBusy(id: number, busy: boolean): void {
    this.busyIds.update((ids) => {
      const next = new Set(ids);
      if (busy) {
        next.add(id);
      } else {
        next.delete(id);
      }
      return next;
    });
  }
}
