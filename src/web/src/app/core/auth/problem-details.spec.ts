import { HttpErrorResponse } from '@angular/common/http';
import { describeError } from './problem-details';

function response(status: number, body: unknown = null): HttpErrorResponse {
  return new HttpErrorResponse({ status, error: body, url: '/api/v1/auth/login' });
}

describe('describeError', () => {
  it('reports a lost connection when the browser never reached a server', () => {
    expect(describeError(response(0))).toContain('Няма връзка със сървъра');
  });

  it('reports an unreachable server when a proxy answers for a stopped api', () => {
    for (const status of [502, 503, 504]) {
      expect(describeError(response(status, 'Error occurred while trying to proxy'))).toContain(
        'Сървърът не отговаря',
      );
    }
  });

  it('does not blame the user for a server error with no problem document', () => {
    const message = describeError(response(500));

    expect(message).toContain('Сървърът');
    expect(message).not.toBe('Възникна неочаквана грешка.');
  });

  it('prefers the first field error of a validation problem', () => {
    const message = describeError(
      response(400, {
        title: 'One or more validation errors occurred.',
        errors: { Email: ['Електронната поща е задължителна.'] },
      }),
    );

    expect(message).toBe('Електронната поща е задължителна.');
  });

  it('falls back to the detail of a problem document', () => {
    const message = describeError(
      response(403, { title: 'Forbidden', detail: 'Не преподавате този предмет.' }),
    );

    expect(message).toBe('Не преподавате този предмет.');
  });

  it('falls back to the title when there is no detail', () => {
    expect(describeError(response(409, { title: 'Конфликт.' }))).toBe('Конфликт.');
  });

  it('handles something that is not an http error at all', () => {
    expect(describeError(new Error('boom'))).toBe('Възникна неочаквана грешка.');
  });
});
