using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Assessment.Application.Abstractions;
using EduPlatform.Modules.Assessment.Contracts;
using EduPlatform.Modules.Assessment.Domain;
using EduPlatform.Modules.SchoolStructure.Contracts;
using FluentValidation;

namespace EduPlatform.Modules.Assessment.Application.UseCases;

public sealed record ListExamsQuery(Guid TeacherUserId) : IQuery<IReadOnlyList<ExamDto>>;

internal sealed class ListExamsHandler(
    IAssessmentReadStore store,
    ISchoolDirectory school) : IQueryHandler<ListExamsQuery, IReadOnlyList<ExamDto>>
{
    public async Task<IReadOnlyList<ExamDto>> HandleAsync(ListExamsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var assignments = await school
            .ListAssignmentsForUserAsync(query.TeacherUserId, "Teacher", cancellationToken)
            .ConfigureAwait(false);

        var ids = assignments.Select(item => item.Id).ToArray();

        return ids.Length == 0
            ? []
            : await store.ListExamsAsync(ids, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task EnsureTeachesAsync(
        ISchoolDirectory school,
        Guid teacherUserId,
        Guid teachingAssignmentId,
        CancellationToken cancellationToken)
    {
        var assignments = await school
            .ListAssignmentsForUserAsync(teacherUserId, "Teacher", cancellationToken)
            .ConfigureAwait(false);

        if (!assignments.Any(item => item.Id == teachingAssignmentId))
        {
            throw new ForbiddenException("You do not teach this subject to this class.");
        }
    }
}

public sealed record GetExamQuery(Guid ExamId, Guid TeacherUserId) : IQuery<ExamDto>;

internal sealed class GetExamHandler(
    IAssessmentRepository repository,
    IAssessmentReadStore store,
    ISchoolDirectory school) : IQueryHandler<GetExamQuery, ExamDto>
{
    public async Task<ExamDto> HandleAsync(GetExamQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var exam = await repository.FindExamAsync(query.ExamId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Test not found.");

        await ListExamsHandler
            .EnsureTeachesAsync(school, query.TeacherUserId, exam.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        return await store.FindExamAsync(query.ExamId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Test not found.");
    }
}

public sealed record CreateExamCommand(
    Guid TeachingAssignmentId,
    string Title,
    string? Description,
    int DurationMinutes,
    Guid TeacherUserId) : ICommand<ExamDto>;

internal sealed class CreateExamValidator : AbstractValidator<CreateExamCommand>
{
    public CreateExamValidator()
    {
        RuleFor(command => command.TeachingAssignmentId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(Exam.MaxTitleLength);
        RuleFor(command => command.Description).MaximumLength(Exam.MaxDescriptionLength);
        RuleFor(command => command.DurationMinutes)
            .InclusiveBetween(Exam.MinDurationMinutes, Exam.MaxDurationMinutes);
    }
}

internal sealed class CreateExamHandler(
    IAssessmentRepository repository,
    IAssessmentReadStore store,
    ISchoolDirectory school) : ICommandHandler<CreateExamCommand, ExamDto>
{
    public async Task<ExamDto> HandleAsync(CreateExamCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await ListExamsHandler
            .EnsureTeachesAsync(school, command.TeacherUserId, command.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        var exam = Exam.Create(
            command.TeachingAssignmentId,
            command.Title,
            command.Description,
            command.DurationMinutes,
            command.TeacherUserId);

        repository.AddExam(exam);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await store.FindExamAsync(exam.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("The test could not be read back.");
    }
}

public sealed record AddExamItemCommand(Guid ExamId, Guid QuestionId, int Points, Guid TeacherUserId)
    : ICommand<ExamDto>;

internal sealed class AddExamItemHandler(
    IAssessmentRepository repository,
    IAssessmentReadStore store,
    ISchoolDirectory school) : ICommandHandler<AddExamItemCommand, ExamDto>
{
    public async Task<ExamDto> HandleAsync(AddExamItemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var exam = await repository.FindExamAsync(command.ExamId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Test not found.");

        await ListExamsHandler
            .EnsureTeachesAsync(school, command.TeacherUserId, exam.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        var question = await repository.FindQuestionAsync(command.QuestionId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException("Question not found.");

        await ListQuestionsHandler
            .EnsureTeachesSubjectAsync(school, command.TeacherUserId, question.SubjectId, cancellationToken)
            .ConfigureAwait(false);

        exam.AddItem(command.QuestionId, command.Points);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await store.FindExamAsync(exam.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("The test could not be read back.");
    }
}

public sealed record AddExamQuestionCommand(
    Guid ExamId,
    QuestionKind Kind,
    string Text,
    string? Explanation,
    IReadOnlyList<OptionInput> Options,
    int Points,
    Guid TeacherUserId) : ICommand<ExamDto>;

internal sealed class AddExamQuestionValidator : AbstractValidator<AddExamQuestionCommand>
{
    public AddExamQuestionValidator()
    {
        RuleFor(command => command.ExamId).NotEmpty();
        RuleFor(command => command.Kind).IsInEnum();
        RuleFor(command => command.Text).NotEmpty().MaximumLength(Question.MaxTextLength);
        RuleFor(command => command.Explanation).MaximumLength(Question.MaxExplanationLength);
        RuleFor(command => command.Points).InclusiveBetween(Exam.MinPoints, Exam.MaxPoints);
    }
}

internal sealed class AddExamQuestionHandler(
    IAssessmentRepository repository,
    IAssessmentReadStore store,
    ISchoolDirectory school) : ICommandHandler<AddExamQuestionCommand, ExamDto>
{
    public async Task<ExamDto> HandleAsync(AddExamQuestionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var exam = await repository.FindExamAsync(command.ExamId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Test not found.");

        await ListExamsHandler
            .EnsureTeachesAsync(school, command.TeacherUserId, exam.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        var assignment = await school.FindAssignmentAsync(exam.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException("Teaching assignment not found.");

        var question = Question.Create(
            assignment.SubjectId,
            command.Kind,
            command.Text,
            command.Explanation,
            [.. command.Options.Select(option => (option.Text, option.IsCorrect))],
            command.TeacherUserId);

        repository.AddQuestion(question);
        exam.AddItem(question.Id, command.Points);

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await store.FindExamAsync(exam.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("The test could not be read back.");
    }
}

public sealed record RemoveExamItemCommand(Guid ExamId, Guid QuestionId, Guid TeacherUserId) : ICommand<ExamDto>;

internal sealed class RemoveExamItemHandler(
    IAssessmentRepository repository,
    IAssessmentReadStore store,
    ISchoolDirectory school) : ICommandHandler<RemoveExamItemCommand, ExamDto>
{
    public async Task<ExamDto> HandleAsync(RemoveExamItemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var exam = await repository.FindExamAsync(command.ExamId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Test not found.");

        await ListExamsHandler
            .EnsureTeachesAsync(school, command.TeacherUserId, exam.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        exam.RemoveItem(command.QuestionId);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (!await repository.QuestionIsUsedAsync(command.QuestionId, cancellationToken).ConfigureAwait(false))
        {
            var orphan = await repository.FindQuestionAsync(command.QuestionId, cancellationToken)
                .ConfigureAwait(false);

            if (orphan is not null)
            {
                repository.RemoveQuestion(orphan);
                await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        return await store.FindExamAsync(exam.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("The test could not be read back.");
    }
}

public sealed record PublishExamCommand(
    Guid ExamId,
    DateTimeOffset? OpensAt,
    DateTimeOffset? ClosesAt,
    Guid TeacherUserId) : ICommand<ExamDto>;

internal sealed class PublishExamHandler(
    IAssessmentRepository repository,
    IAssessmentReadStore store,
    ISchoolDirectory school) : ICommandHandler<PublishExamCommand, ExamDto>
{
    public async Task<ExamDto> HandleAsync(PublishExamCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var exam = await repository.FindExamAsync(command.ExamId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Test not found.");

        await ListExamsHandler
            .EnsureTeachesAsync(school, command.TeacherUserId, exam.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        exam.Publish(command.OpensAt, command.ClosesAt);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await store.FindExamAsync(exam.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("The test could not be read back.");
    }
}

public sealed record DeleteExamCommand(Guid ExamId, Guid TeacherUserId) : ICommand;

internal sealed class DeleteExamHandler(
    IAssessmentRepository repository,
    ISchoolDirectory school) : CommandHandler<DeleteExamCommand>
{
    protected override async Task HandleCoreAsync(DeleteExamCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var exam = await repository.FindExamAsync(command.ExamId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Test not found.");

        await ListExamsHandler
            .EnsureTeachesAsync(school, command.TeacherUserId, exam.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        repository.RemoveExam(exam);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
