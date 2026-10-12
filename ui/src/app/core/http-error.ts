import { HttpErrorResponse } from '@angular/common/http';

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

export function getErrorMessage(error: unknown, fallback = 'Something went wrong. Please try again.'): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }
  if (error.status === 0) {
    return 'Cannot reach the server. Is the API running?';
  }
  if (error.status === 404) {
    return 'This task no longer exists.';
  }

  const problem = error.error as ProblemDetails | null;
  return problem?.detail ?? problem?.title ?? fallback;
}

export function getValidationErrors(error: unknown): Record<string, string[]> | null {
  if (error instanceof HttpErrorResponse && error.status === 400) {
    return (error.error as ProblemDetails | null)?.errors ?? null;
  }
  return null;
}
