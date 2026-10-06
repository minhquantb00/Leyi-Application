namespace Leyi.Application.Features.Identity.Login;

/// <summary>
/// Persists a new login (device session) atomically: records the login time, starts a new refresh token family and
/// revokes the oldest active families beyond the per-user limit (Login.md §4, I1). All policy values come from the
/// token policy options.
/// </summary>
public interface ILoginSessionStore
{
    /// <returns>The raw refresh token, returned to the client once. Only its hash is persisted.</returns>
    Task<string> StartSessionAsync(Guid userId, string? deviceId, DateTime nowUtc, CancellationToken cancellationToken);
}
