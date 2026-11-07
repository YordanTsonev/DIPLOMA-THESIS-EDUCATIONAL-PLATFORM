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

  if (error.status === 502 || error.status === 503 || error.status === 504) {
    return 'Сървърът не отговаря. Опитайте отново след малко.';
  }

  const problem = error.error as ProblemDetails | null;

  const firstFieldError = problem?.errors ? Object.values(problem.errors).flat().at(0) : undefined;

  const described = firstFieldError ?? problem?.detail ?? problem?.title;

  if (described) {
    return described;
  }

  return error.status >= 500
    ? 'Сървърът върна грешка. Опитайте отново след малко.'
    : 'Възникна неочаквана грешка.';
}
