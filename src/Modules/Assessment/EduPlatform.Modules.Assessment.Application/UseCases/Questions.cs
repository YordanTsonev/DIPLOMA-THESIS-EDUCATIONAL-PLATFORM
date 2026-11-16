using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Assessment.Application.Abstractions;
using EduPlatform.Modules.Assessment.Contracts;
using EduPlatform.Modules.Assessment.Domain;
using EduPlatform.Modules.SchoolStructure.Contracts;
using FluentValidation;

namespace EduPlatform.Modules.Assessment.Application.UseCases;

public sealed record OptionInput(string Text, bool IsCorrect);

public sealed record ListQuestionsQuery(Guid TeacherUserId, Guid? SubjectId, bool OwnOnly)
    : IQuery<IReadOnlyList<QuestionDto>>;

internal sealed class ListQuestionsHandler(
    IAssessmentReadStore store,
    ISchoolDirectory school) : IQueryHandler<ListQuestionsQuery, IReadOnlyList<QuestionDto>>
{
    public async Task<IReadOnlyList<QuestionDto>> HandleAsync(
        ListQuestionsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var subjects = await TaughtSubjectsAsync(school, query.TeacherUserId, cancellationToken)
            .ConfigureAwait(false);

        if (query.SubjectId is { } requested)
        {
            subjects = subjects.Where(id => id == requested).ToArray();
        }

        return subjects.Count == 0
            ? []
            : await store
                .ListQuestionsAsync(subjects, query.OwnOnly ? query.TeacherUserId : null, cancellationToken)
                .ConfigureAwait(false);
    }

    internal static async Task<IReadOnlyCollection<Guid>> TaughtSubjectsAsync(
        ISchoolDirectory school,
        Guid teacherUserId,
        CancellationToken cancellationToken)
    {
        var assignments = await school
            .ListAssignmentsForUserAsync(teacherUserId, "Teacher", cancellationToken)
            .ConfigureAwait(false);

        return assignments.Select(item => item.SubjectId).Distinct().ToArray();
    }

    internal static async Task EnsureTeachesSubjectAsync(
        ISchoolDirectory school,
        Guid teacherUserId,
        Guid subjectId,
        CancellationToken cancellationToken)
    {
        var subjects = await TaughtSubjectsAsync(school, teacherUserId, cancellationToken).ConfigureAwait(false);

        if (!subjects.Contains(subjectId))
        {
            throw new ForbiddenException("You do not teach this subject.");
        }
    }
}

public sealed record CreateQuestionCommand(
    Guid SubjectId,
    QuestionKind Kind,
    string Text,
    string? Explanation,
    IReadOnlyList<OptionInput> Options,
    Guid TeacherUserId) : ICommand<QuestionDto>;

internal sealed class CreateQuestionValidator : AbstractValidator<CreateQuestionCommand>
{
    public CreateQuestionValidator()
    {
        RuleFor(command => command.SubjectId).NotEmpty();
        RuleFor(command => command.Kind).IsInEnum();
        RuleFor(command => command.Text).NotEmpty().MaximumLength(Question.MaxTextLength);
        RuleFor(command => command.Explanation).MaximumLength(Question.MaxExplanationLength);
    }
}

internal sealed class CreateQuestionHandler(
    IAssessmentRepository repository,
    IAssessmentReadStore store,
    ISchoolDirectory school) : ICommandHandler<CreateQuestionCommand, QuestionDto>
{
    public async Task<QuestionDto> HandleAsync(CreateQuestionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await ListQuestionsHandler
            .EnsureTeachesSubjectAsync(school, command.TeacherUserId, command.SubjectId, cancellationToken)
            .ConfigureAwait(false);

        var question = Question.Create(
            command.SubjectId,
            command.Kind,
            command.Text,
            command.Explanation,
            [.. command.Options.Select(option => (option.Text, option.IsCorrect))],
            command.TeacherUserId);

        repository.AddQuestion(question);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var created = await store
            .ListQuestionsAsync([command.SubjectId], null, cancellationToken)
            .ConfigureAwait(false);

        return created.SingleOrDefault(item => item.Id == question.Id)
            ?? throw new DomainException("The question could not be read back.");
    }
}

public sealed record DeleteQuestionCommand(Guid QuestionId, Guid TeacherUserId) : ICommand;

internal sealed class DeleteQuestionHandler(
    IAssessmentRepository repository) : CommandHandler<DeleteQuestionCommand>
{
    protected override async Task HandleCoreAsync(
        DeleteQuestionCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var question = await repository.FindQuestionAsync(command.QuestionId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException("Question not found.");

        if (question.AuthorUserId != command.TeacherUserId)
        {
            throw new ForbiddenException("Only the author may remove a question.");
        }

        if (await repository.QuestionIsUsedAsync(question.Id, cancellationToken).ConfigureAwait(false))
        {
            throw new DomainException("This question is used by a test and cannot be removed.");
        }

        repository.RemoveQuestion(question);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
