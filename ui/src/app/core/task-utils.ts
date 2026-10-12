import { TaskItem } from './models';

export function isOverdue(task: TaskItem): boolean {
  if (!task.dueDate || task.isDone) {
    return false;
  }
  return toDateInputValue(task.dueDate) < todayInputValue();
}

export function todayInputValue(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}

export function subTaskProgress(task: TaskItem): { done: number; total: number; percent: number } {
  const total = task.subTasks.length;
  const done = task.subTasks.filter((s) => s.isDone).length;
  return { done, total, percent: total === 0 ? 0 : Math.round((done / total) * 100) };
}

export function toDateInputValue(isoDate: string | null): string {
  return isoDate ? isoDate.slice(0, 10) : '';
}

export function fromDateInputValue(value: string): string | null {
  return value ? `${value}T00:00:00Z` : null;
}
