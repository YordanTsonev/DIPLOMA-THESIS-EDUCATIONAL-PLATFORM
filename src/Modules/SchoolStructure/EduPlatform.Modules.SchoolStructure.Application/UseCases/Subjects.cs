using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.SchoolStructure.Application.Abstractions;
using EduPlatform.Modules.SchoolStructure.Contracts;
using EduPlatform.Modules.SchoolStructure.Domain;
using FluentValidation;

namespace EduPlatform.Modules.SchoolStructure.Application.UseCases;

public sealed record ListSubjectsQuery(bool IncludeInactive = false) : IQuery<IReadOnlyList<SubjectDto>>;

internal sealed class ListSubjectsHandler(ISchoolStructureReadStore store)
    : IQueryHandler<ListSubjectsQuery, IReadOnlyList<SubjectDto>>
{
    public Task<IReadOnlyList<SubjectDto>> HandleAsync(ListSubjectsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return store.ListSubjectsAsync(query.IncludeInactive, cancellationToken);
    }
}

public sealed record CreateSubjectCommand(string Name, string Code) : ICommand<SubjectDto>;

internal sealed class CreateSubjectValidator : AbstractValidator<CreateSubjectCommand>
{
    public CreateSubjectValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Subject.MaxNameLength);
        RuleFor(command => command.Code).NotEmpty().MaximumLength(Subject.MaxCodeLength);
    }
}

internal sealed class CreateSubjectHandler(ISchoolStructureRepository repository)
    : ICommandHandler<CreateSubjectCommand, SubjectDto>
{
    public async Task<SubjectDto> HandleAsync(CreateSubjectCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var code = command.Code.Trim().ToUpperInvariant();

        if (await repository.SubjectCodeExistsAsync(code, cancellationToken).ConfigureAwait(false))
        {
            throw new DomainException($"A subject with code '{code}' already exists.");
        }

        var subject = Subject.Create(command.Name, command.Code);
        repository.AddSubject(subject);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new SubjectDto(subject.Id, subject.Name, subject.Code, subject.IsActive);
    }
}

public sealed record SetSubjectActiveCommand(Guid SubjectId, bool IsActive) : ICommand<SubjectDto>;

internal sealed class SetSubjectActiveHandler(ISchoolStructureRepository repository)
    : ICommandHandler<SetSubjectActiveCommand, SubjectDto>
{
    public async Task<SubjectDto> HandleAsync(SetSubjectActiveCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var subject = await repository.FindSubjectAsync(command.SubjectId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Subject not found.");

        if (command.IsActive)
        {
            subject.Reactivate();
        }
        else
        {
            subject.Deactivate();
        }

        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new SubjectDto(subject.Id, subject.Name, subject.Code, subject.IsActive);
    }
}
