using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Identity.Contracts;
using EduPlatform.Modules.SchoolStructure.Application.Abstractions;
using EduPlatform.Modules.SchoolStructure.Contracts;
using EduPlatform.Modules.SchoolStructure.Domain;

namespace EduPlatform.Modules.SchoolStructure.Application.UseCases;

public sealed record ListChildrenQuery(Guid ParentUserId) : IQuery<IReadOnlyList<ChildDto>>;

internal sealed class ListChildrenHandler(ISchoolDirectory directory)
    : IQueryHandler<ListChildrenQuery, IReadOnlyList<ChildDto>>
{
    public Task<IReadOnlyList<ChildDto>> HandleAsync(
        ListChildrenQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return directory.ListChildrenAsync(query.ParentUserId, cancellationToken);
    }
}

public sealed record LinkChildCommand(Guid ParentUserId, Guid StudentUserId)
    : ICommand<IReadOnlyList<ChildDto>>;

internal sealed class LinkChildHandler(
    ISchoolStructureRepository repository,
    ISchoolDirectory directory,
    IUserDirectory users) : ICommandHandler<LinkChildCommand, IReadOnlyList<ChildDto>>
{
    public async Task<IReadOnlyList<ChildDto>> HandleAsync(
        LinkChildCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var parent = await users.FindByIdAsync(command.ParentUserId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Parent not found.");

        if (!string.Equals(parent.Role, "Parent", StringComparison.Ordinal))
        {
            throw new DomainException("Only a parent account can be linked to a child.");
        }

        var student = await users.FindByIdAsync(command.StudentUserId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Student not found.");

        if (!string.Equals(student.Role, "Student", StringComparison.Ordinal))
        {
            throw new DomainException("Only a student account can be linked as a child.");
        }

        var existing = await repository
            .FindParentLinkAsync(command.ParentUserId, command.StudentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            repository.AddParentLink(ParentChild.Link(command.ParentUserId, command.StudentUserId));
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return await directory.ListChildrenAsync(command.ParentUserId, cancellationToken).ConfigureAwait(false);
    }
}

public sealed record UnlinkChildCommand(Guid ParentUserId, Guid StudentUserId)
    : ICommand<IReadOnlyList<ChildDto>>;

internal sealed class UnlinkChildHandler(
    ISchoolStructureRepository repository,
    ISchoolDirectory directory) : ICommandHandler<UnlinkChildCommand, IReadOnlyList<ChildDto>>
{
    public async Task<IReadOnlyList<ChildDto>> HandleAsync(
        UnlinkChildCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var link = await repository
            .FindParentLinkAsync(command.ParentUserId, command.StudentUserId, cancellationToken)
            .ConfigureAwait(false);

        if (link is not null)
        {
            repository.RemoveParentLink(link);
            await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return await directory.ListChildrenAsync(command.ParentUserId, cancellationToken).ConfigureAwait(false);
    }
}
