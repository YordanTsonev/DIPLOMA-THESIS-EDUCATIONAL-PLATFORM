using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Identity.Contracts;
using EduPlatform.Modules.SchoolStructure.Application.Abstractions;
using EduPlatform.Modules.SchoolStructure.Contracts;
using EduPlatform.Modules.SchoolStructure.Domain;
using FluentValidation;

namespace EduPlatform.Modules.SchoolStructure.Application.UseCases;

public sealed record ListAssignmentsQuery(Guid? SchoolClassId = null, Guid? TeacherUserId = null)
    : IQuery<IReadOnlyList<TeachingAssignmentDto>>;

internal sealed class ListAssignmentsHandler(ISchoolStructureReadStore store)
    : IQueryHandler<ListAssignmentsQuery, IReadOnlyList<TeachingAssignmentDto>>
{
    public Task<IReadOnlyList<TeachingAssignmentDto>> HandleAsync(
        ListAssignmentsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return store.ListAssignmentsAsync(query.SchoolClassId, query.TeacherUserId, cancellationToken);
    }
}

public sealed record CreateAssignmentCommand(Guid TeacherUserId, Guid SubjectId, Guid SchoolClassId, int WeeklyHours)
    : ICommand<TeachingAssignmentDto>;

internal sealed class CreateAssignmentValidator : AbstractValidator<CreateAssignmentCommand>
{
    public CreateAssignmentValidator()
    {
        RuleFor(command => command.TeacherUserId).NotEmpty();
        RuleFor(command => command.SubjectId).NotEmpty();
        RuleFor(command => command.SchoolClassId).NotEmpty();
        RuleFor(command => command.WeeklyHours).InclusiveBetween(1, TeachingAssignment.MaxWeeklyHours);
    }
}

internal sealed class CreateAssignmentHandler(
    ISchoolStructureRepository repository,
    ISchoolStructureReadStore store,
    IUserDirectory users) : ICommandHandler<CreateAssignmentCommand, TeachingAssignmentDto>
{
    public async Task<TeachingAssignmentDto> HandleAsync(
        CreateAssignmentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await CreateClassHandler.EnsureTeacherAsync(users, command.TeacherUserId, cancellationToken).ConfigureAwait(false);

        var subject = await repository.FindSubjectAsync(command.SubjectId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Subject not found.");

        if (!subject.IsActive)
        {
            throw new DomainException("This subject is no longer taught.");
        }

        _ = await repository.FindClassAsync(command.SchoolClassId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Class not found.");

        if (await repository.AssignmentExistsAsync(command.SubjectId, command.SchoolClassId, cancellationToken).ConfigureAwait(false))
        {
            throw new DomainException("This class already has a teacher for this subject.");
        }

        var assignment = TeachingAssignment.Create(
            command.TeacherUserId, command.SubjectId, command.SchoolClassId, command.WeeklyHours);

        repository.AddAssignment(assignment);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var created = await store.ListAssignmentsAsync(command.SchoolClassId, null, cancellationToken).ConfigureAwait(false);

        return created.Single(item => item.Id == assignment.Id);
    }
}
