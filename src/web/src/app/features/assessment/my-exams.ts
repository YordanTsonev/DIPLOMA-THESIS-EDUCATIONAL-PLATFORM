import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AssessmentService, StudentExam } from '../../core/assessment/assessment.service';
import { describeError } from '../../core/auth/problem-details';

@Component({
  selector: 'app-my-exams',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe],
  styleUrl: './my-exams.scss',
  templateUrl: './my-exams.html',
})
export class MyExamsComponent implements OnInit {
  private readonly assessment = inject(AssessmentService);
  private readonly router = inject(Router);

  protected readonly exams = signal<readonly StudentExam[]>([]);
  protected readonly loading = signal(false);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    this.loading.set(true);

    try {
      this.exams.set(await this.assessment.mine());
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected status(exam: StudentExam): string {
    if (exam.isReleased) {
      return 'оценен';
    }

    if (exam.isSubmitted) {
      return 'предаден';
    }

    if (exam.attemptId) {
      return 'започнат';
    }

    return exam.isOpen ? 'отворен' : 'затворен';
  }

  protected async review(exam: StudentExam): Promise<void> {
    await this.router.navigate(['/student/exams', exam.examId, 'result']);
  }

  protected async enter(exam: StudentExam): Promise<void> {
    if (exam.isSubmitted || this.busy()) {
      return;
    }

    if (!exam.isOpen) {
      this.error.set('Този тест не е отворен в момента.');
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    try {
      await this.assessment.start(exam.examId);
      await this.router.navigate(['/student/exams', exam.examId]);
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.busy.set(false);
    }
  }
}
