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
  readonly fileName: string | null;
  readonly sizeBytes: number | null;
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
  readonly fileName: string | null;
  readonly sizeBytes: number | null;
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
  readonly fileName: string | null;
  readonly sizeBytes: number | null;
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

  create(
    payload: {
      teachingAssignmentId: string;
      title: string;
      description: string | null;
      dueOn: string;
      isGraded: boolean;
    },
    file: File | null,
  ): Promise<HomeworkAssignment> {
    const form = new FormData();
    form.append('teachingAssignmentId', payload.teachingAssignmentId);
    form.append('title', payload.title);
    form.append('description', payload.description ?? '');
    form.append('dueOn', payload.dueOn);
    form.append('isGraded', `${payload.isGraded}`);

    if (file) {
      form.append('file', file, file.name);
    }

    return firstValueFrom(this.http.post<HomeworkAssignment>('/api/v1/homework', form));
  }

  remove(assignmentId: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/v1/homework/${assignmentId}`));
  }

  submit(assignmentId: string, text: string, file: File | null): Promise<Submission> {
    const form = new FormData();
    form.append('text', text);

    if (file) {
      form.append('file', file, file.name);
    }

    return firstValueFrom(
      this.http.post<Submission>(`/api/v1/homework/${assignmentId}/submissions`, form),
    );
  }

  downloadAssignmentFile(assignment: { id: string; fileName: string | null }): Promise<void> {
    return this.save(`/api/v1/homework/${assignment.id}/file`, assignment.fileName);
  }

  downloadSubmissionFile(submission: { id: string; fileName: string | null }): Promise<void> {
    return this.save(`/api/v1/homework/submissions/${submission.id}/file`, submission.fileName);
  }

  private async save(url: string, fileName: string | null): Promise<void> {
    const blob = await firstValueFrom(this.http.get(url, { responseType: 'blob' }));

    const href = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = href;
    anchor.download = fileName ?? 'attachment';
    anchor.click();
    URL.revokeObjectURL(href);
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
