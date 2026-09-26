import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

interface Upcoming {
  readonly title: string;
  readonly phase: string;
}

/**
 * The landing screen for each role.
 *
 * It shows who is signed in and names the features the role will gain, phase by phase. A single
 * component serves all four roles because until Phase 2 there is no role-specific data to show —
 * only a different list of what is coming.
 */
@Component({
  selector: 'app-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe],
  styleUrl: './dashboard.scss',
  templateUrl: './dashboard.html',
})
export class DashboardComponent {
  private readonly auth = inject(AuthService);

  /** Supplied by the route, so each role gets its own copy. */
  readonly audience = input.required<'student' | 'teacher' | 'parent' | 'admin'>();

  protected readonly user = this.auth.user;

  protected readonly greeting = computed(() => {
    const current = this.user();
    return current === null ? 'Добре дошли' : `Здравейте, ${current.firstName}`;
  });

  protected readonly upcoming = computed<readonly Upcoming[]>(() => UPCOMING[this.audience()]);
}

const UPCOMING: Readonly<Record<'student' | 'teacher' | 'parent' | 'admin', readonly Upcoming[]>> = {
  student: [
    { title: 'Седмична програма и календар', phase: 'Етап 2' },
    { title: 'Учебни материали по предмети', phase: 'Етап 3' },
    { title: 'Домашни работи и предаване', phase: 'Етап 4' },
    { title: 'Оценки и успех по предмети', phase: 'Етап 5' },
    { title: 'Онлайн тестове с таймер', phase: 'Етап 6' },
    { title: 'AI асистент по вашите уроци', phase: 'Етап 8' },
  ],
  teacher: [
    { title: 'Класове и преподавани предмети', phase: 'Етап 2' },
    { title: 'Качване на учебни материали', phase: 'Етап 3' },
    { title: 'Задаване и проверка на домашни', phase: 'Етап 4' },
    { title: 'Дневник с оценки и отсъствия', phase: 'Етап 5' },
    { title: 'Конструктор на тестове', phase: 'Етап 6' },
    { title: 'AI генериране на въпроси', phase: 'Етап 8' },
  ],
  parent: [
    { title: 'Данни на вашето дете', phase: 'Етап 2' },
    { title: 'Оценки и отсъствия', phase: 'Етап 5' },
    { title: 'Съобщения с учителите', phase: 'Етап 7' },
    { title: 'AI помощник по трудни теми', phase: 'Етап 8' },
  ],
  admin: [
    { title: 'Класове, предмети, преподаватели', phase: 'Етап 2' },
    { title: 'Справки за училището', phase: 'Етап 5' },
    { title: 'Мониторинг и внедряване', phase: 'Етап 9' },
  ],
};
