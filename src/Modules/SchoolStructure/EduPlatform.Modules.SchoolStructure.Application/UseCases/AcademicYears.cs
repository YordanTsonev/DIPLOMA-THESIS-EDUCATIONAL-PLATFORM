using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.SchoolStructure.Application.Abstractions;
using EduPlatform.Modules.SchoolStructure.Contracts;
using EduPlatform.Modules.SchoolStructure.Domain;
using FluentValidation;

namespace EduPlatform.Modules.SchoolStructure.Application.UseCases;

public sealed record ListAcademicYearsQuery : IQuery<IReadOnlyList<AcademicYearDto>>;

internal sealed class ListAcademicYearsHandler(ISchoolStructureReadStore store)
    : IQueryHandler<ListAcademicYearsQuery, IReadOnlyList<AcademicYearDto>>
{
    public Task<IReadOnlyList<AcademicYearDto>> HandleAsync(
        ListAcademicYearsQuery query,
        CancellationToken cancellationToken) => store.ListAcademicYearsAsync(cancellationToken);
}

public sealed record CreateAcademicYearCommand(string Name, DateOnly StartsOn, DateOnly EndsOn, bool MakeCurrent)
    : ICommand<AcademicYearDto>;

internal sealed class CreateAcademicYearValidator : AbstractValidator<CreateAcademicYearCommand>
{
    public CreateAcademicYearValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(AcademicYear.MaxNameLength);
        RuleFor(command => command.EndsOn).GreaterThan(command => command.StartsOn);
    }
}

internal sealed class CreateAcademicYearHandler(ISchoolStructureRepository repository)
    : ICommandHandler<CreateAcademicYearCommand, AcademicYearDto>
{
    public async Task<AcademicYearDto> HandleAsync(
        CreateAcademicYearCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var existing = await repository.ListAcademicYearsAsync(cancellationToken).ConfigureAwait(false);

        if (existing.Any(year => string.Equals(year.Name, command.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException("An academic year with this name already exists.");
        }

        var academicYear = AcademicYear.Create(command.Name, command.StartsOn, command.EndsOn);

        if (command.MakeCurrent)
        {
            foreach (var year in existing.Where(year => year.IsCurrent))
            {
                year.ClearCurrent();
            }

            academicYear.MarkCurrent();
        }

        repository.AddAcademicYear(academicYear);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AcademicYearDto(
            academicYear.Id,
            academicYear.Name,
            academicYear.StartsOn,
            academicYear.EndsOn,
            academicYear.IsCurrent);
    }
}
