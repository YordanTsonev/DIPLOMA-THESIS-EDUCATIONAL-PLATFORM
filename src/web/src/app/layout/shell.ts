import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';
import { UserRole } from '../core/auth/auth.models';

interface NavItem {
  readonly label: string;
  readonly route: string;
}

/**
 * The frame around every signed-in screen: header, role-specific navigation, sign-out.
 *
 * The menu is derived from the role rather than filtered in the template, so adding a role in a
 * later phase means adding one entry to this map and nothing else.
 */
@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  styleUrl: './shell.scss',
  templateUrl: './shell.html',
})
export class ShellComponent {
  private readonly auth = inject(AuthService);

  protected readonly user = this.auth.user;

  protected readonly roleLabel = computed(() => {
    const role = this.auth.role();
    return role === null ? '' : ROLE_LABELS[role];
  });

  protected readonly navigation = computed<readonly NavItem[]>(() => {
    const role = this.auth.role();
    return role === null ? [] : NAVIGATION[role];
  });

  protected async signOut(): Promise<void> {
    await this.auth.logout();
  }
}

const ROLE_LABELS: Readonly<Record<UserRole, string>> = {
  [UserRole.Student]: 'Ученик',
  [UserRole.Teacher]: 'Учител',
  [UserRole.Parent]: 'Родител',
  [UserRole.Admin]: 'Администратор',
};

/**
 * Menu entries per role. Most point at placeholders until the phase that builds them lands,
 * so the shape of the product is visible from the start.
 */
const NAVIGATION: Readonly<Record<UserRole, readonly NavItem[]>> = {
  [UserRole.Student]: [{ label: 'Начало', route: '/student' }],
  [UserRole.Teacher]: [{ label: 'Начало', route: '/teacher' }],
  [UserRole.Parent]: [{ label: 'Начало', route: '/parent' }],
  [UserRole.Admin]: [
    { label: 'Начало', route: '/admin' },
    { label: 'Потребители', route: '/admin/users' },
    { label: 'Състояние', route: '/admin/system' },
  ],
};
