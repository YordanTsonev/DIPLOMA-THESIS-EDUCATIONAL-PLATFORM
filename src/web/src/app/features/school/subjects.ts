import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { describeError } from '../../core/auth/problem-details';
import { SchoolService } from '../../core/school/school.service';
import { SubjectItem } from '../../core/school/school.models';

@Component({
  selector: 'app-school-subjects',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  styleUrl: './subjects.scss',
  templateUrl: './subjects.html',
})
export class SchoolSubjectsComponent implements OnInit {
  private readonly school = inject(SchoolService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly subjects = signal<SubjectItem[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);

  protected readonly createForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
    code: ['', [Validators.required, Validators.maxLength(16)]],
  });

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  protected async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.subjects.set(await this.school.subjects(true));
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected async create(): Promise<void> {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.error.set(null);
    this.notice.set(null);

    try {
      const { name, code } = this.createForm.getRawValue();
      const created = await this.school.createSubject(name, code);
      this.notice.set(`Добавен предмет ${created.name}.`);
      this.createForm.reset();
      await this.load();
    } catch (error) {
      this.error.set(describeError(error));
    }
  }

  protected async toggle(subject: SubjectItem): Promise<void> {
    this.error.set(null);
    this.notice.set(null);

    try {
      await this.school.setSubjectActive(subject.id, !subject.isActive);
      await this.load();
    } catch (error) {
      this.error.set(describeError(error));
    }
  }
}
