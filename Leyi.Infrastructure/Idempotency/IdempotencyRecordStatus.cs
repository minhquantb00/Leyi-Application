namespace Leyi.Infrastructure.Idempotency;

/// <summary>
/// Stored as IN_PROGRESS / COMPLETED (ERD §6.28).
/// </summary>
public enum IdempotencyRecordStatus
{
    InProgress = 1,
    Completed = 2,
}
