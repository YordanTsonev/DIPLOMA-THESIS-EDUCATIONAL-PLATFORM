import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { UserRole } from '../../core/auth/auth.models';
import { AuthService } from '../../core/auth/auth.service';
import { describeError } from '../../core/auth/problem-details';
import { SchoolService } from '../../core/school/school.service';
import {
  CalendarEvent,
  EVENT_LABELS,
  EVENT_TYPES,
  PERIODS,
  SchoolClass,
  SubjectItem,
} from '../../core/school/school.models';

interface Day {
  readonly date: string;
  readonly dayOfMonth: number;
  readonly inMonth: boolean;
  readonly isToday: boolean;
  readonly events: readonly CalendarEvent[];
}

const MONTHS = [
  'януари',
  'февруари',
  'март',
  'април',
  'май',
  'юни',
  'юли',
  'август',
  'септември',
  'октомври',
  'ноември',
  'декември',
];

@Component({
  selector: 'app-calendar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  styleUrl: './calendar.scss',
  templateUrl: './calendar.html',
})
export class CalendarComponent implements OnInit {
  private readonly school = inject(SchoolService);
  private readonly auth = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly weekdayNames = ['пн', 'вт', 'ср', 'чт', 'пт', 'сб', 'нд'];
  protected readonly labels = EVENT_LABELS;
  protected readonly eventTypes = EVENT_TYPES;
  protected readonly periods = PERIODS;

  protected readonly anchor = signal(startOfMonth(new Date()));
  protected readonly events = signal<CalendarEvent[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly selected = signal<Day | null>(null);

  protected readonly canManage = computed(() => {
    const role = this.auth.role();
    return role === UserRole.Admin || role === UserRole.Teacher;
  });

  protected readonly formOpen = signal(false);
  protected readonly saving = signal(false);
  protected readonly classes = signal<SchoolClass[]>([]);
  protected readonly subjects = signal<SubjectItem[]>([]);

  protected readonly createForm = this.formBuilder.nonNullable.group({
    type: ['Event', Validators.required],
    title: ['', [Validators.required, Validators.maxLength(160)]],
    startsOn: [isoDate(new Date()), Validators.required],
    endsOn: [''],
    schoolClassId: [''],
    subjectId: [''],
    period: [''],
    description: ['', Validators.maxLength(1000)],
  });

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

    if (this.formOpen()) {
      this.createForm.patchValue({ startsOn: day.date });
    }
  }

  protected async toggleForm(): Promise<void> {
    const opening = !this.formOpen();
    this.formOpen.set(opening);

    if (!opening || this.classes().length > 0) {
      return;
    }

    try {
      const [classes, subjects] = await Promise.all([
        this.school.classes(),
        this.school.subjects(),
      ]);
      this.classes.set(classes);
      this.subjects.set(subjects);
    } catch (error) {
      this.error.set(describeError(error));
    }
  }

  protected async create(): Promise<void> {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const value = this.createForm.getRawValue();
    this.error.set(null);
    this.saving.set(true);

    try {
      await this.school.createCalendarEvent({
        type: value.type,
        title: value.title,
        startsOn: value.startsOn,
        endsOn: value.endsOn || null,
        schoolClassId: value.schoolClassId || null,
        subjectId: value.subjectId || null,
        period: value.period ? Number(value.period) : null,
        description: value.description || null,
      });

      this.createForm.reset({ type: 'Event', startsOn: value.startsOn });
      this.formOpen.set(false);
      await this.load();
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.saving.set(false);
    }
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
