namespace EduPlatform.BuildingBlocks.Domain;

/// <summary>
/// Base class for entities. Identity is the only thing that decides equality —
/// two rows with the same id are the same entity, whatever their other values.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    protected Entity(Guid id) => Id = id;

    /// <summary>Required by EF Core materialisation.</summary>
    protected Entity() { }

    /// <remarks>
    /// UUIDv7 is time-ordered, which keeps B-tree index inserts sequential.
    /// Random UUIDv4 primary keys fragment the index and slow inserts as tables grow.
    /// </remarks>
    public Guid Id { get; protected init; } = Guid.CreateVersion7();

    public bool Equals(Entity? other) =>
        other is not null && other.GetType() == GetType() && other.Id == Id;

    public override bool Equals(object? obj) => Equals(obj as Entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}
