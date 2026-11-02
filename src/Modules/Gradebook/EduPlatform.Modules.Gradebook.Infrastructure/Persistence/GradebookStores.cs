using EduPlatform.Modules.Gradebook.Application.Abstractions;
using EduPlatform.Modules.Gradebook.Contracts;
using EduPlatform.Modules.Gradebook.Domain;
using EduPlatform.Modules.Identity.Contracts;
using EduPlatform.Modules.SchoolStructure.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Modules.Gradebook.Infrastructure.Persistence;

internal sealed class GradebookRepository(GradebookDbContext dbContext) : IGradebookRepository
{
    public Task<Grade?> FindGradeAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Grades.SingleOrDefaultAsync(grade => grade.Id == id, cancellationToken);

    public Task<Absence?> FindAbsenceAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Absences.SingleOrDefaultAsync(absence => absence.Id == id, cancellationToken);

    public Task<bool> AbsenceExistsAsync(
        Guid studentUserId,
        Guid teachingAssignmentId,
        DateOnly lessonDate,
        int period,
        CancellationToken cancellationToken = default) =>
        dbContext.Absences.AnyAsync(
            absence => absence.StudentUserId == studentUserId
                && absence.TeachingAssignmentId == teachingAssignmentId
                && absence.Date == lessonDate
                && absence.Period == period,
            cancellationToken);

    public void AddGrade(Grade grade) => dbContext.Grades.Add(grade);

    public void RemoveGrade(Grade grade) => dbContext.Grades.Remove(grade);

    public void AddAbsence(Absence absence) => dbContext.Absences.Add(absence);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

internal sealed class GradebookReadStore(
    GradebookDbContext dbContext,
    IUserDirectory users,
    ISchoolDirectory school) : IGradebookReadStore
{
    public Task<IReadOnlyList<GradeDto>> ListGradesAsync(
        Guid teachingAssignmentId,
        CancellationToken cancellationToken = default) =>
        MapGradesAsync(
            dbContext.Grades.AsNoTracking().Where(grade => grade.TeachingAssignmentId == teachingAssignmentId),
            cancellationToken);

    public Task<IReadOnlyList<GradeDto>> ListGradesForStudentAsync(
        Guid studentUserId,
        CancellationToken cancellationToken = default) =>
        MapGradesAsync(
            dbContext.Grades.AsNoTracking().Where(grade => grade.StudentUserId == studentUserId),
            cancellationToken);

    public Task<IReadOnlyList<AbsenceDto>> ListAbsencesAsync(
        Guid teachingAssignmentId,
        CancellationToken cancellationToken = default) =>
        MapAbsencesAsync(
            dbContext.Absences.AsNoTracking().Where(item => item.TeachingAssignmentId == teachingAssignmentId),
            cancellationToken);

    public Task<IReadOnlyList<AbsenceDto>> ListAbsencesForStudentAsync(
        Guid studentUserId,
        CancellationToken cancellationToken = default) =>
        MapAbsencesAsync(
            dbContext.Absences.AsNoTracking().Where(item => item.StudentUserId == studentUserId),
            cancellationToken);

    public async Task<GradeDto?> FindGradeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var mapped = await MapGradesAsync(
            dbContext.Grades.AsNoTracking().Where(grade => grade.Id == id), cancellationToken).ConfigureAwait(false);

        return mapped.SingleOrDefault();
    }

    public async Task<AbsenceDto?> FindAbsenceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var mapped = await MapAbsencesAsync(
            dbContext.Absences.AsNoTracking().Where(item => item.Id == id), cancellationToken).ConfigureAwait(false);

        return mapped.SingleOrDefault();
    }

    private async Task<IReadOnlyList<GradeDto>> MapGradesAsync(
        IQueryable<Grade> query,
        CancellationToken cancellationToken)
    {
        var rows = await query
            .OrderByDescending(grade => grade.GivenAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (rows.Count == 0)
        {
            return [];
        }

        var names = await ResolveStudentNamesAsync(
            rows.Select(grade => grade.StudentUserId), cancellationToken).ConfigureAwait(false);

        var subjects = await ResolveSubjectNamesAsync(
            rows.Select(grade => grade.TeachingAssignmentId), cancellationToken).ConfigureAwait(false);

        return rows.Select(grade => new GradeDto(
            grade.Id,
            grade.StudentUserId,
            names.GetValueOrDefault(grade.StudentUserId, string.Empty),
            grade.TeachingAssignmentId,
            subjects.GetValueOrDefault(grade.TeachingAssignmentId, string.Empty),
            grade.Value,
            grade.Kind.ToString(),
            grade.Reason.ToString(),
            grade.Comment,
            grade.GivenAt)).ToList();
    }

    private async Task<IReadOnlyList<AbsenceDto>> MapAbsencesAsync(
        IQueryable<Absence> query,
        CancellationToken cancellationToken)
    {
        var rows = await query
            .OrderByDescending(item => item.Date)
            .ThenBy(item => item.Period)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (rows.Count == 0)
        {
            return [];
        }

        var names = await ResolveStudentNamesAsync(
            rows.Select(item => item.StudentUserId), cancellationToken).ConfigureAwait(false);

        var subjects = await ResolveSubjectNamesAsync(
            rows.Select(item => item.TeachingAssignmentId), cancellationToken).ConfigureAwait(false);

        return rows.Select(item => new AbsenceDto(
            item.Id,
            item.StudentUserId,
            names.GetValueOrDefault(item.StudentUserId, string.Empty),
            item.TeachingAssignmentId,
            subjects.GetValueOrDefault(item.TeachingAssignmentId, string.Empty),
            item.Date,
            item.Period,
            item.Kind.ToString(),
            item.Note)).ToList();
    }

    private async Task<Dictionary<Guid, string>> ResolveStudentNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var distinct = userIds.Distinct().ToList();
        var found = await users.FindByIdsAsync(distinct, cancellationToken).ConfigureAwait(false);
        return found.ToDictionary(user => user.Id, user => user.FullName);
    }

    private async Task<Dictionary<Guid, string>> ResolveSubjectNamesAsync(
        IEnumerable<Guid> assignmentIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, string>();

        foreach (var id in assignmentIds.Distinct())
        {
            var assignment = await school.FindAssignmentAsync(id, cancellationToken).ConfigureAwait(false);

            if (assignment is not null)
            {
                result[id] = assignment.SubjectName;
            }
        }

        return result;
    }
}
