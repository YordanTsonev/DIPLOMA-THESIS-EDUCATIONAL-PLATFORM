import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { describeError } from '../../core/auth/problem-details';
import { SchoolService } from '../../core/school/school.service';
import {
  AcademicYear,
  ClassStudent,
  SchoolClass,
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

  protected readonly loading = signal(false);
  protected readonly detailLoading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly filter = this.formBuilder.nonNullable.group({
    academicYearId: [''],
  });

  protected readonly createForm = this.formBuilder.nonNullable.group({
    grade: [9, [Validators.required, Validators.min(1), Validators.max(12)]],
    section: ['', [Validators.required, Validators.maxLength(4)]],
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
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
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
      const [students, assignments] = await Promise.all([
        this.school.classStudents(schoolClass.id),
        this.school.assignments(schoolClass.id),
      ]);
      this.students.set(students);
      this.assignments.set(assignments);
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
