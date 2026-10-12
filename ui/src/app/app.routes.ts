import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    title: 'Tasks · Smart Tasks',
    loadComponent: () =>
      import('./features/tasks/task-list/task-list.component').then((m) => m.TaskListComponent),
  },
  {
    path: 'tasks/new',
    title: 'New task · Smart Tasks',
    loadComponent: () =>
      import('./features/tasks/task-form/task-form.component').then((m) => m.TaskFormComponent),
  },
  {
    path: 'tasks/:id',
    title: 'Task · Smart Tasks',
    loadComponent: () =>
      import('./features/tasks/task-detail/task-detail.component').then((m) => m.TaskDetailComponent),
  },
  {
    path: 'tasks/:id/edit',
    title: 'Edit task · Smart Tasks',
    loadComponent: () =>
      import('./features/tasks/task-form/task-form.component').then((m) => m.TaskFormComponent),
  },
  { path: '**', redirectTo: '' },
];
