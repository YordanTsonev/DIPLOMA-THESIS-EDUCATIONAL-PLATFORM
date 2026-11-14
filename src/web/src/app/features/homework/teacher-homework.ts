import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { describeError } from '../../core/auth/problem-details';
import {
  HomeworkAssignment,
  HomeworkService,
  Submission,
} from '../../core/homework/homework.service';
import { SchoolService } from '../../core/school/school.service';
import { TeachingAssignment } from '../../core/school/school.models';

@Component({
  selector: 'app-teacher-homework',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, DatePipe, DecimalPipe],
  styleUrl: './teacher-homework.scss',
  templateUrl: './teacher-homework.html',
})
export class TeacherHomeworkComponent implements OnInit {
  private readonly homework = inject(HomeworkService);
  private readonly school = inject(SchoolService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly subjects = signal<readonly TeachingAssignment[]>([]);
  protected readonly assignments = signal<readonly HomeworkAssignment[]>([]);
  protected readonly selected = signal<HomeworkAssignment | null>(null);
  protected readonly submissions = signal<readonly Submission[]>([]);

  protected readonly loading = signal(false);
  protected readonly busy = signal(false);
  protected readonly formOpen = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);

  protected readonly chosenFile = signal<File | null>(null);

  protected readonly createForm = this.formBuilder.nonNullable.group({
    teachingAssignmentId: ['', Validators.required],
    title: ['', [Validators.required, Validators.maxLength(160)]],
    description: ['', Validators.maxLength(4000)],
    dueOn: ['', Validators.required],
    isGraded: [true],
  });

  protected readonly reviewForm = this.formBuilder.nonNullable.group({
    submissionId: [''],
    mark: [''],
    feedback: ['', Validators.maxLength(1000)],
  });

  async ngOnInit(): Promise<void> {
    this.loading.set(true);

    try {
      const [subjects, assignments] = await Promise.all([
        this.school.assignments(),
        this.homework.assignments(),
      ]);

      this.subjects.set(subjects);
      this.assignments.set(assignments);

      if (subjects.length > 0) {
        this.createForm.patchValue({ teachingAssignmentId: subjects[0].id });
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

  protected pick(event: Event): void {
    this.chosenFile.set((event.target as HTMLInputElement).files?.[0] ?? null);
  }

  protected async downloadAssignment(assignment: HomeworkAssignment): Promise<void> {
    this.error.set(null);

    try {
      await this.homework.downloadAssignmentFile(assignment);
    } catch (error) {
      this.error.set(describeError(error));
    }
  }

  protected async downloadSubmission(submission: Submission): Promise<void> {
    this.error.set(null);

    try {
      await this.homework.downloadSubmissionFile(submission);
    } catch (error) {
      this.error.set(describeError(error));
    }
  }

  protected async create(): Promise<void> {
    if (this.createForm.invalid || this.busy()) {
      this.createForm.markAllAsTouched();
      return;
    }

    await this.run(async () => {
      const value = this.createForm.getRawValue();

      await this.homework.create(
        {
          teachingAssignmentId: value.teachingAssignmentId,
          title: value.title,
          description: value.description.trim() || null,
          dueOn: value.dueOn,
          isGraded: value.isGraded,
        },
        this.chosenFile(),
      );

      this.createForm.patchValue({ title: '', description: '' });
      this.chosenFile.set(null);
      this.formOpen.set(false);
      this.notice.set('Домашното е зададено.');
    });
  }

  protected async open(assignment: HomeworkAssignment): Promise<void> {
    if (this.selected()?.id === assignment.id) {
      this.selected.set(null);
      this.submissions.set([]);
      return;
    }

    this.selected.set(assignment);
    this.error.set(null);

    try {
      this.submissions.set(await this.homework.submissions(assignment.id));
    } catch (error) {
      this.error.set(describeError(error));
      this.submissions.set([]);
    }
  }

  protected startReview(submission: Submission): void {
    this.reviewForm.setValue({
      submissionId: submission.id,
      mark: submission.mark === null ? '' : `${submission.mark}`,
      feedback: submission.feedback ?? '',
    });
  }

  protected async review(): Promise<void> {
    const { submissionId, mark, feedback } = this.reviewForm.getRawValue();

    if (submissionId === '' || this.busy()) {
      return;
    }

    await this.run(async () => {
      await this.homework.review(
        submissionId,
        mark === '' ? null : Number(mark),
        feedback.trim() || null,
      );
      this.reviewForm.reset({ submissionId: '', mark: '', feedback: '' });
      this.notice.set('Проверката е записана.');
    });
  }

  protected async remove(assignment: HomeworkAssignment): Promise<void> {
    await this.run(async () => {
      await this.homework.remove(assignment.id);
      this.selected.set(null);
      this.submissions.set([]);
      this.notice.set('Домашното е премахнато.');
    });
  }

  private async run(action: () => Promise<void>): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    this.notice.set(null);

    try {
      await action();
      this.assignments.set(await this.homework.assignments());

      const current = this.selected();
      if (current) {
        const refreshed = this.assignments().find((item) => item.id === current.id) ?? null;
        this.selected.set(refreshed);
        this.submissions.set(refreshed ? await this.homework.submissions(refreshed.id) : []);
      }
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.busy.set(false);
    }
  }
}
