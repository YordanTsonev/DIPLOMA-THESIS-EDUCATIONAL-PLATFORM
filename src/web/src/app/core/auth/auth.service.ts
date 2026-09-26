import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  AuthenticationResponse,
  HOME_ROUTE,
  LoginRequest,
  RegisterRequest,
  User,
  UserRole,
} from './auth.models';

/**
 * Holds the session and talks to the authentication endpoints.
 *
 * The access token is kept in memory only. Putting it in `localStorage` would leave it readable
 * by any injected script and would survive long after the tab is gone; keeping it in a signal
 * means a reload starts from nothing and the session is rebuilt from the refresh cookie instead.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly accessToken = signal<string | null>(null);
  private readonly currentUser = signal<User | null>(null);

  /** False until `restoreSession()` has finished, so guards do not redirect during start-up. */
  private readonly initialised = signal(false);

  readonly user = this.currentUser.asReadonly();
  readonly isReady = this.initialised.asReadonly();
  readonly isAuthenticated = computed(() => this.currentUser() !== null);
  readonly role = computed(() => this.currentUser()?.role ?? null);

  /** Read by the HTTP interceptor on every outgoing request. */
  token(): string | null {
    return this.accessToken();
  }

  /**
   * Called once at start-up. A reload loses the in-memory token, but the refresh cookie survives,
   * so the session can be rebuilt without asking the user to sign in again.
   */
  async restoreSession(): Promise<void> {
    try {
      await this.refresh();
    } catch {
      // No cookie, or it has expired: an anonymous visitor, which is a normal state.
      this.clear();
    } finally {
      this.initialised.set(true);
    }
  }

  async login(request: LoginRequest): Promise<User> {
    const response = await firstValueFrom(
      this.http.post<AuthenticationResponse>('/api/v1/auth/login', request, {
        withCredentials: true,
      }),
    );

    this.apply(response);
    return response.user;
  }

  async register(request: RegisterRequest): Promise<User> {
    return await firstValueFrom(this.http.post<User>('/api/v1/auth/register', request));
  }

  async refresh(): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<AuthenticationResponse>(
        '/api/v1/auth/refresh',
        {},
        { withCredentials: true },
      ),
    );

    this.apply(response);
  }

  async logout(): Promise<void> {
    try {
      await firstValueFrom(
        this.http.post('/api/v1/auth/logout', {}, { withCredentials: true }),
      );
    } finally {
      // Clear locally even if the call failed — the user asked to be signed out.
      this.clear();
      await this.router.navigate(['/auth/login']);
    }
  }

  async confirmEmail(token: string): Promise<void> {
    await firstValueFrom(this.http.post('/api/v1/auth/confirm-email', { token }));
  }

  async forgotPassword(email: string): Promise<void> {
    await firstValueFrom(this.http.post('/api/v1/auth/forgot-password', { email }));
  }

  async resetPassword(token: string, newPassword: string): Promise<void> {
    await firstValueFrom(this.http.post('/api/v1/auth/reset-password', { token, newPassword }));
  }

  /** Where to send this user after a successful sign-in. */
  homeRoute(role: UserRole = this.role() ?? UserRole.Student): string {
    return HOME_ROUTE[role];
  }

  private apply(response: AuthenticationResponse): void {
    this.accessToken.set(response.accessToken);
    this.currentUser.set(response.user);
  }

  private clear(): void {
    this.accessToken.set(null);
    this.currentUser.set(null);
  }
}
