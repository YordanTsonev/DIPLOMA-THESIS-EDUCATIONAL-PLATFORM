import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  AssessmentService,
  Attempt,
  Exam,
  QUESTION_KIND_LABELS,
  Question,
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

  protected readonly assignments = signal<readonly TeachingAssignment[]>([]);
  protected readonly exams = signal<readonly Exam[]>([]);
  protected readonly selected = signal<Exam | null>(null);
  protected readonly pool = signal<readonly Question[]>([]);
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

  protected readonly addForm = this.formBuilder.nonNullable.group({
    questionId: ['', Validators.required],
    points: [1, [Validators.required, Validators.min(1), Validators.max(100)]],
  });

  protected readonly reviewForm = this.formBuilder.nonNullable.group({
    questionId: [''],
    points: [0, Validators.required],
    feedback: ['', Validators.maxLength(1000)],
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

      const assignment = this.assignments().find((item) => item.id === full.teachingAssignmentId);
      this.pool.set(assignment ? await this.assessment.questions(assignment.subjectId) : []);
      this.attempts.set(full.isPublished ? await this.assessment.attempts(full.id) : []);
    } catch (error) {
      this.error.set(describeError(error));
    }
  }

  protected available(): readonly Question[] {
    const used = new Set(this.selected()?.items.map((item) => item.questionId) ?? []);
    return this.pool().filter((question) => !used.has(question.id));
  }

  protected async addItem(): Promise<void> {
    const exam = this.selected();

    if (!exam || this.addForm.invalid || this.busy()) {
      this.addForm.markAllAsTouched();
      return;
    }

    await this.run(async () => {
      const { questionId, points } = this.addForm.getRawValue();
      this.selected.set(await this.assessment.addItem(exam.id, questionId, points));
      this.addForm.patchValue({ questionId: '' });
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

  protected async release(attempt: Attempt): Promise<void> {
    await this.run(async () => {
      const released = await this.assessment.release(attempt.id, null);
      this.openAttempt.set(released);
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
