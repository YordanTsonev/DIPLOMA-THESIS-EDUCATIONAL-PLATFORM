import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { describeError } from '../../core/auth/problem-details';

@Component({
  selector: 'app-reset-password',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
  styleUrl: './auth-shell.scss',
  templateUrl: './reset-password.html',
})
export class ResetPasswordComponent {
  private readonly auth = inject(AuthService);

  /** Bound from the query string by `withComponentInputBinding()`. */
  readonly token = input('');

  protected readonly error = signal<string | null>(null);
  protected readonly done = signal(false);
  protected readonly busy = signal(false);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    newPassword: ['', [Validators.required, Validators.minLength(10)]],
  });

  protected async submit(): Promise<void> {
    if (this.form.invalid || this.busy()) {
      this.form.markAllAsTouched();
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    try {
      await this.auth.resetPassword(this.token(), this.form.getRawValue().newPassword);
      this.done.set(true);
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.busy.set(false);
    }
  }
}
