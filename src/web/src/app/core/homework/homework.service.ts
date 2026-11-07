import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface Submission {
  readonly id: string;
  readonly assignmentId: string;
  readonly studentUserId: string;
  readonly studentName: string;
  readonly text: string;
  readonly submittedAt: string;
  readonly isLate: boolean;
  readonly mark: number | null;
  readonly feedback: string | null;
  readonly reviewedAt: string | null;
  readonly gradeId: string | null;
}

export interface HomeworkAssignment {
  readonly id: string;
  readonly teachingAssignmentId: string;
  readonly subjectName: string;
  readonly schoolClassName: string;
  readonly title: string;
  readonly description: string | null;
  readonly dueOn: string;
  readonly isGraded: boolean;
  readonly isOverdue: boolean;
  readonly submissionCount: number;
  readonly reviewedCount: number;
  readonly studentCount: number;
}

export interface StudentAssignment {
  readonly id: string;
  readonly teachingAssignmentId: string;
  readonly subjectName: string;
  readonly title: string;
  readonly description: string | null;
  readonly dueOn: string;
  readonly isGraded: boolean;
  readonly isOverdue: boolean;
  readonly submission: Submission | null;
}

@Injectable({ providedIn: 'root' })
export class HomeworkService {
  private readonly http = inject(HttpClient);

  assignments(): Promise<HomeworkAssignment[]> {
    return firstValueFrom(this.http.get<HomeworkAssignment[]>('/api/v1/homework'));
  }

  mine(): Promise<StudentAssignment[]> {
    return firstValueFrom(this.http.get<StudentAssignment[]>('/api/v1/homework/me'));
  }

  submissions(assignmentId: string): Promise<Submission[]> {
    return firstValueFrom(
      this.http.get<Submission[]>(`/api/v1/homework/${assignmentId}/submissions`),
    );
  }

  create(payload: {
    teachingAssignmentId: string;
    title: string;
    description: string | null;
    dueOn: string;
    isGraded: boolean;
  }): Promise<HomeworkAssignment> {
    return firstValueFrom(this.http.post<HomeworkAssignment>('/api/v1/homework', payload));
  }

  remove(assignmentId: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/v1/homework/${assignmentId}`));
  }

  submit(assignmentId: string, text: string): Promise<Submission> {
    return firstValueFrom(
      this.http.post<Submission>(`/api/v1/homework/${assignmentId}/submissions`, { text }),
    );
  }

  review(submissionId: string, mark: number | null, feedback: string | null): Promise<Submission> {
    return firstValueFrom(
      this.http.put<Submission>(`/api/v1/homework/submissions/${submissionId}/review`, {
        mark,
        feedback,
      }),
    );
  }
}
