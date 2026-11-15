using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Assessment.Application.Abstractions;
using EduPlatform.Modules.Assessment.Contracts;
using EduPlatform.Modules.Assessment.Domain;
using EduPlatform.Modules.Gradebook.Contracts;
using EduPlatform.Modules.SchoolStructure.Contracts;

namespace EduPlatform.Modules.Assessment.Application.UseCases;

public sealed record AnswerInput(Guid QuestionId, string? Text, IReadOnlyList<Guid> SelectedOptionIds);

public sealed record ListMyExamsQuery(Guid StudentUserId) : IQuery<IReadOnlyList<StudentExamDto>>;

internal sealed class ListMyExamsHandler(
    IAssessmentReadStore store,
    ISchoolDirectory school) : IQueryHandler<ListMyExamsQuery, IReadOnlyList<StudentExamDto>>
{
    public async Task<IReadOnlyList<StudentExamDto>> HandleAsync(
        ListMyExamsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var assignments = await school
            .ListAssignmentsForUserAsync(query.StudentUserId, "Student", cancellationToken)
            .ConfigureAwait(false);

        var ids = assignments.Select(item => item.Id).ToArray();

        return ids.Length == 0
            ? []
            : await store.ListForStudentAsync(ids, query.StudentUserId, cancellationToken).ConfigureAwait(false);
    }
}

public sealed record StartAttemptCommand(Guid ExamId, Guid StudentUserId) : ICommand<AttemptSheetDto>;

internal sealed class StartAttemptHandler(
    IAssessmentRepository repository,
    ISchoolDirectory school) : ICommandHandler<StartAttemptCommand, AttemptSheetDto>
{
    public async Task<AttemptSheetDto> HandleAsync(StartAttemptCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var exam = await repository.FindExamAsync(command.ExamId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Test not found.");

        await AttemptSheet
            .EnsureEnrolledAsync(school, command.StudentUserId, exam.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        var existing = await repository
            .FindAttemptOfStudentAsync(command.ExamId, command.StudentUserId, cancellationToken)
            .ConfigureAwait(false);

        var attempt = existing;

        if (attempt is null)
        {
            attempt = Attempt.Start(exam, command.StudentUserId, DateTimeOffset.UtcNow);
            repository.AddAttempt(attempt);
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var questions = await repository
            .ListQuestionsAsync([.. exam.Items.Select(item => item.QuestionId)], cancellationToken)
            .ConfigureAwait(false);

        return AttemptSheet.Build(exam, attempt, questions, await SubjectNameAsync(school, exam, cancellationToken)
            .ConfigureAwait(false));
    }

    private static async Task<string> SubjectNameAsync(
        ISchoolDirectory school,
        Exam exam,
        CancellationToken cancellationToken)
    {
        var assignment = await school.FindAssignmentAsync(exam.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        return assignment?.SubjectName ?? string.Empty;
    }
}

public sealed record SaveAnswersCommand(Guid AttemptId, Guid StudentUserId, IReadOnlyList<AnswerInput> Answers)
    : ICommand<AttemptSheetDto>;

internal sealed class SaveAnswersHandler(
    IAssessmentRepository repository,
    ISchoolDirectory school) : ICommandHandler<SaveAnswersCommand, AttemptSheetDto>
{
    public async Task<AttemptSheetDto> HandleAsync(SaveAnswersCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var (exam, attempt) = await AttemptSheet
            .LoadOwnAsync(repository, command.AttemptId, command.StudentUserId, cancellationToken)
            .ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;

        foreach (var answer in command.Answers)
        {
            attempt.Answer(answer.QuestionId, answer.Text, answer.SelectedOptionIds, now);
        }

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var questions = await repository
            .ListQuestionsAsync([.. exam.Items.Select(item => item.QuestionId)], cancellationToken)
            .ConfigureAwait(false);

        var assignment = await school.FindAssignmentAsync(exam.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        return AttemptSheet.Build(exam, attempt, questions, assignment?.SubjectName ?? string.Empty);
    }
}

public sealed record SubmitAttemptCommand(Guid AttemptId, Guid StudentUserId) : ICommand<AttemptSheetDto>;

internal sealed class SubmitAttemptHandler(
    IAssessmentRepository repository,
    ISchoolDirectory school) : ICommandHandler<SubmitAttemptCommand, AttemptSheetDto>
{
    public async Task<AttemptSheetDto> HandleAsync(SubmitAttemptCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var (exam, attempt) = await AttemptSheet
            .LoadOwnAsync(repository, command.AttemptId, command.StudentUserId, cancellationToken)
            .ConfigureAwait(false);

        var questions = await repository
            .ListQuestionsAsync([.. exam.Items.Select(item => item.QuestionId)], cancellationToken)
            .ConfigureAwait(false);

        var outcome = new Dictionary<Guid, (bool IsOpen, bool IsCorrect, int Points)>();

        foreach (var item in exam.Items)
        {
            var question = questions.SingleOrDefault(entry => entry.Id == item.QuestionId);

            if (question is null)
            {
                continue;
            }

            var given = attempt.Answers.SingleOrDefault(entry => entry.QuestionId == item.QuestionId);
            var selected = given?.SelectedOptionIds ?? [];

            outcome[item.QuestionId] =
                (question.IsOpen, question.IsAnsweredCorrectlyBy([.. selected]), item.Points);
        }

        attempt.Submit(outcome, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var assignment = await school.FindAssignmentAsync(exam.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        return AttemptSheet.Build(exam, attempt, questions, assignment?.SubjectName ?? string.Empty);
    }
}

public sealed record ListAttemptsQuery(Guid ExamId, Guid TeacherUserId) : IQuery<IReadOnlyList<AttemptDto>>;

internal sealed class ListAttemptsHandler(
    IAssessmentRepository repository,
    IAssessmentReadStore store,
    ISchoolDirectory school) : IQueryHandler<ListAttemptsQuery, IReadOnlyList<AttemptDto>>
{
    public async Task<IReadOnlyList<AttemptDto>> HandleAsync(
        ListAttemptsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var exam = await repository.FindExamAsync(query.ExamId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Test not found.");

        await ListExamsHandler
            .EnsureTeachesAsync(school, query.TeacherUserId, exam.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        return await store.ListAttemptsAsync(query.ExamId, cancellationToken).ConfigureAwait(false);
    }
}

public sealed record ReviewAnswerCommand(
    Guid AttemptId,
    Guid QuestionId,
    int Points,
    string? Feedback,
    Guid TeacherUserId) : ICommand<AttemptDto>;

internal sealed class ReviewAnswerHandler(
    IAssessmentRepository repository,
    IAssessmentReadStore store,
    ISchoolDirectory school) : ICommandHandler<ReviewAnswerCommand, AttemptDto>
{
    public async Task<AttemptDto> HandleAsync(ReviewAnswerCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var attempt = await repository.FindAttemptAsync(command.AttemptId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException("Attempt not found.");

        var exam = await repository.FindExamAsync(attempt.ExamId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Test not found.");

        await ListExamsHandler
            .EnsureTeachesAsync(school, command.TeacherUserId, exam.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        var item = exam.Items.SingleOrDefault(entry => entry.QuestionId == command.QuestionId)
            ?? throw new DomainException("This question is not part of the test.");

        if (command.Points > item.Points)
        {
            throw new DomainException($"This question is worth at most {item.Points} points.");
        }

        attempt.Review(command.QuestionId, command.Points, command.Feedback);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await store.FindAttemptAsync(attempt.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("The attempt could not be read back.");
    }
}

public sealed record ReleaseAttemptCommand(Guid AttemptId, decimal? Mark, Guid TeacherUserId) : ICommand<AttemptDto>;

internal sealed class ReleaseAttemptHandler(
    IAssessmentRepository repository,
    IAssessmentReadStore store,
    ISchoolDirectory school,
    IGradebookWriter gradebook) : ICommandHandler<ReleaseAttemptCommand, AttemptDto>
{
    public async Task<AttemptDto> HandleAsync(ReleaseAttemptCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var attempt = await repository.FindAttemptAsync(command.AttemptId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException("Attempt not found.");

        var exam = await repository.FindExamAsync(attempt.ExamId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Test not found.");

        await ListExamsHandler
            .EnsureTeachesAsync(school, command.TeacherUserId, exam.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        attempt.Release(command.Mark ?? attempt.SuggestedMark());

        var superseded = attempt.GradeId;

        var grade = await gradebook.GiveTestGradeAsync(
                attempt.StudentUserId,
                exam.TeachingAssignmentId,
                attempt.Mark!.Value,
                command.TeacherUserId,
                exam.Title,
                cancellationToken)
            .ConfigureAwait(false);

        attempt.AttachGrade(grade.Id);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (superseded is { } old)
        {
            await gradebook.RemoveGradeAsync(old, cancellationToken).ConfigureAwait(false);
        }

        return await store.FindAttemptAsync(attempt.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("The attempt could not be read back.");
    }
}

internal static class AttemptSheet
{
    public static async Task EnsureEnrolledAsync(
        ISchoolDirectory school,
        Guid studentUserId,
        Guid teachingAssignmentId,
        CancellationToken cancellationToken)
    {
        var assignments = await school
            .ListAssignmentsForUserAsync(studentUserId, "Student", cancellationToken)
            .ConfigureAwait(false);

        if (!assignments.Any(item => item.Id == teachingAssignmentId))
        {
            throw new ForbiddenException("This test was not set for your class.");
        }
    }

    public static async Task<(Exam Exam, Attempt Attempt)> LoadOwnAsync(
        IAssessmentRepository repository,
        Guid attemptId,
        Guid studentUserId,
        CancellationToken cancellationToken)
    {
        var attempt = await repository.FindAttemptAsync(attemptId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Attempt not found.");

        if (attempt.StudentUserId != studentUserId)
        {
            throw new ForbiddenException("This attempt is not yours.");
        }

        var exam = await repository.FindExamAsync(attempt.ExamId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Test not found.");

        return (exam, attempt);
    }

    public static AttemptSheetDto Build(
        Exam exam,
        Attempt attempt,
        IReadOnlyList<Question> questions,
        string subjectName)
    {
        var byId = questions.ToDictionary(question => question.Id);
        var ordered = Shuffle(exam.Items, attempt.Id);
        var sheet = new List<ExamQuestionDto>(ordered.Count);

        foreach (var item in ordered)
        {
            if (!byId.TryGetValue(item.QuestionId, out var question))
            {
                continue;
            }

            var given = attempt.Answers.SingleOrDefault(entry => entry.QuestionId == item.QuestionId);

            sheet.Add(new ExamQuestionDto(
                question.Id,
                question.Kind.ToString(),
                question.Text,
                item.Points,
                [.. Shuffle(question.Options, attempt.Id).Select(option => new ExamOptionDto(option.Id, option.Text))],
                given?.Text,
                given?.SelectedOptionIds ?? []));
        }

        var secondsLeft = (int)Math.Max(0, (attempt.Deadline - DateTimeOffset.UtcNow).TotalSeconds);

        return new AttemptSheetDto(
            attempt.Id,
            exam.Id,
            exam.Title,
            exam.Description,
            subjectName,
            attempt.Deadline,
            attempt.IsSubmitted ? 0 : secondsLeft,
            attempt.IsSubmitted,
            sheet);
    }

    private static List<T> Shuffle<T>(IReadOnlyList<T> source, Guid seed)
        where T : Entity
    {
        var ordered = source
            .Select(entry => (Entry: entry, Key: Mix(seed, entry.Id)))
            .OrderBy(pair => pair.Key)
            .Select(pair => pair.Entry)
            .ToList();

        return ordered;
    }

    private static int Mix(Guid seed, Guid value)
    {
        Span<byte> buffer = stackalloc byte[32];
        seed.TryWriteBytes(buffer[..16]);
        value.TryWriteBytes(buffer[16..]);

        var hash = 17;

        foreach (var octet in buffer)
        {
            hash = unchecked((hash * 31) + octet);
        }

        return hash;
    }
}
