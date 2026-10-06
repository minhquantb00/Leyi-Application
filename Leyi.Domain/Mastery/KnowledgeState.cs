namespace Leyi.Domain.Mastery;

/// <summary>
/// Overall knowledge state (Learning Model §17, Domain Model §7.3).
/// <see cref="New"/> is derived (no <see cref="UserKnowledgeMastery"/> row exists) and is never persisted.
/// </summary>
public enum KnowledgeState
{
    New,
    Learning,
    Reviewing,
    Weak,
    Strong,
    Mastered,
}
