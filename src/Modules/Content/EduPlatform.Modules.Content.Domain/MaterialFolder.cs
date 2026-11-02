using EduPlatform.BuildingBlocks.Domain;

namespace EduPlatform.Modules.Content.Domain;

public sealed class MaterialFolder : AggregateRoot
{
    public const int MaxNameLength = 120;

    private MaterialFolder(Guid id, Guid teachingAssignmentId, string name)
        : base(id)
    {
        TeachingAssignmentId = teachingAssignmentId;
        Name = name;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    private MaterialFolder() { }

    public Guid TeachingAssignmentId { get; private set; }

    public string Name { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public static MaterialFolder Create(Guid teachingAssignmentId, string? name)
    {
        if (teachingAssignmentId == Guid.Empty)
        {
            throw new DomainException("A teaching assignment is required.");
        }

        return new MaterialFolder(Guid.CreateVersion7(), teachingAssignmentId, Validate(name));
    }

    public void Rename(string? name) => Name = Validate(name);

    private static string Validate(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            throw new DomainException("Folder name is required.");
        }

        return trimmed.Length > MaxNameLength
            ? throw new DomainException($"Folder name must not exceed {MaxNameLength} characters.")
            : trimmed;
    }
}
