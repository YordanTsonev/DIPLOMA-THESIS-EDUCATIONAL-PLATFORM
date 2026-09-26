import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, from, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

/** Endpoints that must never trigger a refresh — a 401 from them *is* the answer. */
const AUTH_ENDPOINTS = ['/api/v1/auth/login', '/api/v1/auth/refresh', '/api/v1/auth/logout'];

/**
 * One refresh at a time, shared by every request that hits a 401 while it is running.
 *
 * Without this, a dashboard firing five parallel calls on an expired token would start five
 * refreshes; each rotates the cookie, so four would present an already-used token and the
 * server would treat it as theft and revoke every session.
 */
let refreshInFlight: Promise<void> | null = null;

function refreshOnce(auth: AuthService): Promise<void> {
  refreshInFlight ??= auth.refresh().finally(() => {
    refreshInFlight = null;
  });

  return refreshInFlight;
}

/**
 * Attaches the access token and transparently renews it once when the server says it expired.
 */
export const authInterceptor: HttpInterceptorFn = (
  request: HttpRequest<unknown>,
  next: HttpHandlerFn,
): Observable<HttpEvent<unknown>> => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const isAuthEndpoint = AUTH_ENDPOINTS.some((url) => request.url.startsWith(url));

  return next(withToken(request, auth.token())).pipe(
    catchError((error: unknown) => {
      const shouldRetry =
        error instanceof HttpErrorResponse && error.status === 401 && !isAuthEndpoint;

      if (!shouldRetry) {
        return throwError(() => error);
      }

      return from(refreshOnce(auth)).pipe(
        // Retry once with the token the refresh produced.
        switchMap(() => next(withToken(request, auth.token()))),
        catchError((refreshError: unknown) => {
          void router.navigate(['/auth/login'], {
            queryParams: { returnUrl: router.url },
          });

          return throwError(() => refreshError);
        }),
      );
    }),
  );
};

function withToken(request: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  if (token === null) {
    return request;
  }

  return request.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}
