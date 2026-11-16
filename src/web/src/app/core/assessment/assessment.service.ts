import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface QuestionOption {
  readonly id: string;
  readonly text: string;
  readonly isCorrect: boolean;
  readonly position: number;
}

export interface Question {
  readonly id: string;
  readonly subjectId: string;
  readonly subjectName: string;
  readonly kind: string;
  readonly text: string;
  readonly explanation: string | null;
  readonly authorUserId: string;
  readonly authorName: string;
  readonly options: readonly QuestionOption[];
}

export interface ExamItem {
  readonly questionId: string;
  readonly points: number;
  readonly position: number;
  readonly question: Question;
}

export interface Exam {
  readonly id: string;
  readonly teachingAssignmentId: string;
  readonly subjectName: string;
  readonly schoolClassName: string;
  readonly title: string;
  readonly description: string | null;
  readonly durationMinutes: number;
  readonly opensAt: string | null;
  readonly closesAt: string | null;
  readonly isPublished: boolean;
  readonly maxScore: number;
  readonly questionCount: number;
  readonly attemptCount: number;
  readonly awaitingReview: number;
  readonly items: readonly ExamItem[];
}

export interface AttemptAnswer {
  readonly questionId: string;
  readonly questionText: string;
  readonly kind: string;
  readonly points: number;
  readonly awardedPoints: number | null;
  readonly feedback: string | null;
  readonly givenText: string | null;
  readonly selectedOptionIds: readonly string[];
  readonly options: readonly QuestionOption[];
}

export interface Attempt {
  readonly id: string;
  readonly examId: string;
  readonly examTitle: string;
  readonly studentUserId: string;
  readonly studentName: string;
  readonly startedAt: string;
  readonly deadline: string;
  readonly submittedAt: string | null;
  readonly score: number | null;
  readonly maxScore: number;
  readonly mark: number | null;
  readonly suggestedMark: number;
  readonly isFullyScored: boolean;
  readonly isReleased: boolean;
  readonly gradeId: string | null;
  readonly answers: readonly AttemptAnswer[];
}

export interface SheetOption {
  readonly id: string;
  readonly text: string;
}

export interface SheetQuestion {
  readonly id: string;
  readonly kind: string;
  readonly text: string;
  readonly points: number;
  readonly options: readonly SheetOption[];
  readonly givenText: string | null;
  readonly selectedOptionIds: readonly string[];
}

export interface AttemptSheet {
  readonly attemptId: string;
  readonly examId: string;
  readonly title: string;
  readonly description: string | null;
  readonly subjectName: string;
  readonly deadline: string;
  readonly secondsLeft: number;
  readonly isSubmitted: boolean;
  readonly questions: readonly SheetQuestion[];
}

export interface ReviewedAnswer {
  readonly questionId: string;
  readonly kind: string;
  readonly questionText: string;
  readonly explanation: string | null;
  readonly points: number;
  readonly awardedPoints: number;
  readonly feedback: string | null;
  readonly givenText: string | null;
  readonly selectedOptionIds: readonly string[];
  readonly options: readonly QuestionOption[];
}

export interface AttemptReview {
  readonly attemptId: string;
  readonly examId: string;
  readonly examTitle: string;
  readonly subjectName: string;
  readonly score: number;
  readonly maxScore: number;
  readonly mark: number;
  readonly releasedAt: string;
  readonly answers: readonly ReviewedAnswer[];
}

export interface StudentExam {
  readonly examId: string;
  readonly title: string;
  readonly subjectName: string;
  readonly durationMinutes: number;
  readonly questionCount: number;
  readonly maxScore: number;
  readonly opensAt: string | null;
  readonly closesAt: string | null;
  readonly isOpen: boolean;
  readonly attemptId: string | null;
  readonly isSubmitted: boolean;
  readonly score: number | null;
  readonly mark: number | null;
  readonly isReleased: boolean;
}

export const QUESTION_KINDS = [
  { value: 'SingleChoice', label: 'Един верен отговор' },
  { value: 'MultipleChoice', label: 'Няколко верни отговора' },
  { value: 'OpenText', label: 'Свободен отговор' },
];

export const QUESTION_KIND_LABELS: Readonly<Record<string, string>> = Object.fromEntries(
  QUESTION_KINDS.map((kind) => [kind.value, kind.label]),
);

const KIND_NUMBERS: Readonly<Record<string, number>> = {
  SingleChoice: 1,
  MultipleChoice: 2,
  OpenText: 3,
};

@Injectable({ providedIn: 'root' })
export class AssessmentService {
  private readonly http = inject(HttpClient);

  questions(subjectId?: string, ownOnly = false): Promise<Question[]> {
    let params = new HttpParams().set('ownOnly', ownOnly);

    if (subjectId) {
      params = params.set('subjectId', subjectId);
    }

    return firstValueFrom(this.http.get<Question[]>('/api/v1/questions', { params }));
  }

  createQuestion(payload: {
    subjectId: string;
    kind: string;
    text: string;
    explanation: string | null;
    options: { text: string; isCorrect: boolean }[];
  }): Promise<Question> {
    return firstValueFrom(
      this.http.post<Question>('/api/v1/questions', {
        subjectId: payload.subjectId,
        kind: KIND_NUMBERS[payload.kind],
        text: payload.text,
        explanation: payload.explanation,
        options: payload.options,
      }),
    );
  }

  deleteQuestion(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/v1/questions/${id}`));
  }

  exams(): Promise<Exam[]> {
    return firstValueFrom(this.http.get<Exam[]>('/api/v1/exams'));
  }

  exam(id: string): Promise<Exam> {
    return firstValueFrom(this.http.get<Exam>(`/api/v1/exams/${id}`));
  }

  createExam(payload: {
    teachingAssignmentId: string;
    title: string;
    description: string | null;
    durationMinutes: number;
  }): Promise<Exam> {
    return firstValueFrom(this.http.post<Exam>('/api/v1/exams', payload));
  }

  addQuestion(
    examId: string,
    payload: {
      kind: string;
      text: string;
      explanation: string | null;
      options: { text: string; isCorrect: boolean }[];
      points: number;
    },
  ): Promise<Exam> {
    return firstValueFrom(
      this.http.post<Exam>(`/api/v1/exams/${examId}/questions`, {
        kind: KIND_NUMBERS[payload.kind],
        text: payload.text,
        explanation: payload.explanation,
        options: payload.options,
        points: payload.points,
      }),
    );
  }

  addItem(examId: string, questionId: string, points: number): Promise<Exam> {
    return firstValueFrom(
      this.http.post<Exam>(`/api/v1/exams/${examId}/items`, { questionId, points }),
    );
  }

  removeItem(examId: string, questionId: string): Promise<Exam> {
    return firstValueFrom(this.http.delete<Exam>(`/api/v1/exams/${examId}/items/${questionId}`));
  }

  publish(examId: string, opensAt: string | null, closesAt: string | null): Promise<Exam> {
    return firstValueFrom(
      this.http.put<Exam>(`/api/v1/exams/${examId}/publish`, { opensAt, closesAt }),
    );
  }

  deleteExam(examId: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/v1/exams/${examId}`));
  }

  attempts(examId: string): Promise<Attempt[]> {
    return firstValueFrom(this.http.get<Attempt[]>(`/api/v1/exams/${examId}/attempts`));
  }

  review(
    attemptId: string,
    questionId: string,
    points: number,
    feedback: string | null,
  ): Promise<Attempt> {
    return firstValueFrom(
      this.http.put<Attempt>(`/api/v1/attempts/${attemptId}/review`, {
        questionId,
        points,
        feedback,
      }),
    );
  }

  release(attemptId: string, mark: number | null): Promise<Attempt> {
    return firstValueFrom(
      this.http.put<Attempt>(`/api/v1/attempts/${attemptId}/release`, { mark }),
    );
  }

  mine(): Promise<StudentExam[]> {
    return firstValueFrom(this.http.get<StudentExam[]>('/api/v1/attempts/me'));
  }

  result(examId: string): Promise<AttemptReview> {
    return firstValueFrom(this.http.get<AttemptReview>(`/api/v1/attempts/result/${examId}`));
  }

  start(examId: string): Promise<AttemptSheet> {
    return firstValueFrom(this.http.post<AttemptSheet>(`/api/v1/attempts/start/${examId}`, {}));
  }

  saveAnswers(
    attemptId: string,
    answers: { questionId: string; text: string | null; selectedOptionIds: string[] }[],
  ): Promise<AttemptSheet> {
    return firstValueFrom(
      this.http.put<AttemptSheet>(`/api/v1/attempts/${attemptId}/answers`, { answers }),
    );
  }

  submit(attemptId: string): Promise<AttemptSheet> {
    return firstValueFrom(this.http.post<AttemptSheet>(`/api/v1/attempts/${attemptId}/submit`, {}));
  }
}
