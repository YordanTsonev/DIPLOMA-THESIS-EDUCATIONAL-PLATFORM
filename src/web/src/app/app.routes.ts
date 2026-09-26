import { Routes } from '@angular/router';
import { anonymousGuard, authGuard, roleGuard } from './core/auth/auth.guards';
import { UserRole } from './core/auth/auth.models';

/**
 * Every component is loaded on demand, so the login screen does not ship the administration
 * table to a student who will never open it.
 */
export const routes: Routes = [
  {
    path: 'auth',
    canActivate: [anonymousGuard],
    children: [
      {
        path: 'login',
        loadComponent: () => import('./features/auth/login').then((m) => m.LoginComponent),
        title: 'Вход — EduPlatform',
      },
      {
        path: 'register',
        loadComponent: () => import('./features/auth/register').then((m) => m.RegisterComponent),
        title: 'Регистрация — EduPlatform',
      },
      {
        path: 'forgot-password',
        loadComponent: () =>
          import('./features/auth/forgot-password').then((m) => m.ForgotPasswordComponent),
        title: 'Забравена парола — EduPlatform',
      },
      {
        path: 'reset-password',
        loadComponent: () =>
          import('./features/auth/reset-password').then((m) => m.ResetPasswordComponent),
        title: 'Нова парола — EduPlatform',
      },
      { path: '', pathMatch: 'full', redirectTo: 'login' },
    ],
  },

  // Confirmation is reached from an e-mail, which a signed-in user may well click, so it sits
  // outside the anonymous-only group.
  {
    path: 'auth/confirm-email',
    loadComponent: () =>
      import('./features/auth/confirm-email').then((m) => m.ConfirmEmailComponent),
    title: 'Потвърждаване — EduPlatform',
  },

  {
    path: '',
    loadComponent: () => import('./layout/shell').then((m) => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      {
        path: 'student',
        canActivate: [roleGuard(UserRole.Student)],
        loadComponent: () =>
          import('./features/dashboard/dashboard').then((m) => m.DashboardComponent),
        data: { audience: 'student' },
        title: 'Начало — EduPlatform',
      },
      {
        path: 'teacher',
        canActivate: [roleGuard(UserRole.Teacher)],
        loadComponent: () =>
          import('./features/dashboard/dashboard').then((m) => m.DashboardComponent),
        data: { audience: 'teacher' },
        title: 'Начало — EduPlatform',
      },
      {
        path: 'parent',
        canActivate: [roleGuard(UserRole.Parent)],
        loadComponent: () =>
          import('./features/dashboard/dashboard').then((m) => m.DashboardComponent),
        data: { audience: 'parent' },
        title: 'Начало — EduPlatform',
      },
      {
        path: 'admin',
        canActivate: [roleGuard(UserRole.Admin)],
        children: [
          {
            path: '',
            loadComponent: () =>
              import('./features/dashboard/dashboard').then((m) => m.DashboardComponent),
            data: { audience: 'admin' },
            title: 'Начало — EduPlatform',
          },
          {
            path: 'users',
            loadComponent: () => import('./features/admin/users').then((m) => m.AdminUsersComponent),
            title: 'Потребители — EduPlatform',
          },
          {
            path: 'system',
            loadComponent: () =>
              import('./features/system-status/system-status').then((m) => m.SystemStatusComponent),
            title: 'Състояние — EduPlatform',
          },
        ],
      },

      // Lands the user on the home for whichever role they hold.
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () => import('./features/home-redirect').then((m) => m.HomeRedirectComponent),
      },
    ],
  },

  { path: '**', redirectTo: '' },
];
