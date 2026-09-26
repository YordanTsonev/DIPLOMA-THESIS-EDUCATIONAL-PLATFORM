import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { describeError } from '../../core/auth/problem-details';

@Component({
  selector: 'app-forgot-password',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
  styleUrl: './auth-shell.scss',
  templateUrl: './forgot-password.html',
})
export class ForgotPasswordComponent {
  private readonly auth = inject(AuthService);

  protected readonly error = signal<string | null>(null);
  protected readonly done = signal(false);
  protected readonly busy = signal(false);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  protected async submit(): Promise<void> {
    if (this.form.invalid || this.busy()) {
      this.form.markAllAsTouched();
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    try {
      await this.auth.forgotPassword(this.form.getRawValue().email);

      // The server answers the same way for a known and an unknown address, and so does this
      // screen. Saying "no such account" here would leak who has one.
      this.done.set(true);
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.busy.set(false);
    }
  }
}
