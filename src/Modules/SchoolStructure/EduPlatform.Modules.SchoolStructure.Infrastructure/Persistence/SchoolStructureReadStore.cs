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
        var query = from schoolClass in dbContext.SchoolClasses.AsNoTracking()
                    join year in dbContext.AcademicYears.AsNoTracking()
                        on schoolClass.AcademicYearId equals year.Id
                    select new ClassRow(
                        schoolClass.Id,
                        schoolClass.AcademicYearId,
                        year.Name,
                        schoolClass.Grade,
                        schoolClass.Section,
                        schoolClass.HomeroomTeacherId,
                        schoolClass.Enrollments.Count(enrollment => enrollment.WithdrawnAt == null));

        if (academicYearId is { } yearId)
        {
            query = query.Where(row => row.AcademicYearId == yearId);
        }

        var rows = await query
            .OrderBy(row => row.Grade)
            .ThenBy(row => row.Section)
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
            .Select(enrollment => new EnrollmentRow(enrollment.StudentUserId, enrollment.EnrolledAt))
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
        var query = from assignment in dbContext.TeachingAssignments.AsNoTracking()
                    join subject in dbContext.Subjects.AsNoTracking()
                        on assignment.SubjectId equals subject.Id
                    join schoolClass in dbContext.SchoolClasses.AsNoTracking()
                        on assignment.SchoolClassId equals schoolClass.Id
                    select new AssignmentRow(
                        assignment.Id,
                        assignment.TeacherUserId,
                        assignment.SubjectId,
                        subject.Name,
                        assignment.SchoolClassId,
                        schoolClass.Grade,
                        schoolClass.Section,
                        assignment.WeeklyHours);

        if (schoolClassId is { } classId)
        {
            query = query.Where(row => row.SchoolClassId == classId);
        }

        if (teacherUserId is { } teacherId)
        {
            query = query.Where(row => row.TeacherUserId == teacherId);
        }

        var rows = await query
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

    private sealed record ClassRow(
        Guid Id,
        Guid AcademicYearId,
        string AcademicYearName,
        int Grade,
        string Section,
        Guid? HomeroomTeacherId,
        int StudentCount);

    private sealed record EnrollmentRow(Guid StudentUserId, DateTimeOffset EnrolledAt);

    private sealed record AssignmentRow(
        Guid Id,
        Guid TeacherUserId,
        Guid SubjectId,
        string SubjectName,
        Guid SchoolClassId,
        int Grade,
        string Section,
        int WeeklyHours);
}
