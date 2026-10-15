import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { User, UserRole } from '../../core/auth/auth.models';
import { describeError } from '../../core/auth/problem-details';
import { SchoolService } from '../../core/school/school.service';
import {
  AcademicYear,
  ClassStudent,
  SchoolClass,
  SubjectItem,
  TeachingAssignment,
} from '../../core/school/school.models';

@Component({
  selector: 'app-school-classes',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, DatePipe],
  styleUrl: './classes.scss',
  templateUrl: './classes.html',
})
export class SchoolClassesComponent implements OnInit {
  private readonly school = inject(SchoolService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly years = signal<AcademicYear[]>([]);
  protected readonly classes = signal<SchoolClass[]>([]);
  protected readonly selected = signal<SchoolClass | null>(null);
  protected readonly students = signal<ClassStudent[]>([]);
  protected readonly assignments = signal<TeachingAssignment[]>([]);

  protected readonly availableStudents = signal<User[]>([]);
  protected readonly teachers = signal<User[]>([]);
  protected readonly subjects = signal<SubjectItem[]>([]);

  protected readonly loading = signal(false);
  protected readonly detailLoading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);

  protected readonly filter = this.formBuilder.nonNullable.group({
    academicYearId: [''],
  });

  protected readonly createForm = this.formBuilder.nonNullable.group({
    grade: [9, [Validators.required, Validators.min(1), Validators.max(12)]],
    section: ['', [Validators.required, Validators.maxLength(4)]],
  });

  protected readonly enrollForm = this.formBuilder.nonNullable.group({
    studentUserId: ['', Validators.required],
  });

  protected readonly assignForm = this.formBuilder.nonNullable.group({
    subjectId: ['', Validators.required],
    teacherUserId: ['', Validators.required],
    weeklyHours: [4, [Validators.required, Validators.min(1), Validators.max(40)]],
  });

  async ngOnInit(): Promise<void> {
    this.loading.set(true);
    try {
      const years = await this.school.academicYears();
      this.years.set(years);

      const current = years.find((year) => year.isCurrent) ?? years.at(0);
      if (current) {
        this.filter.patchValue({ academicYearId: current.id });
      }

      await this.loadClasses();
      await this.loadPickers();
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  private async loadPickers(): Promise<void> {
    const [teachers, subjects] = await Promise.all([
      this.school.usersByRole(UserRole.Teacher),
      this.school.subjects(),
    ]);

    this.teachers.set(teachers);
    this.subjects.set(subjects);
  }

  protected async loadClasses(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const yearId = this.filter.getRawValue().academicYearId;
      this.classes.set(await this.school.classes(yearId || undefined));

      const current = this.selected();
      if (current && !this.classes().some((item) => item.id === current.id)) {
        this.selected.set(null);
        this.students.set([]);
        this.assignments.set([]);
      }
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected async select(schoolClass: SchoolClass): Promise<void> {
    this.selected.set(schoolClass);
    this.detailLoading.set(true);
    this.error.set(null);
    try {
      const [students, assignments, allStudents] = await Promise.all([
        this.school.classStudents(schoolClass.id),
        this.school.assignments(schoolClass.id),
        this.school.usersByRole(UserRole.Student),
      ]);

      this.students.set(students);
      this.assignments.set(assignments);

      const enrolled = new Set(students.map((student) => student.studentUserId));
      this.availableStudents.set(allStudents.filter((student) => !enrolled.has(student.id)));
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.detailLoading.set(false);
    }
  }

  protected async createClass(): Promise<void> {
    const yearId = this.filter.getRawValue().academicYearId;

    if (this.createForm.invalid || !yearId) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.error.set(null);
    try {
      const { grade, section } = this.createForm.getRawValue();
      await this.school.createClass(yearId, grade, section, null);
      this.createForm.patchValue({ section: '' });
      await this.loadClasses();
    } catch (error) {
      this.error.set(describeError(error));
    }
  }

  protected async enroll(): Promise<void> {
    const schoolClass = this.selected();

    if (!schoolClass || this.enrollForm.invalid) {
      this.enrollForm.markAllAsTouched();
      return;
    }

    this.error.set(null);
    this.notice.set(null);

    try {
      await this.school.enrollStudent(schoolClass.id, this.enrollForm.getRawValue().studentUserId);
      this.enrollForm.reset();
      this.notice.set('Ученикът е записан.');
      await this.select(schoolClass);
      await this.loadClasses();
    } catch (error) {
      this.error.set(describeError(error));
    }
  }

  protected async assignSubject(): Promise<void> {
    const schoolClass = this.selected();

    if (!schoolClass || this.assignForm.invalid) {
      this.assignForm.markAllAsTouched();
      return;
    }

    this.error.set(null);
    this.notice.set(null);

    try {
      const { subjectId, teacherUserId, weeklyHours } = this.assignForm.getRawValue();
      await this.school.createAssignment(teacherUserId, subjectId, schoolClass.id, weeklyHours);
      this.assignForm.reset({ weeklyHours: 4 });
      this.notice.set('Предметът е добавен към класа.');
      await this.select(schoolClass);
    } catch (error) {
      this.error.set(describeError(error));
    }
  }

  protected async withdraw(student: ClassStudent): Promise<void> {
    const schoolClass = this.selected();
    if (!schoolClass) {
      return;
    }

    this.error.set(null);
    try {
      await this.school.withdrawStudent(schoolClass.id, student.studentUserId);
      await this.select(schoolClass);
      await this.loadClasses();
    } catch (error) {
      this.error.set(describeError(error));
    }
  }
}
