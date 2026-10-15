using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.SchoolStructure.Domain;

public enum CalendarEventType
{
    Test = 1,
    Exam = 2,
    Deadline = 3,
    Event = 4,
    Holiday = 5,
}

public sealed class CalendarEvent : AggregateRoot
{
    public const int MaxTitleLength = 160;
    public const int MaxDescriptionLength = 1000;

    private CalendarEvent(
        Guid id,
        CalendarEventType type,
        string title,
        DateOnly startsOn,
        DateOnly endsOn,
        Guid? schoolClassId,
        Guid? subjectId,
        int? period,
        string? description,
        Guid createdByUserId)
        : base(id)
    {
        Type = type;
        Title = title;
        StartsOn = startsOn;
        EndsOn = endsOn;
        SchoolClassId = schoolClassId;
        SubjectId = subjectId;
        Period = period;
        Description = description;
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private CalendarEvent() { }

    public CalendarEventType Type { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public DateOnly StartsOn { get; private set; }

    public DateOnly EndsOn { get; private set; }

    public Guid? SchoolClassId { get; private set; }

    public Guid? SubjectId { get; private set; }

    public int? Period { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsSchoolWide => SchoolClassId is null;

    public static CalendarEvent Create(
        CalendarEventType type,
        string? title,
        DateOnly startsOn,
        DateOnly? endsOn,
        Guid createdByUserId,
        Guid? schoolClassId = null,
        Guid? subjectId = null,
        int? period = null,
        string? description = null)
    {
        var trimmedTitle = title?.Trim() ?? string.Empty;

        if (trimmedTitle.Length == 0)
        {
            throw new DomainException("An event title is required.");
        }

        if (trimmedTitle.Length > MaxTitleLength)
        {
            throw new DomainException($"Title must not exceed {MaxTitleLength} characters.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new DomainException($"'{type}' is not a known event type.");
        }

        var last = endsOn ?? startsOn;

        if (last < startsOn)
        {
            throw new DomainException("An event cannot end before it starts.");
        }

        if (period is not null && (period < ScheduleSlot.MinPeriod || period > SchoolPeriods.Count))
        {
            throw new DomainException($"Period must be between {ScheduleSlot.MinPeriod} and {SchoolPeriods.Count}.");
        }

        if (type is CalendarEventType.Test or CalendarEventType.Exam && schoolClassId is null)
        {
            throw new DomainException("A test or an exam must belong to a class.");
        }

        if (last != startsOn && period is not null)
        {
            throw new DomainException("An event spanning several days cannot be tied to a single period.");
        }

        var trimmedDescription = description?.Trim();

        if (trimmedDescription is { Length: 0 })
        {
            trimmedDescription = null;
        }

        if (trimmedDescription is { Length: > MaxDescriptionLength })
        {
            throw new DomainException($"Description must not exceed {MaxDescriptionLength} characters.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new DomainException("The author of the event is required.");
        }

        return new CalendarEvent(
            Guid.CreateVersion7(),
            type,
            trimmedTitle,
            startsOn,
            last,
            schoolClassId,
            subjectId,
            period,
            trimmedDescription,
            createdByUserId);
    }

    public bool OccursOn(DateOnly date) => date >= StartsOn && date <= EndsOn;

    public bool OverlapsRange(DateOnly from, DateOnly to) => StartsOn <= to && from <= EndsOn;

    public void Reschedule(DateOnly startsOn, DateOnly? endsOn)
    {
        var last = endsOn ?? startsOn;

        if (last < startsOn)
        {
            throw new DomainException("An event cannot end before it starts.");
        }

        if (last != startsOn && Period is not null)
        {
            throw new DomainException("An event spanning several days cannot be tied to a single period.");
        }

        StartsOn = startsOn;
        EndsOn = last;
    }
}
