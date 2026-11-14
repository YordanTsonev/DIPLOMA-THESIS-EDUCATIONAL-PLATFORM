import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { describeError } from '../../core/auth/problem-details';
import {
  GRADE_REASON_LABELS,
  GradebookService,
  StudentReport,
  SubjectPerformance,
} from '../../core/gradebook/gradebook.service';

@Component({
  selector: 'app-my-grades',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, DecimalPipe],
  styleUrl: './my-grades.scss',
  templateUrl: './my-grades.html',
})
export class MyGradesComponent implements OnInit {
  private readonly gradebook = inject(GradebookService);

  protected readonly reasonLabels = GRADE_REASON_LABELS;

  protected readonly report = signal<StudentReport | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly selected = signal<SubjectPerformance | null>(null);

  async ngOnInit(): Promise<void> {
    this.loading.set(true);

    try {
      this.report.set(await this.gradebook.myReport());
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected select(subject: SubjectPerformance): void {
    const current = this.selected();
    this.selected.set(
      current?.teachingAssignmentId === subject.teachingAssignmentId ? null : subject,
    );
  }
}
