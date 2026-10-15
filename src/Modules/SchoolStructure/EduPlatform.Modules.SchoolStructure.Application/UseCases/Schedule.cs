using EduPlatform.BuildingBlocks.Application.Messaging;
using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.SchoolStructure.Application.Abstractions;
using EduPlatform.Modules.SchoolStructure.Contracts;
using EduPlatform.Modules.SchoolStructure.Domain;
using FluentValidation;

namespace EduPlatform.Modules.SchoolStructure.Application.UseCases;

public sealed record GetScheduleQuery(Guid? SchoolClassId = null, Guid? TeacherUserId = null)
    : IQuery<IReadOnlyList<ScheduleSlotDto>>;

internal sealed class GetScheduleHandler(ISchoolStructureReadStore store)
    : IQueryHandler<GetScheduleQuery, IReadOnlyList<ScheduleSlotDto>>
{
    public Task<IReadOnlyList<ScheduleSlotDto>> HandleAsync(
        GetScheduleQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return store.ListScheduleAsync(query.SchoolClassId, query.TeacherUserId, cancellationToken);
    }
}

public sealed record GetMyScheduleQuery(Guid UserId, string Role) : IQuery<IReadOnlyList<ScheduleSlotDto>>;

internal sealed class GetMyScheduleHandler(ISchoolStructureReadStore store)
    : IQueryHandler<GetMyScheduleQuery, IReadOnlyList<ScheduleSlotDto>>
{
    public async Task<IReadOnlyList<ScheduleSlotDto>> HandleAsync(
        GetMyScheduleQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.Equals(query.Role, "Teacher", StringComparison.Ordinal))
        {
            return await store.ListScheduleAsync(null, query.UserId, cancellationToken).ConfigureAwait(false);
        }

        var schoolClassId = await store
            .FindClassOfStudentAsync(query.UserId, cancellationToken)
            .ConfigureAwait(false);

        return schoolClassId is null
            ? []
            : await store.ListScheduleAsync(schoolClassId, null, cancellationToken).ConfigureAwait(false);
    }
}

public sealed record AddScheduleSlotCommand(
    Guid TeachingAssignmentId,
    DayOfWeek DayOfWeek,
    int Period,
    string? Room) : ICommand<ScheduleSlotDto>;

internal sealed class AddScheduleSlotValidator : AbstractValidator<AddScheduleSlotCommand>
{
    public AddScheduleSlotValidator()
    {
        RuleFor(command => command.TeachingAssignmentId).NotEmpty();
        RuleFor(command => command.Period).InclusiveBetween(ScheduleSlot.MinPeriod, SchoolPeriods.Count);
        RuleFor(command => command.Room).MaximumLength(ScheduleSlot.MaxRoomLength);
    }
}

internal sealed class AddScheduleSlotHandler(
    ISchoolStructureRepository repository,
    ISchoolStructureReadStore store) : ICommandHandler<AddScheduleSlotCommand, ScheduleSlotDto>
{
    public async Task<ScheduleSlotDto> HandleAsync(
        AddScheduleSlotCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var assignment = await repository
            .FindAssignmentAsync(command.TeachingAssignmentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new DomainException("Teaching assignment not found.");

        var (startsAt, endsAt) = SchoolPeriods.For(command.Period);

        var slot = ScheduleSlot.Create(
            command.TeachingAssignmentId, command.DayOfWeek, command.Period, startsAt, endsAt, command.Room);

        var classSlots = await repository
            .ListSlotsForClassAsync(assignment.SchoolClassId, cancellationToken)
            .ConfigureAwait(false);

        if (classSlots.Any(existing => existing.Overlaps(slot)))
        {
            throw new DomainException("This class already has a lesson at that time.");
        }

        var teacherSlots = await repository
            .ListSlotsForTeacherAsync(assignment.TeacherUserId, cancellationToken)
            .ConfigureAwait(false);

        if (teacherSlots.Any(existing => existing.Overlaps(slot)))
        {
            throw new DomainException("This teacher already has a lesson at that time.");
        }

        repository.AddSlot(slot);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var schedule = await store
            .ListScheduleAsync(assignment.SchoolClassId, null, cancellationToken)
            .ConfigureAwait(false);

        return schedule.Single(item => item.Id == slot.Id);
    }
}

public sealed record RemoveScheduleSlotCommand(Guid SlotId) : ICommand;

internal sealed class RemoveScheduleSlotHandler(ISchoolStructureRepository repository)
    : CommandHandler<RemoveScheduleSlotCommand>
{
    protected override async Task HandleCoreAsync(
        RemoveScheduleSlotCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var slot = await repository.FindSlotAsync(command.SlotId, cancellationToken).ConfigureAwait(false)
            ?? throw new DomainException("Schedule slot not found.");

        repository.RemoveSlot(slot);
        await repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
