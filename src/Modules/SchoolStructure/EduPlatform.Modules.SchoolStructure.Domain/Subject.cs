using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.SchoolStructure.Domain;

public sealed class Subject : AggregateRoot
{
    public const int MaxNameLength = 120;
    public const int MaxCodeLength = 16;

    private Subject(Guid id, string name, string code)
        : base(id)
    {
        Name = name;
        Code = code;
    }

    private Subject() { }

    public string Name { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    public bool IsActive { get; private set; } = true;

    public static Subject Create(string? name, string? code)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        var trimmedCode = code?.Trim().ToUpperInvariant() ?? string.Empty;

        if (trimmedName.Length == 0)
        {
            throw new DomainException("Subject name is required.");
        }

        if (trimmedName.Length > MaxNameLength)
        {
            throw new DomainException($"Subject name must not exceed {MaxNameLength} characters.");
        }

        if (trimmedCode.Length == 0)
        {
            throw new DomainException("Subject code is required.");
        }

        if (trimmedCode.Length > MaxCodeLength)
        {
            throw new DomainException($"Subject code must not exceed {MaxCodeLength} characters.");
        }

        return new Subject(Guid.CreateVersion7(), trimmedName, trimmedCode);
    }

    public void Rename(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new DomainException("Subject name is required.");
        }

        if (trimmed.Length > MaxNameLength)
        {
            throw new DomainException($"Subject name must not exceed {MaxNameLength} characters.");
        }

        Name = trimmed;
    }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
