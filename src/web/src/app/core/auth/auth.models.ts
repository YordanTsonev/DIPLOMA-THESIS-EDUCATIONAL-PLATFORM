/** Mirrors `UserRole` on the server. The numeric values must stay in step with it. */
export enum UserRole {
  Student = 'Student',
  Teacher = 'Teacher',
  Parent = 'Parent',
  Admin = 'Admin',
}

export interface User {
  readonly id: string;
  readonly email: string;
  readonly firstName: string;
  readonly middleName: string | null;
  readonly lastName: string;
  readonly fullName: string;
  readonly role: UserRole;
  readonly isActive: boolean;
  readonly emailConfirmed: boolean;
  readonly phoneNumber: string | null;
  readonly createdAt: string;
  readonly lastSignedInAt: string | null;
}

/**
 * What `/auth/login` and `/auth/refresh` return.
 *
 * There is no refresh token here by design — it lives in an `HttpOnly` cookie the browser
 * attaches by itself, so no script in this application can read it.
 */
export interface AuthenticationResponse {
  readonly accessToken: string;
  readonly accessTokenExpiresAt: string;
  readonly user: User;
}

export interface LoginRequest {
  readonly email: string;
  readonly password: string;
}

export interface RegisterRequest {
  readonly email: string;
  readonly firstName: string;
  readonly middleName: string | null;
  readonly lastName: string;
  readonly password: string;
  readonly role: UserRole;
}

export interface PagedResult<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
  readonly totalPages: number;
}

/** Where each role lands after signing in. */
export const HOME_ROUTE: Readonly<Record<UserRole, string>> = {
  [UserRole.Student]: '/student',
  [UserRole.Teacher]: '/teacher',
  [UserRole.Parent]: '/parent',
  [UserRole.Admin]: '/admin',
};
