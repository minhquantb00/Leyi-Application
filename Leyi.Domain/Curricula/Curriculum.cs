using Leyi.Domain.Common;

namespace Leyi.Domain.Curricula;

/// <summary>
/// Curriculum reference data (ERD §6.5). Personal progress never lives here (Domain Model §6).
/// Content that has learning history is archived, never hard-deleted.
/// </summary>
public sealed class Curriculum : Entity
{
    private Curriculum()
    {
    }

    private Curriculum(Guid id, string code, string name, string version, string? standard)
        : base(id)
    {
        Code = code;
        Name = name;
        Version = version;
        Standard = standard;
        Status = PublicationStatus.Draft;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Version { get; private set; } = string.Empty;

    public string? Standard { get; private set; }

    public PublicationStatus Status { get; private set; }

    public static Curriculum Create(string code, string name, string version, string? standard = null) =>
        new(NewId(), Guard.NotBlank(code), Guard.NotBlank(name), Guard.NotBlank(version), Guard.NullIfBlank(standard));

    public void Activate() => Status = PublicationStatus.Active;

    public void Archive() => Status = PublicationStatus.Archived;
}
