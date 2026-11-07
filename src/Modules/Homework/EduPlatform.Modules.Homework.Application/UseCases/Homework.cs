using EduPlatform.BuildingBlocks.Application.Abstractions;
using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Gradebook.Contracts;
using EduPlatform.Modules.Homework.Application.Abstractions;
using EduPlatform.Modules.Homework.Contracts;
using EduPlatform.Modules.Homework.Domain;
using EduPlatform.Modules.SchoolStructure.Contracts;
using FluentValidation;

namespace EduPlatform.Modules.Homework.Application.UseCases;

public sealed record ListAssignmentsForTeacherQuery(Guid TeacherUserId)
    : IQuery<IReadOnlyList<AssignmentDto>>;

internal sealed class ListAssignmentsForTeacherHandler(
    IHomeworkReadStore store,
    ISchoolDirectory school) : IQueryHandler<ListAssignmentsForTeacherQuery, IReadOnlyList<AssignmentDto>>
{
    public async Task<IReadOnlyList<AssignmentDto>> HandleAsync(
        ListAssignmentsForTeacherQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var assignments = await school
            .ListAssignmentsForUserAsync(query.TeacherUserId, "Teacher", cancellationToken)
            .ConfigureAwait(false);

        var ids = assignments.Select(item => item.Id).ToArray();

        return ids.Length == 0
            ? []
            : await store.ListAssignmentsAsync(ids, cancellationToken).ConfigureAwait(false);
    }
}

public sealed record ListSubmissionsQuery(Guid AssignmentId, Guid TeacherUserId)
    : IQuery<IReadOnlyList<SubmissionDto>>;

internal sealed class ListSubmissionsHandler(
    IHomeworkRepository repository,
    IHomeworkReadStore store,
    ISchoolDirectory school) : IQueryHandler<ListSubmissionsQuery, IReadOnlyList<SubmissionDto>>
{
    public async Task<IReadOnlyList<SubmissionDto>> HandleAsync(
        ListSubmissionsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var assignment = await repository.FindAssignmentAsync(query.AssignmentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException("Homework assignment not found.");

        await EnsureTeachesAsync(school, query.TeacherUserId, assignment.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        return await store.ListSubmissionsAsync(query.AssignmentId, cancellationToken).ConfigureAwait(false);
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

public sealed record ListMyHomeworkQuery(Guid StudentUserId) : IQuery<IReadOnlyList<StudentAssignmentDto>>;

internal sealed class ListMyHomeworkHandler(
    IHomeworkReadStore store,
    ISchoolDirectory school) : IQueryHandler<ListMyHomeworkQuery, IReadOnlyList<StudentAssignmentDto>>
{
    public async Task<IReadOnlyList<StudentAssignmentDto>> HandleAsync(
        ListMyHomeworkQuery query,
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

public sealed record CreateAssignmentCommand(
    Guid TeachingAssignmentId,
    string Title,
    string? Description,
    DateOnly DueOn,
    bool IsGraded,
    Guid TeacherUserId,
    Stream? Content = null,
    string? FileName = null,
    string? ContentType = null,
    long SizeBytes = 0) : ICommand<AssignmentDto>;

internal sealed class CreateAssignmentValidator : AbstractValidator<CreateAssignmentCommand>
{
    public CreateAssignmentValidator()
    {
        RuleFor(command => command.TeachingAssignmentId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(Assignment.MaxTitleLength);
        RuleFor(command => command.Description).MaximumLength(Assignment.MaxDescriptionLength);
    }
}

internal sealed class CreateAssignmentHandler(
    IHomeworkRepository repository,
    IHomeworkReadStore store,
    ISchoolDirectory school,
    IFileStorage storage) : ICommandHandler<CreateAssignmentCommand, AssignmentDto>
{
    public async Task<AssignmentDto> HandleAsync(
        CreateAssignmentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await ListSubmissionsHandler
            .EnsureTeachesAsync(school, command.TeacherUserId, command.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        var assignment = Assignment.Create(
            command.TeachingAssignmentId,
            command.Title,
            command.Description,
            command.DueOn,
            command.IsGraded,
            command.TeacherUserId,
            DateOnly.FromDateTime(DateTime.UtcNow));

        if (command.Content is not null)
        {
            var key = await storage
                .SaveAsync(
                    StorageBuckets.Submissions,
                    command.Content,
                    command.FileName ?? string.Empty,
                    command.ContentType ?? string.Empty,
                    command.SizeBytes,
                    cancellationToken)
                .ConfigureAwait(false);

            assignment.Attach(key, command.FileName, command.ContentType, command.SizeBytes);
        }

        repository.AddAssignment(assignment);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var created = await store
            .ListAssignmentsAsync([command.TeachingAssignmentId], cancellationToken)
            .ConfigureAwait(false);

        return created.SingleOrDefault(item => item.Id == assignment.Id)
            ?? throw new DomainException("The assignment could not be read back.");
    }
}

public sealed record DeleteAssignmentCommand(Guid AssignmentId, Guid TeacherUserId) : ICommand;

internal sealed class DeleteAssignmentHandler(
    IHomeworkRepository repository,
    ISchoolDirectory school) : CommandHandler<DeleteAssignmentCommand>
{
    protected override async Task HandleCoreAsync(
        DeleteAssignmentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var assignment = await repository.FindAssignmentAsync(command.AssignmentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException("Homework assignment not found.");

        await ListSubmissionsHandler
            .EnsureTeachesAsync(school, command.TeacherUserId, assignment.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        repository.RemoveAssignment(assignment);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed record SubmitHomeworkCommand(
    Guid AssignmentId,
    Guid StudentUserId,
    string Text,
    Stream? Content = null,
    string? FileName = null,
    string? ContentType = null,
    long SizeBytes = 0) : ICommand<SubmissionDto>;

internal sealed class SubmitHomeworkValidator : AbstractValidator<SubmitHomeworkCommand>
{
    public SubmitHomeworkValidator()
    {
        RuleFor(command => command.AssignmentId).NotEmpty();
        RuleFor(command => command.Text).MaximumLength(Submission.MaxTextLength);
    }
}

internal sealed class SubmitHomeworkHandler(
    IHomeworkRepository repository,
    IHomeworkReadStore store,
    ISchoolDirectory school,
    IFileStorage storage) : ICommandHandler<SubmitHomeworkCommand, SubmissionDto>
{
    public async Task<SubmissionDto> HandleAsync(
        SubmitHomeworkCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var assignment = await repository.FindAssignmentAsync(command.AssignmentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException("Homework assignment not found.");

        var enrolled = await school
            .ListAssignmentsForUserAsync(command.StudentUserId, "Student", cancellationToken)
            .ConfigureAwait(false);

        if (!enrolled.Any(item => item.Id == assignment.TeachingAssignmentId))
        {
            throw new ForbiddenException("This homework was not set for your class.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var existing = await repository
            .FindSubmissionOfStudentAsync(command.AssignmentId, command.StudentUserId, cancellationToken)
            .ConfigureAwait(false);

        var keeps = command.Content is not null || existing?.HasAttachment == true;

        Submission submission;

        if (existing is null)
        {
            submission = Submission.Create(
                command.AssignmentId, command.StudentUserId, command.Text, keeps, assignment.DueOn, today);

            repository.AddSubmission(submission);
        }
        else
        {
            existing.Replace(command.Text, keeps, assignment.DueOn, today);
            submission = existing;
        }

        if (command.Content is not null)
        {
            var replaced = submission.DetachFile();

            var key = await storage
                .SaveAsync(
                    StorageBuckets.Submissions,
                    command.Content,
                    command.FileName ?? string.Empty,
                    command.ContentType ?? string.Empty,
                    command.SizeBytes,
                    cancellationToken)
                .ConfigureAwait(false);

            submission.Attach(key, command.FileName, command.ContentType, command.SizeBytes);

            if (replaced is not null)
            {
                await storage.DeleteAsync(StorageBuckets.Submissions, replaced, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await store.FindSubmissionAsync(submission.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("The submission could not be read back.");
    }
}

public sealed record ReviewSubmissionCommand(
    Guid SubmissionId,
    decimal? Mark,
    string? Feedback,
    Guid TeacherUserId) : ICommand<SubmissionDto>;

internal sealed class ReviewSubmissionValidator : AbstractValidator<ReviewSubmissionCommand>
{
    public ReviewSubmissionValidator()
    {
        RuleFor(command => command.SubmissionId).NotEmpty();
        RuleFor(command => command.Feedback).MaximumLength(Submission.MaxFeedbackLength);
        RuleFor(command => command.Mark)
            .InclusiveBetween(Submission.MinMark, Submission.MaxMark)
            .When(command => command.Mark is not null);
    }
}

internal sealed class ReviewSubmissionHandler(
    IHomeworkRepository repository,
    IHomeworkReadStore store,
    ISchoolDirectory school,
    IGradebookWriter gradebook) : ICommandHandler<ReviewSubmissionCommand, SubmissionDto>
{
    public async Task<SubmissionDto> HandleAsync(
        ReviewSubmissionCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var submission = await repository.FindSubmissionAsync(command.SubmissionId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException("Submission not found.");

        var assignment = await repository.FindAssignmentAsync(submission.AssignmentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException("Homework assignment not found.");

        await ListSubmissionsHandler
            .EnsureTeachesAsync(school, command.TeacherUserId, assignment.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        var wasGraded = submission.GradeId is not null;

        submission.Review(command.Mark, command.Feedback, command.TeacherUserId, assignment.IsGraded);

        if (!wasGraded && submission.Mark is { } mark)
        {
            var grade = await gradebook.GiveHomeworkGradeAsync(
                    submission.StudentUserId,
                    assignment.TeachingAssignmentId,
                    mark,
                    command.TeacherUserId,
                    assignment.Title,
                    cancellationToken)
                .ConfigureAwait(false);

            submission.AttachGrade(grade.Id);
        }

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await store.FindSubmissionAsync(submission.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("The submission could not be read back.");
    }
}

public sealed record HomeworkFileDto(Stream Content, string FileName, string ContentType);

public sealed record DownloadAssignmentFileQuery(Guid AssignmentId, Guid UserId, string Role)
    : IQuery<HomeworkFileDto>;

internal sealed class DownloadAssignmentFileHandler(
    IHomeworkRepository repository,
    ISchoolDirectory school,
    IFileStorage storage) : IQueryHandler<DownloadAssignmentFileQuery, HomeworkFileDto>
{
    public async Task<HomeworkFileDto> HandleAsync(
        DownloadAssignmentFileQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var assignment = await repository.FindAssignmentAsync(query.AssignmentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException("Homework assignment not found.");

        if (assignment.StorageKey is not { } key)
        {
            throw new DomainException("This homework has no attachment.");
        }

        await EnsureSeesAsync(school, query.UserId, query.Role, assignment.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false);

        var content = await storage.OpenAsync(StorageBuckets.Submissions, key, cancellationToken)
            .ConfigureAwait(false);

        return new HomeworkFileDto(
            content,
            assignment.FileName ?? "attachment",
            assignment.ContentType ?? "application/octet-stream");
    }

    internal static async Task EnsureSeesAsync(
        ISchoolDirectory school,
        Guid userId,
        string role,
        Guid teachingAssignmentId,
        CancellationToken cancellationToken)
    {
        if (role == "Admin")
        {
            return;
        }

        var own = await school.ListAssignmentsForUserAsync(userId, role, cancellationToken).ConfigureAwait(false);

        if (!own.Any(item => item.Id == teachingAssignmentId))
        {
            throw new ForbiddenException("This homework does not belong to you.");
        }
    }
}

public sealed record DownloadSubmissionFileQuery(Guid SubmissionId, Guid UserId, string Role)
    : IQuery<HomeworkFileDto>;

internal sealed class DownloadSubmissionFileHandler(
    IHomeworkRepository repository,
    ISchoolDirectory school,
    IFileStorage storage) : IQueryHandler<DownloadSubmissionFileQuery, HomeworkFileDto>
{
    public async Task<HomeworkFileDto> HandleAsync(
        DownloadSubmissionFileQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var submission = await repository.FindSubmissionAsync(query.SubmissionId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException("Submission not found.");

        if (submission.StorageKey is not { } key)
        {
            throw new DomainException("This submission has no attachment.");
        }

        if (submission.StudentUserId != query.UserId)
        {
            var assignment = await repository.FindAssignmentAsync(submission.AssignmentId, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new DomainException("Homework assignment not found.");

            if (query.Role != "Admin")
            {
                await ListSubmissionsHandler
                    .EnsureTeachesAsync(school, query.UserId, assignment.TeachingAssignmentId, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        var content = await storage.OpenAsync(StorageBuckets.Submissions, key, cancellationToken)
            .ConfigureAwait(false);

        return new HomeworkFileDto(
            content,
            submission.FileName ?? "attachment",
            submission.ContentType ?? "application/octet-stream");
    }
}
