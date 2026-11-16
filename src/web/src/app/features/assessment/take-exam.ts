import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { Router } from '@angular/router';
import {
  AssessmentService,
  AttemptSheet,
  SheetQuestion,
} from '../../core/assessment/assessment.service';
import { describeError } from '../../core/auth/problem-details';

const AUTOSAVE_DELAY_MS = 3000;
const FINAL_SAVE_AT_SECONDS = 5;

@Component({
  selector: 'app-take-exam',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './take-exam.scss',
  templateUrl: './take-exam.html',
})
export class TakeExamComponent implements OnInit {
  private readonly assessment = inject(AssessmentService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly examId = input.required<string>();

  protected readonly sheet = signal<AttemptSheet | null>(null);
  protected readonly answers = signal<Record<string, { text: string; selected: string[] }>>({});
  protected readonly secondsLeft = signal(0);
  protected readonly loading = signal(false);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);
  protected readonly savedAt = signal<string | null>(null);

  private readonly dirty = signal(false);
  private pending: ReturnType<typeof setTimeout> | null = null;
  private flushing = false;

  protected readonly clock = computed(() => {
    const total = this.secondsLeft();
    const minutes = Math.floor(total / 60);
    const seconds = total % 60;
    return `${minutes}:${`${seconds}`.padStart(2, '0')}`;
  });

  protected readonly isLow = computed(() => this.secondsLeft() > 0 && this.secondsLeft() <= 120);

  async ngOnInit(): Promise<void> {
    this.loading.set(true);

    try {
      await this.refresh(await this.assessment.start(this.examId()));
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  private async refresh(sheet: AttemptSheet): Promise<void> {
    this.sheet.set(sheet);
    this.secondsLeft.set(sheet.secondsLeft);

    const next: Record<string, { text: string; selected: string[] }> = {};

    for (const question of sheet.questions) {
      next[question.id] = {
        text: question.givenText ?? '',
        selected: [...question.selectedOptionIds],
      };
    }

    this.answers.set(next);
    this.dirty.set(false);

    if (!sheet.isSubmitted) {
      this.tick();
    }
  }

  private tick(): void {
    const timer = setInterval(() => {
      const left = this.secondsLeft() - 1;
      this.secondsLeft.set(Math.max(0, left));

      if (left === FINAL_SAVE_AT_SECONDS) {
        void this.flush();
      }

      if (left <= 0) {
        clearInterval(timer);
        void this.submit(true);
      }
    }, 1000);

    this.destroyRef.onDestroy(() => {
      clearInterval(timer);
      this.cancelPending();
    });
  }

  private touch(): void {
    this.dirty.set(true);
    this.cancelPending();
    this.pending = setTimeout(() => void this.flush(), AUTOSAVE_DELAY_MS);
  }

  private cancelPending(): void {
    if (this.pending) {
      clearTimeout(this.pending);
      this.pending = null;
    }
  }

  private async flush(): Promise<string | null> {
    const sheet = this.sheet();

    if (!sheet || sheet.isSubmitted || !this.dirty() || this.flushing) {
      return null;
    }

    this.cancelPending();
    this.flushing = true;
    this.dirty.set(false);

    try {
      await this.assessment.saveAnswers(sheet.attemptId, this.payload());
      this.savedAt.set(new Date().toLocaleTimeString('bg-BG', { hour: '2-digit', minute: '2-digit' }));
      return null;
    } catch (error) {
      this.dirty.set(true);
      return describeError(error);
    } finally {
      this.flushing = false;
    }
  }

  protected setText(questionId: string, event: Event): void {
    const value = (event.target as HTMLTextAreaElement).value;
    this.answers.set({
      ...this.answers(),
      [questionId]: { ...this.answers()[questionId], text: value },
    });
    this.touch();
  }

  protected toggle(question: SheetQuestion, optionId: string): void {
    const current = this.answers()[question.id] ?? { text: '', selected: [] };
    const has = current.selected.includes(optionId);

    const selected =
      question.kind === 'SingleChoice'
        ? has
          ? []
          : [optionId]
        : has
          ? current.selected.filter((id) => id !== optionId)
          : [...current.selected, optionId];

    this.answers.set({ ...this.answers(), [question.id]: { ...current, selected } });
    this.touch();
  }

  protected isPicked(questionId: string, optionId: string): boolean {
    return this.answers()[questionId]?.selected.includes(optionId) ?? false;
  }

  protected async save(): Promise<void> {
    const sheet = this.sheet();

    if (!sheet || sheet.isSubmitted || this.busy()) {
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    this.notice.set(null);
    this.dirty.set(true);

    const failure = await this.flush();

    if (failure) {
      this.error.set(failure);
    } else {
      this.notice.set('Отговорите са запазени.');
    }

    this.busy.set(false);
  }

  protected async submit(automatic = false): Promise<void> {
    const sheet = this.sheet();

    if (!sheet || sheet.isSubmitted) {
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    this.cancelPending();

    const failure = await this.flush();

    if (failure && !automatic) {
      this.error.set(failure);
      this.busy.set(false);
      return;
    }

    try {
      const submitted = await this.assessment.submit(sheet.attemptId);
      this.sheet.set(submitted);
      this.secondsLeft.set(0);
      this.notice.set(automatic ? 'Времето изтече — тестът е предаден.' : 'Тестът е предаден.');
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.busy.set(false);
    }
  }

  protected async back(): Promise<void> {
    await this.router.navigate(['/student/exams']);
  }

  private payload(): { questionId: string; text: string | null; selectedOptionIds: string[] }[] {
    return Object.entries(this.answers()).map(([questionId, value]) => ({
      questionId,
      text: value.text.trim() || null,
      selectedOptionIds: value.selected,
    }));
  }
}
