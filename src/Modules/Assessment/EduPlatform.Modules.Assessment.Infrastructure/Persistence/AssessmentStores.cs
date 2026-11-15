using EduPlatform.Modules.Assessment.Application.Abstractions;
using EduPlatform.Modules.Assessment.Contracts;
using EduPlatform.Modules.Assessment.Domain;
using EduPlatform.Modules.Identity.Contracts;
using EduPlatform.Modules.SchoolStructure.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Modules.Assessment.Infrastructure.Persistence;

internal sealed class AssessmentRepository(AssessmentDbContext dbContext) : IAssessmentRepository
{
    public Task<Question?> FindQuestionAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Questions.SingleOrDefaultAsync(question => question.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Question>> ListQuestionsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        return ids.Count == 0
            ? []
            : await dbContext.Questions
                .Where(question => ids.Contains(question.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
    }

    public Task<Exam?> FindExamAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Exams.SingleOrDefaultAsync(exam => exam.Id == id, cancellationToken);

    public Task<Attempt?> FindAttemptAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Attempts.SingleOrDefaultAsync(attempt => attempt.Id == id, cancellationToken);

    public Task<Attempt?> FindAttemptOfStudentAsync(
        Guid examId,
        Guid studentUserId,
        CancellationToken cancellationToken = default) =>
        dbContext.Attempts.SingleOrDefaultAsync(
            attempt => attempt.ExamId == examId && attempt.StudentUserId == studentUserId,
            cancellationToken);

    public void AddQuestion(Question question) => dbContext.Questions.Add(question);

    public void RemoveQuestion(Question question) => dbContext.Questions.Remove(question);

    public void AddExam(Exam exam) => dbContext.Exams.Add(exam);

    public void RemoveExam(Exam exam) => dbContext.Exams.Remove(exam);

    public void AddAttempt(Attempt attempt) => dbContext.Attempts.Add(attempt);

    public Task<bool> QuestionIsUsedAsync(Guid questionId, CancellationToken cancellationToken = default) =>
        dbContext.Exams.AnyAsync(exam => exam.Items.Any(item => item.QuestionId == questionId), cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}

internal sealed class AssessmentReadStore(
    AssessmentDbContext dbContext,
    IUserDirectory users,
    ISchoolDirectory school) : IAssessmentReadStore
{
    public async Task<IReadOnlyList<QuestionDto>> ListQuestionsAsync(
        IReadOnlyCollection<Guid> subjectIds,
        Guid? authorUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subjectIds);

        if (subjectIds.Count == 0)
        {
            return [];
        }

        var query = dbContext.Questions
            .AsNoTracking()
            .Where(question => subjectIds.Contains(question.SubjectId));

        if (authorUserId is { } author)
        {
            query = query.Where(question => question.AuthorUserId == author);
        }

        var rows = await query
            .OrderByDescending(question => question.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return await MapQuestionsAsync(rows, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ExamDto>> ListExamsAsync(
        IReadOnlyCollection<Guid> teachingAssignmentIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(teachingAssignmentIds);

        if (teachingAssignmentIds.Count == 0)
        {
            return [];
        }

        var rows = await dbContext.Exams
            .AsNoTracking()
            .Where(exam => teachingAssignmentIds.Contains(exam.TeachingAssignmentId))
            .OrderByDescending(exam => exam.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = new List<ExamDto>(rows.Count);

        foreach (var exam in rows)
        {
            result.Add(await MapExamAsync(exam, includeItems: false, cancellationToken).ConfigureAwait(false));
        }

        return result;
    }

    public async Task<ExamDto?> FindExamAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exam = await dbContext.Exams
            .AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.Id == id, cancellationToken)
            .ConfigureAwait(false);

        return exam is null
            ? null
            : await MapExamAsync(exam, includeItems: true, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AttemptDto>> ListAttemptsAsync(
        Guid examId,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Attempts
            .AsNoTracking()
            .Where(attempt => attempt.ExamId == examId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = new List<AttemptDto>(rows.Count);

        foreach (var attempt in rows)
        {
            result.Add(await MapAttemptAsync(attempt, cancellationToken).ConfigureAwait(false));
        }

        return [.. result.OrderBy(entry => entry.StudentName, StringComparer.Ordinal)];
    }

    public async Task<AttemptDto?> FindAttemptAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var attempt = await dbContext.Attempts
            .AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.Id == id, cancellationToken)
            .ConfigureAwait(false);

        return attempt is null
            ? null
            : await MapAttemptAsync(attempt, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StudentExamDto>> ListForStudentAsync(
        IReadOnlyCollection<Guid> teachingAssignmentIds,
        Guid studentUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(teachingAssignmentIds);

        if (teachingAssignmentIds.Count == 0)
        {
            return [];
        }

        var exams = await dbContext.Exams
            .AsNoTracking()
            .Where(exam => exam.IsPublished && teachingAssignmentIds.Contains(exam.TeachingAssignmentId))
            .OrderByDescending(exam => exam.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (exams.Count == 0)
        {
            return [];
        }

        var attempts = await dbContext.Attempts
            .AsNoTracking()
            .Where(attempt => attempt.StudentUserId == studentUserId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var byExam = attempts.ToDictionary(attempt => attempt.ExamId);
        var now = DateTimeOffset.UtcNow;
        var result = new List<StudentExamDto>(exams.Count);

        foreach (var exam in exams)
        {
            var assignment = await school.FindAssignmentAsync(exam.TeachingAssignmentId, cancellationToken)
                .ConfigureAwait(false);

            var attempt = byExam.GetValueOrDefault(exam.Id);

            result.Add(new StudentExamDto(
                exam.Id,
                exam.Title,
                assignment?.SubjectName ?? string.Empty,
                exam.DurationMinutes,
                exam.Items.Count,
                exam.MaxScore,
                exam.OpensAt,
                exam.ClosesAt,
                exam.AcceptsAttemptAt(now),
                attempt?.Id,
                attempt?.IsSubmitted ?? false,
                attempt?.IsReleased == true ? attempt.Score : null,
                attempt?.IsReleased == true ? attempt.Mark : null,
                attempt?.IsReleased ?? false));
        }

        return result;
    }

    private async Task<IReadOnlyList<QuestionDto>> MapQuestionsAsync(
        List<Question> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var authors = await users
            .FindByIdsAsync([.. rows.Select(question => question.AuthorUserId).Distinct()], cancellationToken)
            .ConfigureAwait(false);

        var names = authors.ToDictionary(person => person.Id, person => person.FullName);
        var subjects = await school.ListSubjectsAsync(cancellationToken).ConfigureAwait(false);
        var subjectNames = subjects.ToDictionary(subject => subject.Id, subject => subject.Name);

        return [.. rows.Select(question => new QuestionDto(
            question.Id,
            question.SubjectId,
            subjectNames.GetValueOrDefault(question.SubjectId, string.Empty),
            question.Kind.ToString(),
            question.Text,
            question.Explanation,
            question.AuthorUserId,
            names.GetValueOrDefault(question.AuthorUserId, string.Empty),
            [.. question.Options
                .OrderBy(option => option.Position)
                .Select(option => new QuestionOptionDto(option.Id, option.Text, option.IsCorrect, option.Position))]))];
    }

    private async Task<ExamDto> MapExamAsync(Exam exam, bool includeItems, CancellationToken cancellationToken)
    {
        var assignment = await school.FindAssignmentAsync(exam.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        var attempts = await dbContext.Attempts
            .AsNoTracking()
            .Where(attempt => attempt.ExamId == exam.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var awaiting = attempts.Count(attempt => attempt.IsSubmitted && !attempt.IsReleased);

        IReadOnlyList<ExamItemDto> items = [];

        if (includeItems && exam.Items.Count > 0)
        {
            var questions = await dbContext.Questions
                .AsNoTracking()
                .Where(question => exam.Items.Select(item => item.QuestionId).Contains(question.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var mapped = await MapQuestionsAsync(questions, cancellationToken).ConfigureAwait(false);
            var byId = mapped.ToDictionary(question => question.Id);

            items = [.. exam.Items
                .OrderBy(item => item.Position)
                .Where(item => byId.ContainsKey(item.QuestionId))
                .Select(item => new ExamItemDto(item.QuestionId, item.Points, item.Position, byId[item.QuestionId]))];
        }

        return new ExamDto(
            exam.Id,
            exam.TeachingAssignmentId,
            assignment?.SubjectName ?? string.Empty,
            assignment?.SchoolClassName ?? string.Empty,
            exam.Title,
            exam.Description,
            exam.DurationMinutes,
            exam.OpensAt,
            exam.ClosesAt,
            exam.IsPublished,
            exam.MaxScore,
            exam.Items.Count,
            attempts.Count,
            awaiting,
            items);
    }

    private async Task<AttemptDto> MapAttemptAsync(Attempt attempt, CancellationToken cancellationToken)
    {
        var exam = await dbContext.Exams
            .AsNoTracking()
            .SingleOrDefaultAsync(entry => entry.Id == attempt.ExamId, cancellationToken)
            .ConfigureAwait(false);

        var person = await users.FindByIdAsync(attempt.StudentUserId, cancellationToken).ConfigureAwait(false);

        var questions = await dbContext.Questions
            .AsNoTracking()
            .Where(question => attempt.Answers.Select(answer => answer.QuestionId).Contains(question.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var byId = questions.ToDictionary(question => question.Id);
        var points = exam?.Items.ToDictionary(item => item.QuestionId, item => item.Points) ?? [];
        var answers = new List<AttemptAnswerDto>(attempt.Answers.Count);

        foreach (var answer in attempt.Answers)
        {
            if (!byId.TryGetValue(answer.QuestionId, out var question))
            {
                continue;
            }

            answers.Add(new AttemptAnswerDto(
                answer.QuestionId,
                question.Text,
                question.Kind.ToString(),
                points.GetValueOrDefault(answer.QuestionId),
                answer.AwardedPoints,
                answer.Feedback,
                answer.Text,
                answer.SelectedOptionIds,
                [.. question.Options
                    .OrderBy(option => option.Position)
                    .Select(option => new QuestionOptionDto(
                        option.Id, option.Text, option.IsCorrect, option.Position))]));
        }

        return new AttemptDto(
            attempt.Id,
            attempt.ExamId,
            exam?.Title ?? string.Empty,
            attempt.StudentUserId,
            person?.FullName ?? string.Empty,
            attempt.StartedAt,
            attempt.Deadline,
            attempt.SubmittedAt,
            attempt.Score,
            attempt.MaxScore,
            attempt.Mark,
            attempt.SuggestedMark(),
            attempt.IsFullyScored,
            attempt.IsReleased,
            attempt.GradeId,
            answers);
    }
}
