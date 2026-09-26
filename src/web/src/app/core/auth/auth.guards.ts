import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { UserRole } from './auth.models';

/**
 * Lets a route through only for a signed-in user.
 *
 * Waits for `restoreSession()` to finish first: on a hard reload the token has not been
 * recovered yet, and without the wait every protected route would bounce to the login page
 * for a moment before the session comes back.
 */
export const authGuard: CanActivateFn = async (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isReady()) {
    await auth.restoreSession();
  }

  if (auth.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/auth/login'], {
    queryParams: { returnUrl: state.url },
  });
};

/**
 * Restricts a route to specific roles.
 *
 * This is convenience for the user interface, not security. The server enforces the same rule
 * on every endpoint; a guard only decides what to render.
 */
export function roleGuard(...allowed: readonly UserRole[]): CanActivateFn {
  return async (route, state) => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isReady()) {
      await auth.restoreSession();
    }

    if (!auth.isAuthenticated()) {
      return router.createUrlTree(['/auth/login'], {
        queryParams: { returnUrl: state.url },
      });
    }

    const role = auth.role();
    if (role !== null && allowed.includes(role)) {
      return true;
    }

    // Send them to their own home rather than to a dead end.
    return router.createUrlTree([auth.homeRoute()]);
  };
}

/** Keeps a signed-in user off the login and registration pages. */
export const anonymousGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isReady()) {
    await auth.restoreSession();
  }

  return auth.isAuthenticated() ? router.createUrlTree([auth.homeRoute()]) : true;
};
