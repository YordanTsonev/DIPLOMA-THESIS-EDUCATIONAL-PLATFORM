import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface Grade {
  readonly id: string;
  readonly studentUserId: string;
  readonly studentName: string;
  readonly teachingAssignmentId: string;
  readonly subjectName: string;
  readonly value: number;
  readonly kind: string;
  readonly reason: string;
  readonly comment: string | null;
  readonly givenAt: string;
}

export interface StudentRow {
  readonly studentUserId: string;
  readonly studentName: string;
  readonly grades: readonly Grade[];
  readonly average: number | null;
  readonly unexcusedAbsences: number;
  readonly excusedAbsences: number;
  readonly lateArrivals: number;
}

export interface ClassGradebook {
  readonly teachingAssignmentId: string;
  readonly subjectName: string;
  readonly schoolClassName: string;
  readonly students: readonly StudentRow[];
}

export interface SubjectPerformance {
  readonly teachingAssignmentId: string;
  readonly subjectName: string;
  readonly grades: readonly Grade[];
  readonly average: number | null;
  readonly unexcusedAbsences: number;
  readonly excusedAbsences: number;
  readonly lateArrivals: number;
}

export interface StudentReport {
  readonly studentUserId: string;
  readonly studentName: string;
  readonly subjects: readonly SubjectPerformance[];
  readonly overallAverage: number | null;
}

export const GRADE_REASONS = [
  { value: 'Oral', label: 'Устно изпитване' },
  { value: 'Written', label: 'Писмена работа' },
  { value: 'Test', label: 'Тест' },
  { value: 'Homework', label: 'Домашна работа' },
  { value: 'Project', label: 'Проект' },
  { value: 'Activity', label: 'Активност' },
];

export const GRADE_REASON_LABELS: Readonly<Record<string, string>> = Object.fromEntries(
  GRADE_REASONS.map((reason) => [reason.value, reason.label]),
);

export const ABSENCE_KINDS = [
  { value: 'Unexcused', label: 'Неизвинено' },
  { value: 'Excused', label: 'Извинено' },
  { value: 'Late', label: 'Закъснение' },
];

@Injectable({ providedIn: 'root' })
export class GradebookService {
  private readonly http = inject(HttpClient);

  classGradebook(teachingAssignmentId: string): Promise<ClassGradebook> {
    return firstValueFrom(
      this.http.get<ClassGradebook>(`/api/v1/gradebook/class/${teachingAssignmentId}`),
    );
  }

  myReport(): Promise<StudentReport> {
    return firstValueFrom(this.http.get<StudentReport>('/api/v1/gradebook/me'));
  }

  giveGrade(payload: {
    studentUserId: string;
    teachingAssignmentId: string;
    value: number;
    kind: string;
    reason: string;
    comment: string | null;
  }): Promise<Grade> {
    return firstValueFrom(this.http.post<Grade>('/api/v1/gradebook/grades', payload));
  }

  deleteGrade(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/v1/gradebook/grades/${id}`));
  }

  recordAbsence(payload: {
    studentUserId: string;
    teachingAssignmentId: string;
    date: string;
    period: number;
    kind: string;
    note: string | null;
  }): Promise<unknown> {
    return firstValueFrom(this.http.post('/api/v1/gradebook/absences', payload));
  }
}
