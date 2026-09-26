import { ChangeDetectionStrategy, Component, OnInit, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { describeError } from '../../core/auth/problem-details';

type State = 'working' | 'confirmed' | 'failed';

@Component({
  selector: 'app-confirm-email',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  styleUrl: './auth-shell.scss',
  templateUrl: './confirm-email.html',
})
export class ConfirmEmailComponent implements OnInit {
  private readonly auth = inject(AuthService);

  /** Bound from the query string by `withComponentInputBinding()`. */
  readonly token = input('');

  protected readonly state = signal<State>('working');
  protected readonly error = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    if (this.token() === '') {
      this.state.set('failed');
      this.error.set('Връзката е непълна.');
      return;
    }

    try {
      await this.auth.confirmEmail(this.token());
      this.state.set('confirmed');
    } catch (error) {
      this.error.set(describeError(error));
      this.state.set('failed');
    }
  }
}
