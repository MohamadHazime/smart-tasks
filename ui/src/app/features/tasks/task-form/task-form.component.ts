import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, NonNullableFormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { getErrorMessage, getValidationErrors } from '../../../core/http-error';
import {
  DESCRIPTION_MAX_LENGTH,
  PRIORITY_LABELS,
  Priority,
  SaveTaskRequest,
  TITLE_MAX_LENGTH,
} from '../../../core/models';
import { fromDateInputValue, toDateInputValue } from '../../../core/task-utils';
import { TaskService } from '../../../core/task.service';
import { IconComponent } from '../../../shared/icon.component';

type TextField = 'title' | 'description';

function notBlank(control: AbstractControl<string>): ValidationErrors | null {
  return control.value && !control.value.trim() ? { blank: true } : null;
}

@Component({
  selector: 'app-task-form',
  imports: [ReactiveFormsModule, RouterLink, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './task-form.component.html',
  styleUrl: './task-form.component.scss',
})
export class TaskFormComponent implements OnInit {
  readonly id = input<string>();

  private readonly fb = inject(NonNullableFormBuilder);
  private readonly taskService = inject(TaskService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly titleMax = TITLE_MAX_LENGTH;
  protected readonly descriptionMax = DESCRIPTION_MAX_LENGTH;
  protected readonly priorities = [Priority.Low, Priority.Medium, Priority.High].map((value) => ({
    value,
    label: PRIORITY_LABELS[value],
  }));

  protected readonly form = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(TITLE_MAX_LENGTH), notBlank]],
    description: ['', [Validators.maxLength(DESCRIPTION_MAX_LENGTH)]],
    priority: this.fb.control<Priority>(Priority.Medium),
    dueDate: [''],
  });

  protected readonly isEdit = computed(() => !!this.id());
  protected readonly cancelLink = computed(() => (this.isEdit() ? ['/tasks', this.id()] : ['/']));
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.id();
    if (!id) {
      return;
    }

    this.loading.set(true);
    this.taskService
      .getById(Number(id))
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (task) =>
          this.form.setValue({
            title: task.title,
            description: task.description ?? '',
            priority: task.priority,
            dueDate: toDateInputValue(task.dueDate),
          }),
        error: (err) => this.error.set(getErrorMessage(err)),
      });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const request: SaveTaskRequest = {
      title: value.title.trim(),
      description: value.description.trim() || null,
      priority: value.priority,
      dueDate: fromDateInputValue(value.dueDate),
    };

    const id = this.id();
    const save$ = id ? this.taskService.update(Number(id), request) : this.taskService.create(request);

    this.saving.set(true);
    this.error.set(null);
    save$
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (task) => this.router.navigate(['/tasks', task.id]),
        error: (err) => this.handleSaveError(err),
      });
  }

  protected fieldError(field: TextField): string | null {
    const control = this.form.controls[field];
    if (!control.touched || !control.errors) {
      return null;
    }

    const errors = control.errors;
    if (errors['required'] || errors['blank']) {
      return 'Title is required.';
    }
    if (errors['maxlength']) {
      return `Keep it under ${errors['maxlength'].requiredLength} characters.`;
    }
    return errors['server'] ?? null;
  }

  private handleSaveError(err: unknown): void {
    const fieldErrors = getValidationErrors(err);
    if (!fieldErrors) {
      this.error.set(getErrorMessage(err));
      return;
    }

    for (const [key, messages] of Object.entries(fieldErrors)) {
      const control = this.form.get(key.charAt(0).toLowerCase() + key.slice(1));
      if (control) {
        control.setErrors({ server: messages[0] });
        control.markAsTouched();
      } else {
        this.error.set(messages[0]);
      }
    }
  }
}
