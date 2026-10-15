using EduPlatform.Modules.Identity.Contracts;
using EduPlatform.Modules.SchoolStructure.Application.Abstractions;
using EduPlatform.Modules.SchoolStructure.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Modules.SchoolStructure.Infrastructure.Persistence;

internal sealed class SchoolStructureReadStore(SchoolStructureDbContext dbContext, IUserDirectory users)
    : ISchoolStructureReadStore
{
    public async Task<IReadOnlyList<AcademicYearDto>> ListAcademicYearsAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.AcademicYears
            .AsNoTracking()
            .OrderByDescending(year => year.StartsOn)
            .Select(year => new AcademicYearDto(year.Id, year.Name, year.StartsOn, year.EndsOn, year.IsCurrent))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<SubjectDto>> ListSubjectsAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Subjects.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(subject => subject.IsActive);
        }

        return await query
            .OrderBy(subject => subject.Name)
            .Select(subject => new SubjectDto(subject.Id, subject.Name, subject.Code, subject.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SchoolClassDto>> ListClassesAsync(
        Guid? academicYearId,
        CancellationToken cancellationToken = default)
    {
        var classes = dbContext.SchoolClasses.AsNoTracking();

        if (academicYearId is { } yearId)
        {
            classes = classes.Where(schoolClass => schoolClass.AcademicYearId == yearId);
        }

        var rows = await classes
            .OrderBy(schoolClass => schoolClass.Grade)
            .ThenBy(schoolClass => schoolClass.Section)
            .Join(
                dbContext.AcademicYears.AsNoTracking(),
                schoolClass => schoolClass.AcademicYearId,
                year => year.Id,
                (schoolClass, year) => new
                {
                    schoolClass.Id,
                    schoolClass.AcademicYearId,
                    AcademicYearName = year.Name,
                    schoolClass.Grade,
                    schoolClass.Section,
                    schoolClass.HomeroomTeacherId,
                    StudentCount = schoolClass.Enrollments.Count(enrollment => enrollment.WithdrawnAt == null),
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var teacherNames = await ResolveNamesAsync(
            rows.Where(row => row.HomeroomTeacherId is not null).Select(row => row.HomeroomTeacherId!.Value),
            cancellationToken).ConfigureAwait(false);

        return rows.Select(row => new SchoolClassDto(
            row.Id,
            row.AcademicYearId,
            row.AcademicYearName,
            row.Grade,
            row.Section,
            row.Grade + row.Section,
            row.HomeroomTeacherId,
            row.HomeroomTeacherId is { } id && teacherNames.TryGetValue(id, out var name) ? name : null,
            row.StudentCount)).ToList();
    }

    public async Task<SchoolClassDto?> FindClassAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var classes = await ListClassesAsync(null, cancellationToken).ConfigureAwait(false);
        return classes.SingleOrDefault(schoolClass => schoolClass.Id == id);
    }

    public async Task<IReadOnlyList<ClassStudentDto>> ListClassStudentsAsync(
        Guid schoolClassId,
        CancellationToken cancellationToken = default)
    {
        var enrollments = await dbContext.ClassEnrollments
            .AsNoTracking()
            .Where(enrollment => enrollment.SchoolClassId == schoolClassId && enrollment.WithdrawnAt == null)
            .Select(enrollment => new { enrollment.StudentUserId, enrollment.EnrolledAt })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (enrollments.Count == 0)
        {
            return [];
        }

        var students = await users
            .FindByIdsAsync(enrollments.Select(enrollment => enrollment.StudentUserId).ToList(), cancellationToken)
            .ConfigureAwait(false);

        var byId = students.ToDictionary(student => student.Id);

        return enrollments
            .Where(enrollment => byId.ContainsKey(enrollment.StudentUserId))
            .Select(enrollment => new ClassStudentDto(
                enrollment.StudentUserId,
                byId[enrollment.StudentUserId].FullName,
                byId[enrollment.StudentUserId].Email,
                enrollment.EnrolledAt))
            .OrderBy(student => student.FullName, StringComparer.CurrentCulture)
            .ToList();
    }

    public async Task<IReadOnlyList<TeachingAssignmentDto>> ListAssignmentsAsync(
        Guid? schoolClassId,
        Guid? teacherUserId,
        CancellationToken cancellationToken = default)
    {
        var assignments = dbContext.TeachingAssignments.AsNoTracking();

        if (schoolClassId is { } classId)
        {
            assignments = assignments.Where(assignment => assignment.SchoolClassId == classId);
        }

        if (teacherUserId is { } teacherId)
        {
            assignments = assignments.Where(assignment => assignment.TeacherUserId == teacherId);
        }

        var rows = await assignments
            .Join(
                dbContext.Subjects.AsNoTracking(),
                assignment => assignment.SubjectId,
                subject => subject.Id,
                (assignment, subject) => new { assignment, SubjectName = subject.Name })
            .Join(
                dbContext.SchoolClasses.AsNoTracking(),
                row => row.assignment.SchoolClassId,
                schoolClass => schoolClass.Id,
                (row, schoolClass) => new
                {
                    row.assignment.Id,
                    row.assignment.TeacherUserId,
                    row.assignment.SubjectId,
                    row.SubjectName,
                    row.assignment.SchoolClassId,
                    schoolClass.Grade,
                    schoolClass.Section,
                    row.assignment.WeeklyHours,
                })
            .OrderBy(row => row.Grade)
            .ThenBy(row => row.Section)
            .ThenBy(row => row.SubjectName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var teacherNames = await ResolveNamesAsync(
            rows.Select(row => row.TeacherUserId), cancellationToken).ConfigureAwait(false);

        return rows.Select(row => new TeachingAssignmentDto(
            row.Id,
            row.TeacherUserId,
            teacherNames.TryGetValue(row.TeacherUserId, out var name) ? name : "-",
            row.SubjectId,
            row.SubjectName,
            row.SchoolClassId,
            row.Grade + row.Section,
            row.WeeklyHours)).ToList();
    }

    public async Task<IReadOnlyList<ScheduleSlotDto>> ListScheduleAsync(
        Guid? schoolClassId,
        Guid? teacherUserId,
        CancellationToken cancellationToken = default)
    {
        var slots = dbContext.ScheduleSlots.AsNoTracking();

        var rows = await slots
            .Join(
                dbContext.TeachingAssignments.AsNoTracking(),
                slot => slot.TeachingAssignmentId,
                assignment => assignment.Id,
                (slot, assignment) => new { slot, assignment })
            .Where(row => schoolClassId == null || row.assignment.SchoolClassId == schoolClassId)
            .Where(row => teacherUserId == null || row.assignment.TeacherUserId == teacherUserId)
            .Join(
                dbContext.Subjects.AsNoTracking(),
                row => row.assignment.SubjectId,
                subject => subject.Id,
                (row, subject) => new { row.slot, row.assignment, SubjectName = subject.Name })
            .Join(
                dbContext.SchoolClasses.AsNoTracking(),
                row => row.assignment.SchoolClassId,
                schoolClass => schoolClass.Id,
                (row, schoolClass) => new
                {
                    row.slot.Id,
                    row.slot.TeachingAssignmentId,
                    row.slot.DayOfWeek,
                    row.slot.Period,
                    row.slot.StartsAt,
                    row.slot.EndsAt,
                    row.slot.Room,
                    row.assignment.SubjectId,
                    row.SubjectName,
                    row.assignment.SchoolClassId,
                    schoolClass.Grade,
                    schoolClass.Section,
                    row.assignment.TeacherUserId,
                })
            .OrderBy(row => row.DayOfWeek)
            .ThenBy(row => row.Period)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var teacherNames = await ResolveNamesAsync(
            rows.Select(row => row.TeacherUserId), cancellationToken).ConfigureAwait(false);

        return rows.Select(row => new ScheduleSlotDto(
            row.Id,
            row.TeachingAssignmentId,
            row.DayOfWeek,
            row.Period,
            row.StartsAt,
            row.EndsAt,
            row.Room,
            row.SubjectId,
            row.SubjectName,
            row.SchoolClassId,
            row.Grade + row.Section,
            row.TeacherUserId,
            teacherNames.TryGetValue(row.TeacherUserId, out var name) ? name : "-")).ToList();
    }

    public async Task<Guid?> FindClassOfStudentAsync(
        Guid studentUserId,
        CancellationToken cancellationToken = default)
    {
        var enrollment = await dbContext.ClassEnrollments
            .AsNoTracking()
            .Where(item => item.StudentUserId == studentUserId && item.WithdrawnAt == null)
            .Select(item => new { item.SchoolClassId })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return enrollment?.SchoolClassId;
    }

    public async Task<IReadOnlyList<CalendarEventDto>> ListCalendarAsync(
        DateOnly rangeStart,
        DateOnly rangeEnd,
        Guid? schoolClassId,
        CancellationToken cancellationToken = default)
    {
        var events = dbContext.CalendarEvents
            .AsNoTracking()
            .Where(item => item.StartsOn <= rangeEnd && rangeStart <= item.EndsOn);

        if (schoolClassId is { } classId)
        {
            events = events.Where(item => item.SchoolClassId == null || item.SchoolClassId == classId);
        }

        var rows = await events
            .GroupJoin(
                dbContext.SchoolClasses.AsNoTracking(),
                item => item.SchoolClassId,
                schoolClass => (Guid?)schoolClass.Id,
                (item, classes) => new { item, classes })
            .SelectMany(
                row => row.classes.DefaultIfEmpty(),
                (row, schoolClass) => new { row.item, schoolClass })
            .GroupJoin(
                dbContext.Subjects.AsNoTracking(),
                row => row.item.SubjectId,
                subject => (Guid?)subject.Id,
                (row, subjects) => new { row.item, row.schoolClass, subjects })
            .SelectMany(
                row => row.subjects.DefaultIfEmpty(),
                (row, subject) => new
                {
                    row.item.Id,
                    row.item.Type,
                    row.item.Title,
                    row.item.Description,
                    row.item.StartsOn,
                    row.item.EndsOn,
                    row.item.SchoolClassId,
                    ClassGrade = row.schoolClass == null ? (int?)null : row.schoolClass.Grade,
                    ClassSection = row.schoolClass == null ? null : row.schoolClass.Section,
                    row.item.SubjectId,
                    SubjectName = subject == null ? null : subject.Name,
                    row.item.Period,
                })
            .OrderBy(row => row.StartsOn)
            .ThenBy(row => row.Period)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(row => new CalendarEventDto(
            row.Id,
            row.Type.ToString(),
            row.Title,
            row.Description,
            row.StartsOn,
            row.EndsOn,
            row.SchoolClassId,
            row.ClassGrade is null ? null : row.ClassGrade + row.ClassSection,
            row.SubjectId,
            row.SubjectName,
            row.Period,
            row.SchoolClassId is null)).ToList();
    }

    public async Task<IReadOnlyList<Guid>> ListClassesOfTeacherAsync(
        Guid teacherUserId,
        CancellationToken cancellationToken = default) =>
        await dbContext.TeachingAssignments
            .AsNoTracking()
            .Where(assignment => assignment.TeacherUserId == teacherUserId)
            .Select(assignment => assignment.SchoolClassId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    private async Task<Dictionary<Guid, string>> ResolveNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var distinct = userIds.Distinct().ToList();

        if (distinct.Count == 0)
        {
            return [];
        }

        var found = await users.FindByIdsAsync(distinct, cancellationToken).ConfigureAwait(false);
        return found.ToDictionary(user => user.Id, user => user.FullName);
    }



}
