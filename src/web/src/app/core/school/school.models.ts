export interface AcademicYear {
  readonly id: string;
  readonly name: string;
  readonly startsOn: string;
  readonly endsOn: string;
  readonly isCurrent: boolean;
}

export interface SubjectItem {
  readonly id: string;
  readonly name: string;
  readonly code: string;
  readonly isActive: boolean;
}

export interface SchoolClass {
  readonly id: string;
  readonly academicYearId: string;
  readonly academicYearName: string;
  readonly grade: number;
  readonly section: string;
  readonly displayName: string;
  readonly homeroomTeacherId: string | null;
  readonly homeroomTeacherName: string | null;
  readonly studentCount: number;
}

export interface ClassStudent {
  readonly studentUserId: string;
  readonly fullName: string;
  readonly email: string;
  readonly enrolledAt: string;
}

export interface TeachingAssignment {
  readonly id: string;
  readonly teacherUserId: string;
  readonly teacherName: string;
  readonly subjectId: string;
  readonly subjectName: string;
  readonly schoolClassId: string;
  readonly schoolClassName: string;
  readonly weeklyHours: number;
}

export interface ScheduleSlot {
  readonly id: string;
  readonly teachingAssignmentId: string;
  readonly dayOfWeek: string;
  readonly period: number;
  readonly startsAt: string;
  readonly endsAt: string;
  readonly room: string | null;
  readonly subjectId: string;
  readonly subjectName: string;
  readonly schoolClassId: string;
  readonly schoolClassName: string;
  readonly teacherUserId: string;
  readonly teacherName: string;
}

export const WEEKDAYS: ReadonlyArray<{ value: string; label: string }> = [
  { value: 'Monday', label: 'Понеделник' },
  { value: 'Tuesday', label: 'Вторник' },
  { value: 'Wednesday', label: 'Сряда' },
  { value: 'Thursday', label: 'Четвъртък' },
  { value: 'Friday', label: 'Петък' },
];

export const PERIODS: readonly number[] = [1, 2, 3, 4, 5, 6, 7, 8];

export interface CalendarEvent {
  readonly id: string;
  readonly type: string;
  readonly title: string;
  readonly description: string | null;
  readonly startsOn: string;
  readonly endsOn: string;
  readonly schoolClassId: string | null;
  readonly schoolClassName: string | null;
  readonly subjectId: string | null;
  readonly subjectName: string | null;
  readonly period: number | null;
  readonly isSchoolWide: boolean;
}

export const EVENT_LABELS: Readonly<Record<string, string>> = {
  Test: 'Контролно',
  Exam: 'Изпит',
  Deadline: 'Краен срок',
  Event: 'Събитие',
  Holiday: 'Ваканция',
};
