import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterOutlet } from '@angular/router';
import { TaskService } from './core/task.service';

@Component({
  imports: [RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  private readonly taskService = inject(TaskService);

  protected readonly taskCount = signal<number | null>(null);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.taskService
      .getAll()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (tasks) => this.taskCount.set(tasks.length),
        error: () => this.error.set('Could not reach the API. Is it running on port 5062?'),
      });
  }
}
