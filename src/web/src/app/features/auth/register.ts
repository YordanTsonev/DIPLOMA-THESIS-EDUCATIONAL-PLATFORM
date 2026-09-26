import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { UserRole } from '../../core/auth/auth.models';
import { describeError } from '../../core/auth/problem-details';

@Component({
  selector: 'app-register',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
  styleUrl: './auth-shell.scss',
  templateUrl: './register.html',
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);

  /** Teacher and admin accounts are created by an administrator; the server refuses them here. */
  protected readonly roles = [
    { value: UserRole.Student, label: 'Ученик' },
    { value: UserRole.Parent, label: 'Родител' },
  ];

  protected readonly error = signal<string | null>(null);
  protected readonly done = signal(false);
  protected readonly busy = signal(false);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    firstName: ['', Validators.required],
    middleName: [''],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(10)]],
    role: [UserRole.Student, Validators.required],
  });

  protected async submit(): Promise<void> {
    if (this.form.invalid || this.busy()) {
      this.form.markAllAsTouched();
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    try {
      const value = this.form.getRawValue();
      await this.auth.register({
        ...value,
        middleName: value.middleName.trim() === '' ? null : value.middleName.trim(),
      });

      this.done.set(true);
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.busy.set(false);
    }
  }
}
