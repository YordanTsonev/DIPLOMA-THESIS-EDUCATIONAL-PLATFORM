import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { describeError } from '../../core/auth/problem-details';
import {
  ABSENCE_KINDS,
  ClassGradebook,
  GRADE_REASONS,
  GradebookService,
  StudentRow,
} from '../../core/gradebook/gradebook.service';
import { SchoolService } from '../../core/school/school.service';
import { TeachingAssignment } from '../../core/school/school.models';

@Component({
  selector: 'app-class-gradebook',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, DecimalPipe],
  styleUrl: './class-gradebook.scss',
  templateUrl: './class-gradebook.html',
})
export class ClassGradebookComponent implements OnInit {
  private readonly gradebook = inject(GradebookService);
  private readonly school = inject(SchoolService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly reasons = GRADE_REASONS;
  protected readonly absenceKinds = ABSENCE_KINDS;
  protected readonly periods = [1, 2, 3, 4, 5, 6, 7, 8];

  protected readonly assignments = signal<readonly TeachingAssignment[]>([]);
  protected readonly selectedId = signal<string>('');
  protected readonly view = signal<ClassGradebook | null>(null);
  protected readonly loading = signal(false);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);
  protected readonly activeStudent = signal<StudentRow | null>(null);

  protected readonly gradeForm = this.formBuilder.nonNullable.group({
    value: [5, [Validators.required, Validators.min(2), Validators.max(6)]],
    reason: ['Oral', Validators.required],
    comment: [''],
  });

  protected readonly absenceForm = this.formBuilder.nonNullable.group({
    date: [new Date().toISOString().slice(0, 10), Validators.required],
    period: [1, Validators.required],
    kind: ['Unexcused', Validators.required],
    note: [''],
  });

  async ngOnInit(): Promise<void> {
    this.loading.set(true);
    try {
      const assignments = await this.school.assignments();
      this.assignments.set(assignments);

      if (assignments.length > 0) {
        await this.select(assignments[0].id);
      }
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected async select(teachingAssignmentId: string): Promise<void> {
    this.selectedId.set(teachingAssignmentId);
    this.activeStudent.set(null);
    this.loading.set(true);
    this.error.set(null);

    try {
      this.view.set(await this.gradebook.classGradebook(teachingAssignmentId));
    } catch (error) {
      this.error.set(describeError(error));
      this.view.set(null);
    } finally {
      this.loading.set(false);
    }
  }

  protected openStudent(student: StudentRow): void {
    this.activeStudent.set(this.activeStudent()?.studentUserId === student.studentUserId ? null : student);
  }

  protected async giveGrade(): Promise<void> {
    const student = this.activeStudent();

    if (!student || this.gradeForm.invalid || this.busy()) {
      this.gradeForm.markAllAsTouched();
      return;
    }

    await this.run(async () => {
      const { value, reason, comment } = this.gradeForm.getRawValue();

      await this.gradebook.giveGrade({
        studentUserId: student.studentUserId,
        teachingAssignmentId: this.selectedId(),
        value,
        kind: 'Current',
        reason,
        comment: comment.trim() || null,
      });

      this.gradeForm.reset({ value: 5, reason: 'Oral', comment: '' });
      this.notice.set('Оценката е нанесена.');
    });
  }

  protected async recordAbsence(): Promise<void> {
    const student = this.activeStudent();

    if (!student || this.absenceForm.invalid || this.busy()) {
      this.absenceForm.markAllAsTouched();
      return;
    }

    await this.run(async () => {
      const { date, period, kind, note } = this.absenceForm.getRawValue();

      await this.gradebook.recordAbsence({
        studentUserId: student.studentUserId,
        teachingAssignmentId: this.selectedId(),
        date,
        period,
        kind,
        note: note.trim() || null,
      });

      this.notice.set('Отсъствието е записано.');
    });
  }

  protected async deleteGrade(gradeId: string): Promise<void> {
    await this.run(async () => {
      await this.gradebook.deleteGrade(gradeId);
      this.notice.set('Оценката е премахната.');
    });
  }

  private async run(action: () => Promise<void>): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    this.notice.set(null);

    try {
      await action();
      const current = this.activeStudent()?.studentUserId;
      await this.select(this.selectedId());

      if (current) {
        const refreshed = this.view()?.students.find((item) => item.studentUserId === current);
        this.activeStudent.set(refreshed ?? null);
      }
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.busy.set(false);
    }
  }
}
