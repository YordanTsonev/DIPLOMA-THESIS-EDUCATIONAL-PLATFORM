import { HttpErrorResponse } from '@angular/common/http';

/** RFC 9457 problem document, as produced by the API. */
interface ProblemDetails {
  readonly title?: string;
  readonly detail?: string;
  readonly status?: number;
  readonly errors?: Record<string, readonly string[]>;
}

/**
 * Turns an API error into one sentence fit to show a user.
 *
 * Validation problems carry a field-by-field map; the first message from it is far more useful
 * than the generic title that accompanies it.
 */
export function describeError(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'Възникна неочаквана грешка.';
  }

  if (error.status === 0) {
    return 'Няма връзка със сървъра. Проверете интернет връзката си.';
  }

  const problem = error.error as ProblemDetails | null;

  const firstFieldError = problem?.errors
    ? Object.values(problem.errors).flat().at(0)
    : undefined;

  return firstFieldError ?? problem?.detail ?? problem?.title ?? 'Възникна неочаквана грешка.';
}
