import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  AssessmentService,
  Attempt,
  Exam,
  QUESTION_KINDS,
  QUESTION_KIND_LABELS,
} from '../../core/assessment/assessment.service';
import { describeError } from '../../core/auth/problem-details';
import { SchoolService } from '../../core/school/school.service';
import { TeachingAssignment } from '../../core/school/school.models';

@Component({
  selector: 'app-exams',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, DatePipe, DecimalPipe],
  styleUrl: './exams.scss',
  templateUrl: './exams.html',
})
export class ExamsComponent implements OnInit {
  private readonly assessment = inject(AssessmentService);
  private readonly school = inject(SchoolService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly kindLabels = QUESTION_KIND_LABELS;
  protected readonly kinds = QUESTION_KINDS;

  protected readonly assignments = signal<readonly TeachingAssignment[]>([]);
  protected readonly exams = signal<readonly Exam[]>([]);
  protected readonly selected = signal<Exam | null>(null);
  protected readonly attempts = signal<readonly Attempt[]>([]);
  protected readonly openAttempt = signal<Attempt | null>(null);

  protected readonly loading = signal(false);
  protected readonly busy = signal(false);
  protected readonly formOpen = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);

  protected readonly createForm = this.formBuilder.nonNullable.group({
    teachingAssignmentId: ['', Validators.required],
    title: ['', [Validators.required, Validators.maxLength(160)]],
    description: ['', Validators.maxLength(2000)],
    durationMinutes: [30, [Validators.required, Validators.min(5), Validators.max(240)]],
  });

  protected readonly questionForm = this.formBuilder.nonNullable.group({
    kind: ['SingleChoice', Validators.required],
    text: ['', [Validators.required, Validators.maxLength(2000)]],
    explanation: ['', Validators.maxLength(1000)],
    points: [1, [Validators.required, Validators.min(1), Validators.max(100)]],
  });

  protected readonly options = signal<readonly { text: string; isCorrect: boolean }[]>([
    { text: '', isCorrect: false },
    { text: '', isCorrect: false },
  ]);

  protected readonly reviewForm = this.formBuilder.nonNullable.group({
    questionId: [''],
    points: [0, Validators.required],
    feedback: ['', Validators.maxLength(1000)],
  });

  protected readonly releaseForm = this.formBuilder.nonNullable.group({
    attemptId: [''],
    mark: [0, [Validators.required, Validators.min(2), Validators.max(6)]],
  });

  async ngOnInit(): Promise<void> {
    this.loading.set(true);

    try {
      const [assignments, exams] = await Promise.all([
        this.school.assignments(),
        this.assessment.exams(),
      ]);

      this.assignments.set(assignments);
      this.exams.set(exams);

      if (assignments.length > 0) {
        this.createForm.patchValue({ teachingAssignmentId: assignments[0].id });
      }
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected toggleForm(): void {
    this.formOpen.set(!this.formOpen());
  }

  protected async create(): Promise<void> {
    if (this.createForm.invalid || this.busy()) {
      this.createForm.markAllAsTouched();
      return;
    }

    await this.run(async () => {
      const value = this.createForm.getRawValue();

      await this.assessment.createExam({
        teachingAssignmentId: value.teachingAssignmentId,
        title: value.title,
        description: value.description.trim() || null,
        durationMinutes: value.durationMinutes,
      });

      this.createForm.patchValue({ title: '', description: '' });
      this.formOpen.set(false);
      this.notice.set('Тестът е създаден. Сега добавете въпроси.');
    });
  }

  protected async open(exam: Exam): Promise<void> {
    if (this.selected()?.id === exam.id) {
      this.selected.set(null);
      this.attempts.set([]);
      this.openAttempt.set(null);
      return;
    }

    this.error.set(null);
    this.openAttempt.set(null);

    try {
      const full = await this.assessment.exam(exam.id);
      this.selected.set(full);

      this.attempts.set(full.isPublished ? await this.assessment.attempts(full.id) : []);
    } catch (error) {
      this.error.set(describeError(error));
    }
  }

  protected get isOpenKind(): boolean {
    return this.questionForm.getRawValue().kind === 'OpenText';
  }

  protected addOption(): void {
    this.options.set([...this.options(), { text: '', isCorrect: false }]);
  }

  protected removeOption(index: number): void {
    this.options.set(this.options().filter((_, position) => position !== index));
  }

  protected setOptionText(index: number, event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.options.set(
      this.options().map((option, position) =>
        position === index ? { ...option, text: value } : option,
      ),
    );
  }

  protected toggleCorrect(index: number): void {
    const single = this.questionForm.getRawValue().kind === 'SingleChoice';

    this.options.set(
      this.options().map((option, position) => {
        if (position === index) {
          return { ...option, isCorrect: !option.isCorrect };
        }

        return single ? { ...option, isCorrect: false } : option;
      }),
    );
  }

  protected async addQuestion(): Promise<void> {
    const exam = this.selected();

    if (!exam || this.questionForm.invalid || this.busy()) {
      this.questionForm.markAllAsTouched();
      return;
    }

    const value = this.questionForm.getRawValue();
    const payloadOptions = this.isOpenKind
      ? []
      : this.options()
          .filter((option) => option.text.trim() !== '')
          .map((option) => ({ text: option.text.trim(), isCorrect: option.isCorrect }));

    await this.run(async () => {
      this.selected.set(
        await this.assessment.addQuestion(exam.id, {
          kind: value.kind,
          text: value.text,
          explanation: value.explanation.trim() || null,
          options: payloadOptions,
          points: value.points,
        }),
      );

      this.questionForm.patchValue({ text: '', explanation: '' });
      this.options.set([
        { text: '', isCorrect: false },
        { text: '', isCorrect: false },
      ]);
      this.notice.set('Въпросът е добавен.');
    });
  }

  protected async removeItem(questionId: string): Promise<void> {
    const exam = this.selected();

    if (!exam) {
      return;
    }

    await this.run(async () => {
      this.selected.set(await this.assessment.removeItem(exam.id, questionId));
    });
  }

  protected async publish(): Promise<void> {
    const exam = this.selected();

    if (!exam) {
      return;
    }

    await this.run(async () => {
      const published = await this.assessment.publish(exam.id, null, null);
      this.selected.set(published);
      this.attempts.set(await this.assessment.attempts(published.id));
      this.notice.set('Тестът е публикуван и вече е видим за учениците.');
    });
  }

  protected async removeExam(exam: Exam): Promise<void> {
    await this.run(async () => {
      await this.assessment.deleteExam(exam.id);
      this.selected.set(null);
      this.attempts.set([]);
      this.notice.set('Тестът е премахнат.');
    });
  }

  protected showAttempt(attempt: Attempt): void {
    this.openAttempt.set(this.openAttempt()?.id === attempt.id ? null : attempt);
    this.reviewForm.reset({ questionId: '', points: 0, feedback: '' });
    this.releaseForm.reset({ attemptId: '', mark: 0 });
  }

  protected startReview(questionId: string, points: number | null, feedback: string | null): void {
    this.reviewForm.setValue({ questionId, points: points ?? 0, feedback: feedback ?? '' });
  }

  protected async review(): Promise<void> {
    const attempt = this.openAttempt();
    const { questionId, points, feedback } = this.reviewForm.getRawValue();

    if (!attempt || questionId === '' || this.busy()) {
      return;
    }

    await this.run(async () => {
      const updated = await this.assessment.review(
        attempt.id,
        questionId,
        points,
        feedback.trim() || null,
      );
      this.openAttempt.set(updated);
      this.reviewForm.reset({ questionId: '', points: 0, feedback: '' });
      this.notice.set('Отговорът е оценен.');
    });
  }

  protected startRelease(attempt: Attempt): void {
    this.releaseForm.setValue({
      attemptId: attempt.id,
      mark: attempt.mark ?? attempt.suggestedMark,
    });
  }

  protected async release(): Promise<void> {
    const attempt = this.openAttempt();
    const { attemptId, mark } = this.releaseForm.getRawValue();

    if (!attempt || attemptId !== attempt.id || this.releaseForm.invalid || this.busy()) {
      this.releaseForm.markAllAsTouched();
      return;
    }

    await this.run(async () => {
      const released = await this.assessment.release(attempt.id, mark);
      this.openAttempt.set(released);
      this.releaseForm.reset({ attemptId: '', mark: 0 });
      this.notice.set(`Резултатът е публикуван с оценка ${released.mark}.`);
    });
  }

  private async run(action: () => Promise<void>): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    this.notice.set(null);

    try {
      await action();
      this.exams.set(await this.assessment.exams());

      const current = this.selected();
      if (current) {
        const refreshed = await this.assessment.exam(current.id);
        this.selected.set(refreshed);

        if (refreshed.isPublished) {
          this.attempts.set(await this.assessment.attempts(refreshed.id));
        }
      }
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.busy.set(false);
    }
  }
}
