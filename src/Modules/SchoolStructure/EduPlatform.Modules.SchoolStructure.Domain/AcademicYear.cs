using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.SchoolStructure.Domain;

public sealed class AcademicYear : AggregateRoot
{
    public const int MaxNameLength = 20;

    private AcademicYear(Guid id, string name, DateOnly startsOn, DateOnly endsOn)
        : base(id)
    {
        Name = name;
        StartsOn = startsOn;
        EndsOn = endsOn;
    }

    private AcademicYear() { }

    public string Name { get; private set; } = null!;

    public DateOnly StartsOn { get; private set; }

    public DateOnly EndsOn { get; private set; }

    public bool IsCurrent { get; private set; }

    public static AcademicYear Create(string? name, DateOnly startsOn, DateOnly endsOn)
    {
        var trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new DomainException("Academic year name is required.");
        }

        if (trimmed.Length > MaxNameLength)
        {
            throw new DomainException($"Academic year name must not exceed {MaxNameLength} characters.");
        }

        if (endsOn <= startsOn)
        {
            throw new DomainException("An academic year must end after it starts.");
        }

        return new AcademicYear(Guid.CreateVersion7(), trimmed, startsOn, endsOn);
    }

    public bool Covers(DateOnly date) => date >= StartsOn && date <= EndsOn;

    public void MarkCurrent() => IsCurrent = true;

    public void ClearCurrent() => IsCurrent = false;
}
