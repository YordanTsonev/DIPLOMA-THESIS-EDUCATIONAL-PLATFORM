using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.SchoolStructure.Application.Abstractions;
using EduPlatform.Modules.SchoolStructure.Contracts;
using EduPlatform.Modules.SchoolStructure.Domain;
using FluentValidation;

namespace EduPlatform.Modules.SchoolStructure.Application.UseCases;

public sealed record GetCalendarQuery(DateOnly From, DateOnly To, Guid? SchoolClassId = null)
    : IQuery<IReadOnlyList<CalendarEventDto>>;

internal sealed class GetCalendarValidator : AbstractValidator<GetCalendarQuery>
{
    public GetCalendarValidator()
    {
        RuleFor(query => query.To).GreaterThanOrEqualTo(query => query.From);

        RuleFor(query => query)
            .Must(query => query.To.DayNumber - query.From.DayNumber <= 366)
            .WithMessage("A calendar range cannot span more than a year.");
    }
}

internal sealed class GetCalendarHandler(ISchoolStructureReadStore store)
    : IQueryHandler<GetCalendarQuery, IReadOnlyList<CalendarEventDto>>
{
    public Task<IReadOnlyList<CalendarEventDto>> HandleAsync(
        GetCalendarQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return store.ListCalendarAsync(query.From, query.To, query.SchoolClassId, cancellationToken);
    }
}

public sealed record GetMyCalendarQuery(Guid UserId, string Role, DateOnly From, DateOnly To)
    : IQuery<IReadOnlyList<CalendarEventDto>>;

internal sealed class GetMyCalendarHandler(ISchoolStructureReadStore store)
    : IQueryHandler<GetMyCalendarQuery, IReadOnlyList<CalendarEventDto>>
{
    public async Task<IReadOnlyList<CalendarEventDto>> HandleAsync(
        GetMyCalendarQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.Equals(query.Role, "Student", StringComparison.Ordinal))
        {
            var schoolClassId = await store
                .FindClassOfStudentAsync(query.UserId, cancellationToken)
                .ConfigureAwait(false);

            return await store
                .ListCalendarAsync(query.From, query.To, schoolClassId, cancellationToken)
                .ConfigureAwait(false);
        }

        if (string.Equals(query.Role, "Teacher", StringComparison.Ordinal))
        {
            var classIds = await store
                .ListClassesOfTeacherAsync(query.UserId, cancellationToken)
                .ConfigureAwait(false);

            var all = await store
                .ListCalendarAsync(query.From, query.To, null, cancellationToken)
                .ConfigureAwait(false);

            return all
                .Where(item => item.IsSchoolWide || classIds.Contains(item.SchoolClassId!.Value))
                .ToList();
        }

        return await store.ListCalendarAsync(query.From, query.To, null, cancellationToken).ConfigureAwait(false);
    }
}

public sealed record CreateCalendarEventCommand(
    CalendarEventType Type,
    string Title,
    DateOnly StartsOn,
    DateOnly? EndsOn,
    Guid CreatedByUserId,
    Guid? SchoolClassId,
    Guid? SubjectId,
    int? Period,
    string? Description) : ICommand<CalendarEventDto>;

internal sealed class CreateCalendarEventValidator : AbstractValidator<CreateCalendarEventCommand>
{
    public CreateCalendarEventValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(CalendarEvent.MaxTitleLength);
        RuleFor(command => command.Description).MaximumLength(CalendarEvent.MaxDescriptionLength);
        RuleFor(command => command.Type).IsInEnum();
        RuleFor(command => command.CreatedByUserId).NotEmpty();
    }
}

internal sealed class CreateCalendarEventHandler(
    ISchoolStructureRepository repository,
    ISchoolStructureReadStore store) : ICommandHandler<CreateCalendarEventCommand, CalendarEventDto>
{
    public async Task<CalendarEventDto> HandleAsync(
        CreateCalendarEventCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.SchoolClassId is { } classId)
        {
            _ = await repository.FindClassAsync(classId, cancellationToken).ConfigureAwait(false)
                ?? throw new DomainException("Class not found.");
        }

        if (command.SubjectId is { } subjectId)
        {
            _ = await repository.FindSubjectAsync(subjectId, cancellationToken).ConfigureAwait(false)
                ?? throw new DomainException("Subject not found.");
        }

        var calendarEvent = CalendarEvent.Create(
            command.Type,
            command.Title,
            command.StartsOn,
            command.EndsOn,
            command.CreatedByUserId,
            command.SchoolClassId,
            command.SubjectId,
            command.Period,
            command.Description);

        repository.AddCalendarEvent(calendarEvent);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var created = await store
            .ListCalendarAsync(calendarEvent.StartsOn, calendarEvent.EndsOn, null, cancellationToken)
            .ConfigureAwait(false);

        return created.Single(item => item.Id == calendarEvent.Id);
    }
}

public sealed record DeleteCalendarEventCommand(Guid EventId) : ICommand;

internal sealed class DeleteCalendarEventHandler(ISchoolStructureRepository repository)
    : CommandHandler<DeleteCalendarEventCommand>
{
    protected override async Task HandleCoreAsync(
        DeleteCalendarEventCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var calendarEvent = await repository
            .FindCalendarEventAsync(command.EventId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException("Event not found.");

        repository.RemoveCalendarEvent(calendarEvent);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
