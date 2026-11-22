import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { describeError } from '../../core/auth/problem-details';
import { Absence, GradebookService } from '../../core/gradebook/gradebook.service';

@Component({
  selector: 'app-homeroom',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  styleUrl: './homeroom.scss',
  templateUrl: './homeroom.html',
})
export class HomeroomComponent implements OnInit {
  private readonly gradebook = inject(GradebookService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly absences = signal<readonly Absence[]>([]);
  protected readonly loading = signal(false);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);
  protected readonly activeId = signal<string | null>(null);
  protected readonly onlyPending = signal(true);

  protected readonly form = this.formBuilder.nonNullable.group({ note: [''] });

  protected readonly visible = computed(() =>
    this.onlyPending()
      ? this.absences().filter((absence) => absence.kind !== 'Excused')
      : this.absences(),
  );

  protected readonly pendingCount = computed(
    () => this.absences().filter((absence) => absence.kind !== 'Excused').length,
  );

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      this.absences.set(await this.gradebook.homeroomAbsences());
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected toggleFilter(): void {
    this.onlyPending.set(!this.onlyPending());
  }

  protected open(absence: Absence): void {
    this.activeId.set(this.activeId() === absence.id ? null : absence.id);
    this.form.reset({ note: absence.note ?? '' });
    this.notice.set(null);
    this.error.set(null);
  }

  protected async excuse(absence: Absence): Promise<void> {
    if (this.busy()) {
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    this.notice.set(null);

    try {
      const note = this.form.getRawValue().note.trim();
      const updated = await this.gradebook.excuseAbsence(absence.id, note || null);

      this.absences.set(
        this.absences().map((item) => (item.id === updated.id ? updated : item)),
      );
      this.activeId.set(null);
      this.notice.set(`Отсъствието на ${updated.studentName} е извинено.`);
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.busy.set(false);
    }
  }

  protected label(kind: string): string {
    if (kind === 'Excused') {
      return 'извинено';
    }

    return kind === 'Late' ? 'закъснение' : 'неизвинено';
  }
}
