namespace Leyi.Domain.Common;

/// <summary>
/// Base type for entities identified by a client-generated <see cref="Guid"/>.
/// Identifiers are created by the domain (version 7 GUIDs: time-ordered, index friendly), never by the database.
/// </summary>
public abstract class Entity
{
    protected Entity()
    {
    }

    protected Entity(Guid id)
    {
        Id = Guard.NotEmpty(id, nameof(id));
    }

    public Guid Id { get; private set; }

    protected static Guid NewId() => Guid.CreateVersion7();
}
