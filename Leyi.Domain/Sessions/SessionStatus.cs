namespace Leyi.Domain.Sessions;

/// <summary>
/// Learning session status (ERD §6.18, Domain Model §10.2). GENERATED / IN_PROGRESS / PAUSED are "open";
/// at most one open session exists per user. V1 starts a session in the same use case that creates it,
/// so GENERATED is a domain transition and is not persisted long-term.
/// </summary>
public enum SessionStatus
{
    Generated,
    InProgress,
    Paused,
    Completed,
    Abandoned,
    Expired,
}

public static class SessionStatusExtensions
{
    /// <summary>Statuses covered by the one-open-session-per-user rule (filtered unique index).</summary>
    public static readonly IReadOnlyList<SessionStatus> OpenStatuses = [SessionStatus.Generated, SessionStatus.InProgress, SessionStatus.Paused];

    /// <summary>Statuses of a session that has ended; such a session is "meaningful" for cold-start with enough evaluated activities (ASE §25).</summary>
    public static readonly IReadOnlyList<SessionStatus> EndedStatuses = [SessionStatus.Completed, SessionStatus.Abandoned, SessionStatus.Expired];

    public static bool IsOpen(this SessionStatus status) => OpenStatuses.Contains(status);
}
