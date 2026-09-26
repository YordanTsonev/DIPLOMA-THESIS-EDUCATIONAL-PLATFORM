import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';

/**
 * Sends the user from the root path to the home screen for their role.
 *
 * A component rather than a redirect in the route table, because the destination is only known
 * once the session has been restored and the role is available.
 */
@Component({
  selector: 'app-home-redirect',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '',
})
export class HomeRedirectComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  async ngOnInit(): Promise<void> {
    await this.router.navigateByUrl(this.auth.homeRoute());
  }
}
