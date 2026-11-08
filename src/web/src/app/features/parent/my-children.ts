import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { describeError } from '../../core/auth/problem-details';
import { GRADE_REASON_LABELS, StudentReport } from '../../core/gradebook/gradebook.service';
import { StudentAssignment } from '../../core/homework/homework.service';
import { Child, GuardiansService } from '../../core/school/guardians.service';

@Component({
  selector: 'app-my-children',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe],
  styleUrl: './my-children.scss',
  templateUrl: './my-children.html',
})
export class MyChildrenComponent implements OnInit {
  private readonly guardians = inject(GuardiansService);
  private readonly http = inject(HttpClient);

  protected readonly reasonLabels = GRADE_REASON_LABELS;

  protected readonly children = signal<readonly Child[]>([]);
  protected readonly selected = signal<Child | null>(null);
  protected readonly report = signal<StudentReport | null>(null);
  protected readonly homework = signal<readonly StudentAssignment[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    this.loading.set(true);

    try {
      const children = await this.guardians.myChildren();
      this.children.set(children);

      if (children.length > 0) {
        await this.select(children[0]);
      }
    } catch (error) {
      this.error.set(describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected async select(child: Child): Promise<void> {
    this.selected.set(child);
    this.error.set(null);
    this.loading.set(true);

    try {
      const [report, homework] = await Promise.all([
        firstValueFrom(
          this.http.get<StudentReport>(`/api/v1/gradebook/students/${child.studentUserId}`),
        ),
        firstValueFrom(
          this.http.get<StudentAssignment[]>(`/api/v1/homework/students/${child.studentUserId}`),
        ),
      ]);

      this.report.set(report);
      this.homework.set(homework);
    } catch (error) {
      this.error.set(describeError(error));
      this.report.set(null);
      this.homework.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  protected status(item: StudentAssignment): string {
    if (item.submission?.reviewedAt) {
      return 'проверено';
    }

    if (item.submission) {
      return 'предадено';
    }

    return item.isOverdue ? 'просрочено' : 'предстои';
  }
}
