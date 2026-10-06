using Leyi.Application.Abstractions.Messaging;
using Leyi.Application.Abstractions.Results;

namespace Leyi.Application.Features.Identity.Logout;

/// <summary>
/// Revokes every active refresh token of the authenticated user. Idempotent; a later login starts a new session
/// (logout-all is not an account state). Issued access tokens stay valid until they expire (no blacklist in V1).
/// </summary>
internal sealed class LogoutAllHandler : ICommandHandler<LogoutAllCommand, Result<Unit>>
{
    private readonly IRefreshTokenRevoker refreshTokenRevoker;
    private readonly TimeProvider timeProvider;

    public LogoutAllHandler(IRefreshTokenRevoker refreshTokenRevoker, TimeProvider timeProvider)
    {
        this.refreshTokenRevoker = refreshTokenRevoker;
        this.timeProvider = timeProvider;
    }

    public async Task<Result<Unit>> HandleAsync(LogoutAllCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.UserId == Guid.Empty)
        {
            throw new ArgumentException("The authenticated user id is required.", nameof(command));
        }

        await refreshTokenRevoker.RevokeAllAsync(command.UserId, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        return Unit.Value;
    }
}
