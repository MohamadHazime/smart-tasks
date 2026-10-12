import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { concatMap, finalize, from, toArray } from 'rxjs';
import { getErrorMessage } from '../../../core/http-error';
import { SubTask, TITLE_MAX_LENGTH, TaskItem } from '../../../core/models';
import { isOverdue, subTaskProgress } from '../../../core/task-utils';
import { TaskService } from '../../../core/task.service';
import { IconComponent } from '../../../shared/icon.component';
import { PriorityBadgeComponent } from '../../../shared/priority-badge.component';

interface Suggestion {
  title: string;
  selected: boolean;
}

@Component({
  selector: 'app-task-detail',
  imports: [DatePipe, ReactiveFormsModule, RouterLink, IconComponent, PriorityBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './task-detail.component.html',
  styleUrl: './task-detail.component.scss',
})
export class TaskDetailComponent implements OnInit {
  readonly id = input.required<string>();

  private readonly taskService = inject(TaskService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  private readonly taskId = computed(() => Number(this.id()));

  protected readonly task = signal<TaskItem | null>(null);
  protected readonly loadError = signal<string | null>(null);
  protected readonly actionError = signal<string | null>(null);
  protected readonly busy = signal(false);
  protected readonly busySubTaskIds = signal<ReadonlySet<number>>(new Set());

  protected readonly newSubTask = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(TITLE_MAX_LENGTH)],
  });
  protected readonly addingSubTask = signal(false);

  protected readonly suggestions = signal<Suggestion[] | null>(null);
  protected readonly suggesting = signal(false);
  protected readonly savingSuggestions = signal(false);
  protected readonly aiError = signal<string | null>(null);

  protected readonly progress = computed(() => {
    const task = this.task();
    return task ? subTaskProgress(task) : null;
  });
  protected readonly overdue = computed(() => {
    const task = this.task();
    return task ? isOverdue(task) : false;
  });
  protected readonly selectedCount = computed(
    () => this.suggestions()?.filter((s) => s.selected).length ?? 0,
  );

  ngOnInit(): void {
    this.taskService
      .getById(this.taskId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (task) => this.task.set(task),
        error: (err) => this.loadError.set(getErrorMessage(err)),
      });
  }

  protected toggleTask(): void {
    this.busy.set(true);
    this.actionError.set(null);
    this.taskService
      .toggle(this.taskId())
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (task) => this.task.set(task),
        error: (err) => this.actionError.set(getErrorMessage(err)),
      });
  }

  protected deleteTask(): void {
    const task = this.task();
    if (!task || !confirm(`Delete "${task.title}"? This also removes its subtasks.`)) {
      return;
    }

    this.busy.set(true);
    this.taskService
      .delete(task.id)
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => this.router.navigate(['/']),
        error: (err) => this.actionError.set(getErrorMessage(err)),
      });
  }

  protected toggleSubTask(subTask: SubTask): void {
    this.setSubTaskBusy(subTask.id, true);
    this.patchSubTask(subTask.id, !subTask.isDone);

    this.taskService
      .toggleSubTask(this.taskId(), subTask.id)
      .pipe(
        finalize(() => this.setSubTaskBusy(subTask.id, false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        error: (err) => {
          this.patchSubTask(subTask.id, subTask.isDone);
          this.actionError.set(getErrorMessage(err));
        },
      });
  }

  protected addSubTask(event: Event): void {
    event.preventDefault();

    const title = this.newSubTask.value.trim();
    if (!title || this.newSubTask.invalid) {
      return;
    }

    this.addingSubTask.set(true);
    this.actionError.set(null);
    this.taskService
      .addSubTask(this.taskId(), { title })
      .pipe(
        finalize(() => this.addingSubTask.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (subTask) => {
          this.appendSubTasks([subTask]);
          this.newSubTask.reset();
        },
        error: (err) => this.actionError.set(getErrorMessage(err)),
      });
  }

  protected suggest(): void {
    this.suggesting.set(true);
    this.aiError.set(null);
    this.suggestions.set(null);

    this.taskService
      .suggestSubTasks(this.taskId())
      .pipe(
        finalize(() => this.suggesting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (result) => {
          if (result.suggestions.length === 0) {
            this.aiError.set('No new ideas this time. The existing subtasks already cover it.');
            return;
          }
          this.suggestions.set(result.suggestions.map((title) => ({ title, selected: true })));
        },
        error: (err) => this.aiError.set(getErrorMessage(err, 'The AI could not generate suggestions.')),
      });
  }

  protected toggleSuggestion(index: number): void {
    this.suggestions.update(
      (list) => list?.map((s, i) => (i === index ? { ...s, selected: !s.selected } : s)) ?? null,
    );
  }

  protected dismissSuggestions(): void {
    this.suggestions.set(null);
    this.aiError.set(null);
  }

  protected saveSuggestions(): void {
    const selected = this.suggestions()?.filter((s) => s.selected) ?? [];
    if (selected.length === 0) {
      return;
    }

    this.savingSuggestions.set(true);
    this.aiError.set(null);
    from(selected)
      .pipe(
        concatMap((s) => this.taskService.addSubTask(this.taskId(), { title: s.title })),
        toArray(),
        finalize(() => this.savingSuggestions.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (created) => {
          this.appendSubTasks(created);
          this.suggestions.set(null);
        },
        error: (err) => {
          this.aiError.set(getErrorMessage(err));
          this.reload();
        },
      });
  }

  private reload(): void {
    this.taskService
      .getById(this.taskId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((task) => this.task.set(task));
  }

  private appendSubTasks(subTasks: SubTask[]): void {
    this.task.update((task) => (task ? { ...task, subTasks: [...task.subTasks, ...subTasks] } : task));
  }

  private patchSubTask(id: number, isDone: boolean): void {
    this.task.update((task) =>
      task ? { ...task, subTasks: task.subTasks.map((s) => (s.id === id ? { ...s, isDone } : s)) } : task,
    );
  }

  private setSubTaskBusy(id: number, busy: boolean): void {
    this.busySubTaskIds.update((ids) => {
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
