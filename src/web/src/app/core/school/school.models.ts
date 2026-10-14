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
