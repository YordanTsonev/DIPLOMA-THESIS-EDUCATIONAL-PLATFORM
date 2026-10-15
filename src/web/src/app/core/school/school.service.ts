import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { PagedResult, User, UserRole } from '../auth/auth.models';
import {
  AcademicYear,
  CalendarEvent,
  ScheduleSlot,
  ClassStudent,
  SchoolClass,
  SubjectItem,
  TeachingAssignment,
} from './school.models';

@Injectable({ providedIn: 'root' })
export class SchoolService {
  private readonly http = inject(HttpClient);

  academicYears(): Promise<AcademicYear[]> {
    return firstValueFrom(this.http.get<AcademicYear[]>('/api/v1/academic-years'));
  }

  subjects(includeInactive = false): Promise<SubjectItem[]> {
    const params = new HttpParams().set('includeInactive', includeInactive);
    return firstValueFrom(this.http.get<SubjectItem[]>('/api/v1/subjects', { params }));
  }

  createSubject(name: string, code: string): Promise<SubjectItem> {
    return firstValueFrom(this.http.post<SubjectItem>('/api/v1/subjects', { name, code }));
  }

  setSubjectActive(id: string, isActive: boolean): Promise<SubjectItem> {
    return firstValueFrom(this.http.put<SubjectItem>(`/api/v1/subjects/${id}/active`, { isActive }));
  }

  classes(academicYearId?: string): Promise<SchoolClass[]> {
    let params = new HttpParams();
    if (academicYearId) {
      params = params.set('academicYearId', academicYearId);
    }
    return firstValueFrom(this.http.get<SchoolClass[]>('/api/v1/classes', { params }));
  }

  createClass(
    academicYearId: string,
    grade: number,
    section: string,
    homeroomTeacherId: string | null,
  ): Promise<SchoolClass> {
    return firstValueFrom(
      this.http.post<SchoolClass>('/api/v1/classes', {
        academicYearId,
        grade,
        section,
        homeroomTeacherId,
      }),
    );
  }

  classStudents(schoolClassId: string): Promise<ClassStudent[]> {
    return firstValueFrom(this.http.get<ClassStudent[]>(`/api/v1/classes/${schoolClassId}/students`));
  }

  enrollStudent(schoolClassId: string, studentUserId: string): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`/api/v1/classes/${schoolClassId}/students`, { studentUserId }),
    );
  }

  withdrawStudent(schoolClassId: string, studentUserId: string): Promise<void> {
    return firstValueFrom(
      this.http.delete<void>(`/api/v1/classes/${schoolClassId}/students/${studentUserId}`),
    );
  }

  createAssignment(
    teacherUserId: string,
    subjectId: string,
    schoolClassId: string,
    weeklyHours: number,
  ): Promise<TeachingAssignment> {
    return firstValueFrom(
      this.http.post<TeachingAssignment>('/api/v1/teaching-assignments', {
        teacherUserId,
        subjectId,
        schoolClassId,
        weeklyHours,
      }),
    );
  }

  calendar(from: string, to: string): Promise<CalendarEvent[]> {
    const params = new HttpParams().set('from', from).set('to', to);
    return firstValueFrom(this.http.get<CalendarEvent[]>('/api/v1/calendar', { params }));
  }

  createCalendarEvent(payload: {
    type: string;
    title: string;
    startsOn: string;
    endsOn: string | null;
    schoolClassId: string | null;
    subjectId: string | null;
    period: number | null;
    description: string | null;
  }): Promise<CalendarEvent> {
    return firstValueFrom(this.http.post<CalendarEvent>('/api/v1/calendar', payload));
  }

  deleteCalendarEvent(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/v1/calendar/${id}`));
  }

  mySchedule(): Promise<ScheduleSlot[]> {
    return firstValueFrom(this.http.get<ScheduleSlot[]>('/api/v1/schedule/me'));
  }

  classSchedule(schoolClassId: string): Promise<ScheduleSlot[]> {
    const params = new HttpParams().set('schoolClassId', schoolClassId);
    return firstValueFrom(this.http.get<ScheduleSlot[]>('/api/v1/schedule', { params }));
  }

  addScheduleSlot(
    teachingAssignmentId: string,
    dayOfWeek: string,
    period: number,
    room: string | null,
  ): Promise<ScheduleSlot> {
    return firstValueFrom(
      this.http.post<ScheduleSlot>('/api/v1/schedule', {
        teachingAssignmentId,
        dayOfWeek,
        period,
        room,
      }),
    );
  }

  removeScheduleSlot(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/v1/schedule/${id}`));
  }

  usersByRole(role: UserRole, search = ''): Promise<User[]> {
    let params = new HttpParams().set('role', role).set('pageSize', 100).set('isActive', true);
    if (search.trim() !== '') {
      params = params.set('search', search.trim());
    }

    return firstValueFrom(
      this.http.get<PagedResult<User>>('/api/v1/users', { params }),
    ).then((page) => [...page.items]);
  }

  assignments(schoolClassId?: string): Promise<TeachingAssignment[]> {
    let params = new HttpParams();
    if (schoolClassId) {
      params = params.set('schoolClassId', schoolClassId);
    }
    return firstValueFrom(
      this.http.get<TeachingAssignment[]>('/api/v1/teaching-assignments', { params }),
    );
  }
}
