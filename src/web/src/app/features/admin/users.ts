import { HttpClient, HttpParams } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { PagedResult, User, UserRole } from '../../core/auth/auth.models';
import { describeError } from '../../core/auth/problem-details';

/**
 * User administration: search, create accounts in any role, change roles, deactivate.
 *
 * This screen is how teacher and administrator accounts come into being — the public
 * registration form refuses those roles.
 */
@Component({
  selector: 'app-admin-users',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, DatePipe],
  styleUrl: './users.scss',
  templateUrl: './users.html',
})
export class AdminUsersComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly roles = Object.values(UserRole);

  protected readonly result = signal<PagedResult<User> | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);
  protected readonly creating = signal(false);

  protected readonly filters = this.formBuilder.nonNullable.group({
    search: [''],
    role: [''],
    isActive: [''],
  });

  protected readonly createForm = this.formBuilder.nonNullable.group({
    firstName: ['', Validators.required],
    middleName: [''],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(10)]],
    role: [UserRole.Teacher, Validators.required],
  });

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  protected async load(page = 1): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const { search, role, isActive } = this.filters.getRawValue();

      let params = new HttpParams().set('page', page).set('pageSize', 20);
      if (search.trim() !== '') {
        params = params.set('search', search.trim());
      }
      if (role !== '') {
        params = params.set('role', role);
      }
      if (isActive !== '') {
        params = params.set('isActive', isActive);
      }

      this.result.set(
        await firstValueFrom(this.http.get<PagedResult<User>>('/api/v1/users', { params })),
      );
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected async createUser(): Promise<void> {
    if (this.createForm.invalid || this.creating()) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.creating.set(true);
    this.error.set(null);

    try {
      const value = this.createForm.getRawValue();
      const created = await firstValueFrom(
        this.http.post<User>('/api/v1/users', {
          ...value,
          middleName: value.middleName.trim() === '' ? null : value.middleName.trim(),
        }),
      );

      this.notice.set(`Създаден акаунт за ${created.fullName}.`);
      this.createForm.reset({ role: UserRole.Teacher });
      await this.load();
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.creating.set(false);
    }
  }

  protected async changeRole(user: User, role: string): Promise<void> {
    if (role === user.role) {
      return;
    }

    await this.mutate(
      () => firstValueFrom(this.http.put<User>(`/api/v1/users/${user.id}/role`, { role })),
      `Ролята на ${user.fullName} е променена. Сесиите му са прекратени.`,
    );
  }

  protected async toggleActive(user: User): Promise<void> {
    await this.mutate(
      () =>
        firstValueFrom(
          this.http.put<User>(`/api/v1/users/${user.id}/active`, { isActive: !user.isActive }),
        ),
      user.isActive ? `${user.fullName} е деактивиран.` : `${user.fullName} е активиран отново.`,
    );
  }

  private async mutate(action: () => Promise<User>, message: string): Promise<void> {
    this.error.set(null);
    this.notice.set(null);

    try {
      await action();
      this.notice.set(message);
      await this.load(this.result()?.page ?? 1);
    } catch (error) {
      this.error.set(describeError(error));
    }
  }
}
