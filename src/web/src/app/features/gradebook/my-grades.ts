import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { describeError } from '../../core/auth/problem-details';
import { GradebookService, StudentReport } from '../../core/gradebook/gradebook.service';

@Component({
  selector: 'app-my-grades',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe],
  styleUrl: './my-grades.scss',
  templateUrl: './my-grades.html',
})
export class MyGradesComponent implements OnInit {
  private readonly gradebook = inject(GradebookService);

  protected readonly report = signal<StudentReport | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

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
}
