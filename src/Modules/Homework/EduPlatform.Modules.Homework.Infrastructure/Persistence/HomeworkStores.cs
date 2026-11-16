using EduPlatform.Modules.Assessment.Contracts;
using EduPlatform.Modules.Homework.Application.Abstractions;
using EduPlatform.Modules.Homework.Contracts;
using EduPlatform.Modules.Homework.Domain;
using EduPlatform.Modules.Identity.Contracts;
using EduPlatform.Modules.SchoolStructure.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Modules.Homework.Infrastructure.Persistence;

internal sealed class HomeworkRepository(HomeworkDbContext dbContext) : IHomeworkRepository
{
    public Task<Assignment?> FindAssignmentAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Assignments.SingleOrDefaultAsync(assignment => assignment.Id == id, cancellationToken);

    public Task<Submission?> FindSubmissionAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Submissions.SingleOrDefaultAsync(submission => submission.Id == id, cancellationToken);

    public Task<Submission?> FindSubmissionOfStudentAsync(
        Guid assignmentId,
        Guid studentUserId,
        CancellationToken cancellationToken = default) =>
        dbContext.Submissions.SingleOrDefaultAsync(
            submission => submission.AssignmentId == assignmentId && submission.StudentUserId == studentUserId,
            cancellationToken);

    public void AddAssignment(Assignment assignment) => dbContext.Assignments.Add(assignment);

    public void RemoveAssignment(Assignment assignment) => dbContext.Assignments.Remove(assignment);

    public void AddSubmission(Submission submission) => dbContext.Submissions.Add(submission);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

internal sealed class HomeworkReadStore(
    HomeworkDbContext dbContext,
    IUserDirectory users,
    ISchoolDirectory school,
    IExamDirectory exams) : IHomeworkReadStore, IHomeworkDirectory
{
    public Task<bool> ExamIsUsedAsync(Guid examId, CancellationToken cancellationToken = default) =>
        dbContext.Assignments
            .AsNoTracking()
            .AnyAsync(assignment => assignment.ExamId == examId, cancellationToken);

    public async Task<IReadOnlyList<AssignmentDto>> ListAssignmentsAsync(
        IReadOnlyCollection<Guid> teachingAssignmentIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(teachingAssignmentIds);

        if (teachingAssignmentIds.Count == 0)
        {
            return [];
        }

        var rows = await dbContext.Assignments
            .AsNoTracking()
            .Where(assignment => teachingAssignmentIds.Contains(assignment.TeachingAssignmentId))
            .OrderByDescending(assignment => assignment.DueOn)
            .Select(assignment => new
            {
                assignment.Id,
                assignment.TeachingAssignmentId,
                assignment.Title,
                assignment.Description,
                assignment.DueOn,
                assignment.IsGraded,
                assignment.FileName,
                assignment.SizeBytes,
                assignment.ExamId,
                Submissions = dbContext.Submissions.Count(item => item.AssignmentId == assignment.Id),
                Reviewed = dbContext.Submissions
                    .Count(item => item.AssignmentId == assignment.Id && item.ReviewedAt != null),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = new List<AssignmentDto>(rows.Count);

        foreach (var row in rows)
        {
            var teaching = await school.FindAssignmentAsync(row.TeachingAssignmentId, cancellationToken)
                .ConfigureAwait(false);

            var students = teaching is null
                ? []
                : await school.ListClassStudentsAsync(teaching.SchoolClassId, cancellationToken)
                    .ConfigureAwait(false);

            var exam = row.ExamId is { } examId
                ? await exams.FindExamSummaryAsync(examId, cancellationToken).ConfigureAwait(false)
                : null;

            result.Add(new AssignmentDto(
                row.Id,
                row.TeachingAssignmentId,
                teaching?.SubjectName ?? string.Empty,
                teaching?.SchoolClassName ?? string.Empty,
                row.Title,
                row.Description,
                row.DueOn,
                row.IsGraded,
                row.DueOn < today,
                row.Submissions,
                row.Reviewed,
                students.Count,
                row.FileName,
                row.SizeBytes,
                row.ExamId,
                exam?.Title,
                exam?.QuestionCount ?? 0,
                exam?.DurationMinutes ?? 0));
        }

        return result;
    }

    public Task<IReadOnlyList<SubmissionDto>> ListSubmissionsAsync(
        Guid assignmentId,
        CancellationToken cancellationToken = default) =>
        MapAsync(
            dbContext.Submissions.AsNoTracking().Where(submission => submission.AssignmentId == assignmentId),
            cancellationToken);

    public async Task<SubmissionDto?> FindSubmissionAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var mapped = await MapAsync(
                dbContext.Submissions.AsNoTracking().Where(submission => submission.Id == id),
                cancellationToken)
            .ConfigureAwait(false);

        return mapped.SingleOrDefault();
    }

    public async Task<IReadOnlyList<StudentAssignmentDto>> ListForStudentAsync(
        IReadOnlyCollection<Guid> teachingAssignmentIds,
        Guid studentUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(teachingAssignmentIds);

        if (teachingAssignmentIds.Count == 0)
        {
            return [];
        }

        var rows = await dbContext.Assignments
            .AsNoTracking()
            .Where(assignment => teachingAssignmentIds.Contains(assignment.TeachingAssignmentId))
            .OrderByDescending(assignment => assignment.DueOn)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (rows.Count == 0)
        {
            return [];
        }

        var own = await MapAsync(
                dbContext.Submissions.AsNoTracking().Where(submission => submission.StudentUserId == studentUserId),
                cancellationToken)
            .ConfigureAwait(false);

        var byAssignment = own.ToDictionary(submission => submission.AssignmentId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = new List<StudentAssignmentDto>(rows.Count);

        foreach (var row in rows)
        {
            var teaching = await school.FindAssignmentAsync(row.TeachingAssignmentId, cancellationToken)
                .ConfigureAwait(false);

            var exam = row.ExamId is { } examId
                ? await exams.FindExamSummaryAsync(examId, cancellationToken).ConfigureAwait(false)
                : null;

            var attempt = row.ExamId is { } takenId
                ? await exams.FindAttemptAsync(takenId, studentUserId, cancellationToken).ConfigureAwait(false)
                : null;

            result.Add(new StudentAssignmentDto(
                row.Id,
                row.TeachingAssignmentId,
                teaching?.SubjectName ?? string.Empty,
                row.Title,
                row.Description,
                row.DueOn,
                row.IsGraded,
                row.DueOn < today,
                byAssignment.GetValueOrDefault(row.Id),
                row.FileName,
                row.SizeBytes,
                row.ExamId,
                exam?.Title,
                exam?.QuestionCount ?? 0,
                exam?.DurationMinutes ?? 0,
                attempt?.IsSubmitted ?? false,
                attempt?.Score,
                attempt?.MaxScore ?? exam?.MaxScore ?? 0,
                attempt?.Mark,
                attempt?.IsReleased ?? false));
        }

        return result;
    }

    private async Task<IReadOnlyList<SubmissionDto>> MapAsync(
        IQueryable<Submission> query,
        CancellationToken cancellationToken)
    {
        var rows = await query
            .OrderByDescending(submission => submission.SubmittedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (rows.Count == 0)
        {
            return [];
        }

        var ids = rows.Select(submission => submission.StudentUserId).Distinct().ToArray();
        var people = await users.FindByIdsAsync(ids, cancellationToken).ConfigureAwait(false);
        var names = people.ToDictionary(person => person.Id, person => person.FullName);

        return [.. rows.Select(submission => new SubmissionDto(
            submission.Id,
            submission.AssignmentId,
            submission.StudentUserId,
            names.GetValueOrDefault(submission.StudentUserId, string.Empty),
            submission.Text,
            submission.SubmittedAt,
            submission.IsLate,
            submission.Mark,
            submission.Feedback,
            submission.ReviewedAt,
            submission.GradeId,
            submission.FileName,
            submission.SizeBytes))];
    }
}
