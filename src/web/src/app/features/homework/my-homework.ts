import { DatePipe, DecimalPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { describeError } from '../../core/auth/problem-details';
import { HomeworkService, StudentAssignment } from '../../core/homework/homework.service';

@Component({
  selector: 'app-my-homework',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, DatePipe, DecimalPipe],
  styleUrl: './my-homework.scss',
  templateUrl: './my-homework.html',
})
export class MyHomeworkComponent implements OnInit {
  private readonly homework = inject(HomeworkService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly assignments = signal<readonly StudentAssignment[]>([]);
  protected readonly selected = signal<StudentAssignment | null>(null);
  protected readonly loading = signal(false);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);

  protected readonly pending = computed(
    () => this.assignments().filter((item) => item.submission === null && !item.isOverdue).length,
  );

  protected readonly submitForm = this.formBuilder.nonNullable.group({
    text: ['', [Validators.required, Validators.maxLength(8000)]],
  });

  async ngOnInit(): Promise<void> {
    this.loading.set(true);

    try {
      this.assignments.set(await this.homework.mine());
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected status(item: StudentAssignment): string {
    if (item.submission?.reviewedAt) {
      return 'проверено';
    }

    if (item.submission) {
      return item.submission.isLate ? 'предадено със закъснение' : 'предадено';
    }

    return item.isOverdue ? 'просрочено' : 'предстои';
  }

  protected open(item: StudentAssignment): void {
    const current = this.selected();
    this.notice.set(null);
    this.error.set(null);

    if (current?.id === item.id) {
      this.selected.set(null);
      return;
    }

    this.selected.set(item);
    this.submitForm.setValue({ text: item.submission?.text ?? '' });
  }

  protected async submit(): Promise<void> {
    const item = this.selected();

    if (!item || this.submitForm.invalid || this.busy()) {
      this.submitForm.markAllAsTouched();
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    this.notice.set(null);

    try {
      await this.homework.submit(item.id, this.submitForm.getRawValue().text);
      this.assignments.set(await this.homework.mine());
      this.selected.set(this.assignments().find((entry) => entry.id === item.id) ?? null);
      this.notice.set('Домашното е предадено.');
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.busy.set(false);
    }
  }
}
