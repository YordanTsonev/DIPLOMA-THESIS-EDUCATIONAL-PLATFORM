import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { PagedResult, User, UserRole } from '../auth/auth.models';

export interface Child {
  readonly studentUserId: string;
  readonly fullName: string;
  readonly email: string;
  readonly schoolClassId: string | null;
  readonly schoolClassName: string | null;
}

@Injectable({ providedIn: 'root' })
export class GuardiansService {
  private readonly http = inject(HttpClient);

  myChildren(): Promise<Child[]> {
    return firstValueFrom(this.http.get<Child[]>('/api/v1/guardians/me/children'));
  }

  childrenOf(parentUserId: string): Promise<Child[]> {
    return firstValueFrom(this.http.get<Child[]>(`/api/v1/guardians/${parentUserId}/children`));
  }

  link(parentUserId: string, studentUserId: string): Promise<Child[]> {
    return firstValueFrom(
      this.http.post<Child[]>(`/api/v1/guardians/${parentUserId}/children`, { studentUserId }),
    );
  }

  unlink(parentUserId: string, studentUserId: string): Promise<Child[]> {
    return firstValueFrom(
      this.http.delete<Child[]>(`/api/v1/guardians/${parentUserId}/children/${studentUserId}`),
    );
  }

  students(search = ''): Promise<User[]> {
    let params = new HttpParams()
      .set('role', UserRole.Student)
      .set('pageSize', 100)
      .set('isActive', true);

    if (search.trim() !== '') {
      params = params.set('search', search.trim());
    }

    return firstValueFrom(this.http.get<PagedResult<User>>('/api/v1/users', { params })).then(
      (page) => [...page.items],
    );
  }
}
