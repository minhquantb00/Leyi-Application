namespace Leyi.Domain.Sessions;

/// <summary>
/// Session activity status (ERD §6.19). SKIPPED is reserved: V1 has no skip operation.
/// </summary>
public enum ActivityStatus
{
    Planned,
    InProgress,
    Completed,
    Skipped,
    Cancelled,
}
