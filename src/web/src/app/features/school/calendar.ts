import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';
import { describeError } from '../../core/auth/problem-details';
import { SchoolService } from '../../core/school/school.service';
import { CalendarEvent, EVENT_LABELS } from '../../core/school/school.models';

interface Day {
  readonly date: string;
  readonly dayOfMonth: number;
  readonly inMonth: boolean;
  readonly isToday: boolean;
  readonly events: readonly CalendarEvent[];
}

const MONTHS = [
  'януари', 'февруари', 'март', 'април', 'май', 'юни',
  'юли', 'август', 'септември', 'октомври', 'ноември', 'декември',
];

@Component({
  selector: 'app-calendar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './calendar.scss',
  templateUrl: './calendar.html',
})
export class CalendarComponent implements OnInit {
  private readonly school = inject(SchoolService);
  private readonly auth = inject(AuthService);

  protected readonly weekdayNames = ['пн', 'вт', 'ср', 'чт', 'пт', 'сб', 'нд'];
  protected readonly labels = EVENT_LABELS;

  protected readonly anchor = signal(startOfMonth(new Date()));
  protected readonly events = signal<CalendarEvent[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly selected = signal<Day | null>(null);

  protected readonly monthLabel = computed(() => {
    const date = this.anchor();
    return `${MONTHS[date.getMonth()]} ${date.getFullYear()}`;
  });

  protected readonly weeks = computed<readonly (readonly Day[])[]>(() => {
    const first = this.anchor();
    const today = isoDate(new Date());
    const start = new Date(first);
    start.setDate(1 - mondayIndex(first.getDay()));

    const result: Day[][] = [];

    for (let week = 0; week < 6; week++) {
      const row: Day[] = [];

      for (let day = 0; day < 7; day++) {
        const current = new Date(start);
        current.setDate(start.getDate() + week * 7 + day);
        const iso = isoDate(current);

        row.push({
          date: iso,
          dayOfMonth: current.getDate(),
          inMonth: current.getMonth() === first.getMonth(),
          isToday: iso === today,
          events: this.events().filter((item) => iso >= item.startsOn && iso <= item.endsOn),
        });
      }

      result.push(row);
    }

    return result;
  });

  protected readonly upcoming = computed(() =>
    [...this.events()].sort((a, b) => a.startsOn.localeCompare(b.startsOn)).slice(0, 6),
  );

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const first = this.anchor();
      const from = new Date(first);
      from.setDate(1 - mondayIndex(first.getDay()));
      const to = new Date(from);
      to.setDate(from.getDate() + 41);

      this.events.set(await this.school.calendar(isoDate(from), isoDate(to)));
      this.selected.set(null);
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected async shift(months: number): Promise<void> {
    const next = new Date(this.anchor());
    next.setMonth(next.getMonth() + months);
    this.anchor.set(startOfMonth(next));
    await this.load();
  }

  protected async today(): Promise<void> {
    this.anchor.set(startOfMonth(new Date()));
    await this.load();
  }

  protected select(day: Day): void {
    this.selected.set(day.events.length > 0 ? day : null);
  }

  protected canManage(): boolean {
    const role = this.auth.role();
    return role === 'Admin' || role === 'Teacher';
  }

  protected async remove(item: CalendarEvent): Promise<void> {
    this.error.set(null);
    try {
      await this.school.deleteCalendarEvent(item.id);
      await this.load();
    } catch (error) {
      this.error.set(describeError(error));
    }
  }
}

function startOfMonth(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), 1);
}

function mondayIndex(day: number): number {
  return (day + 6) % 7;
}

function isoDate(date: Date): string {
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}
