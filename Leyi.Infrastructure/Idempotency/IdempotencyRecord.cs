namespace Leyi.Infrastructure.Idempotency;

/// <summary>
/// Persistence model for <c>Idempotency-Key</c> handling (ERD §6.28), unique per user + endpoint + key.
/// Replay semantics, header requirement, payload-mismatch handling and retention are open (C6);
/// no request-handling behavior is implemented here.
/// </summary>
public sealed class IdempotencyRecord
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Logical action, e.g. <c>SubmitAttempt</c>.</summary>
    public required string Endpoint { get; set; }

    public required string IdempotencyKey { get; set; }

    /// <summary>Hash of the request payload, used to detect key reuse with a different payload.</summary>
    public required string RequestHash { get; set; }

    public IdempotencyRecordStatus Status { get; set; }

    public short? ResponseStatusCode { get; set; }

    public string? ResponseBody { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime ExpiresAt { get; set; }
}
