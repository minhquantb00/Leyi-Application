using FluentValidation;
using Leyi.Application.Abstractions.Messaging;
using Leyi.Application.Abstractions.Results;

namespace Leyi.Application.Features.Identity.Logout;

/// <summary>
/// Revokes the family of the presented refresh token for the authenticated user. Always succeeds once the input is
/// valid, so the response never reveals whether the token exists or whose it is. Not reuse detection: presenting an
/// old rotated token here is an explicit user action. Account status is not checked: a disabled account may still end
/// its sessions. Issued access tokens stay valid until they expire (no blacklist in V1).
/// </summary>
internal sealed class LogoutHandler : ICommandHandler<LogoutCommand, Result<Unit>>
{
    private readonly IValidator<LogoutCommand> validator;
    private readonly IRefreshTokenRevoker refreshTokenRevoker;
    private readonly TimeProvider timeProvider;

    public LogoutHandler(IValidator<LogoutCommand> validator, IRefreshTokenRevoker refreshTokenRevoker, TimeProvider timeProvider)
    {
        this.validator = validator;
        this.refreshTokenRevoker = refreshTokenRevoker;
        this.timeProvider = timeProvider;
    }

    public async Task<Result<Unit>> HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.UserId == Guid.Empty)
        {
            throw new ArgumentException("The authenticated user id is required.", nameof(command));
        }

        var input = command with { RefreshToken = command.RefreshToken ?? string.Empty };

        var validation = await validator.ValidateAsync(input, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        await refreshTokenRevoker.RevokeFamilyAsync(input.UserId, input.RefreshToken, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        return Unit.Value;
    }
}
