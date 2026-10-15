import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';
import { describeError } from '../../core/auth/problem-details';
import { SchoolService } from '../../core/school/school.service';
import { PERIODS, ScheduleSlot, WEEKDAYS } from '../../core/school/school.models';

interface Cell {
  readonly period: number;
  readonly slots: readonly ScheduleSlot[];
}

interface Column {
  readonly value: string;
  readonly label: string;
  readonly cells: readonly Cell[];
}

@Component({
  selector: 'app-timetable',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './timetable.scss',
  templateUrl: './timetable.html',
})
export class TimetableComponent implements OnInit {
  private readonly school = inject(SchoolService);
  private readonly auth = inject(AuthService);

  protected readonly periods = PERIODS;
  protected readonly slots = signal<ScheduleSlot[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly isTeacher = computed(() => this.auth.role() === 'Teacher');

  protected readonly columns = computed<readonly Column[]>(() =>
    WEEKDAYS.map((day) => ({
      value: day.value,
      label: day.label,
      cells: PERIODS.map((period) => ({
        period,
        slots: this.slots().filter(
          (slot) => slot.dayOfWeek === day.value && slot.period === period,
        ),
      })),
    })),
  );

  protected readonly usedPeriods = computed(() => {
    const used = new Set(this.slots().map((slot) => slot.period));
    return PERIODS.filter((period) => used.has(period));
  });

  protected readonly isEmpty = computed(() => this.slots().length === 0);

  async ngOnInit(): Promise<void> {
    this.loading.set(true);
    try {
      this.slots.set(await this.school.mySchedule());
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected timeOf(period: number): string {
    const slot = this.slots().find((item) => item.period === period);
    return slot ? `${slot.startsAt.slice(0, 5)} – ${slot.endsAt.slice(0, 5)}` : '';
  }

}
