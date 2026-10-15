using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.SchoolStructure.Domain;

public sealed class ScheduleSlot : AggregateRoot
{
    public const int MinPeriod = 1;
    public const int MaxPeriod = 10;
    public const int MaxRoomLength = 20;

    private ScheduleSlot(
        Guid id,
        Guid teachingAssignmentId,
        DayOfWeek dayOfWeek,
        int period,
        TimeOnly startsAt,
        TimeOnly endsAt,
        string? room)
        : base(id)
    {
        TeachingAssignmentId = teachingAssignmentId;
        DayOfWeek = dayOfWeek;
        Period = period;
        StartsAt = startsAt;
        EndsAt = endsAt;
        Room = room;
    }

    private ScheduleSlot() { }

    public Guid TeachingAssignmentId { get; private set; }

    public DayOfWeek DayOfWeek { get; private set; }

    public int Period { get; private set; }

    public TimeOnly StartsAt { get; private set; }

    public TimeOnly EndsAt { get; private set; }

    public string? Room { get; private set; }

    public static ScheduleSlot Create(
        Guid teachingAssignmentId,
        DayOfWeek dayOfWeek,
        int period,
        TimeOnly startsAt,
        TimeOnly endsAt,
        string? room = null)
    {
        if (teachingAssignmentId == Guid.Empty)
        {
            throw new DomainException("A teaching assignment is required.");
        }

        if (dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            throw new DomainException("Lessons cannot be scheduled at the weekend.");
        }

        if (period is < MinPeriod or > MaxPeriod)
        {
            throw new DomainException($"Period must be between {MinPeriod} and {MaxPeriod}.");
        }

        if (endsAt <= startsAt)
        {
            throw new DomainException("A lesson must end after it starts.");
        }

        var trimmedRoom = room?.Trim();

        if (trimmedRoom is { Length: 0 })
        {
            trimmedRoom = null;
        }

        if (trimmedRoom is { Length: > MaxRoomLength })
        {
            throw new DomainException($"Room must not exceed {MaxRoomLength} characters.");
        }

        return new ScheduleSlot(
            Guid.CreateVersion7(), teachingAssignmentId, dayOfWeek, period, startsAt, endsAt, trimmedRoom);
    }

    public bool Overlaps(ScheduleSlot other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return DayOfWeek == other.DayOfWeek && StartsAt < other.EndsAt && other.StartsAt < EndsAt;
    }

    public void Move(DayOfWeek dayOfWeek, int period, TimeOnly startsAt, TimeOnly endsAt)
    {
        if (dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            throw new DomainException("Lessons cannot be scheduled at the weekend.");
        }

        if (period is < MinPeriod or > MaxPeriod)
        {
            throw new DomainException($"Period must be between {MinPeriod} and {MaxPeriod}.");
        }

        if (endsAt <= startsAt)
        {
            throw new DomainException("A lesson must end after it starts.");
        }

        DayOfWeek = dayOfWeek;
        Period = period;
        StartsAt = startsAt;
        EndsAt = endsAt;
    }
}
