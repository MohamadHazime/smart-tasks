export enum Priority {
  Low = 0,
  Medium = 1,
  High = 2,
}

export const PRIORITY_LABELS: Record<Priority, string> = {
  [Priority.Low]: 'Low',
  [Priority.Medium]: 'Medium',
  [Priority.High]: 'High',
};

export type TaskStatusFilter = 'Open' | 'Done';

export interface SubTask {
  id: number;
  title: string;
  isDone: boolean;
}

export interface TaskItem {
  id: number;
  title: string;
  description: string | null;
  priority: Priority;
  isDone: boolean;
  dueDate: string | null;
  createdAt: string;
  modifiedAt: string | null;
  subTasks: SubTask[];
}

export interface SaveTaskRequest {
  title: string;
  description: string | null;
  priority: Priority;
  dueDate: string | null;
}

export interface CreateSubTaskRequest {
  title: string;
}

export interface SubTaskSuggestions {
  taskId: number;
  suggestions: string[];
}

export interface TaskFilters {
  status?: TaskStatusFilter | null;
  priority?: Priority | null;
}

export const TITLE_MAX_LENGTH = 200;
export const DESCRIPTION_MAX_LENGTH = 2000;
