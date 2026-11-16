import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  AssessmentService,
  AttemptReview,
  QUESTION_KIND_LABELS,
  ReviewedAnswer,
} from '../../core/assessment/assessment.service';
import { describeError } from '../../core/auth/problem-details';

@Component({
  selector: 'app-exam-result',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe],
  styleUrl: './exam-result.scss',
  templateUrl: './exam-result.html',
})
export class ExamResultComponent implements OnInit {
  private readonly assessment = inject(AssessmentService);
  private readonly router = inject(Router);

  readonly examId = input.required<string>();

  protected readonly kindLabels = QUESTION_KIND_LABELS;
  protected readonly review = signal<AttemptReview | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    this.loading.set(true);

    try {
      this.review.set(await this.assessment.result(this.examId()));
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected isFull(answer: ReviewedAnswer): boolean {
    return answer.awardedPoints === answer.points;
  }

  protected isMissed(answer: ReviewedAnswer): boolean {
    return answer.awardedPoints === 0;
  }

  protected picked(answer: ReviewedAnswer, optionId: string): boolean {
    return answer.selectedOptionIds.includes(optionId);
  }

  protected async back(): Promise<void> {
    await this.router.navigate(['/student/exams']);
  }
}
