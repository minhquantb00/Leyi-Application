using FluentValidation;
using Leyi.Application.Abstractions.Authentication;
using Leyi.Application.Abstractions.Messaging;
using Leyi.Application.Abstractions.Results;

namespace Leyi.Application.Features.Identity.RefreshAccessToken;

/// <summary>
/// Validates input, then rotates the refresh token. The access token is issued through the rotator callback after the
/// token and account checks and before the rotation is committed; the token pair is returned only after the commit.
/// No grace period (V1): any reuse of a rotated token revokes the family.
/// </summary>
internal sealed class RefreshAccessTokenHandler : ICommandHandler<RefreshAccessTokenCommand, Result<RefreshAccessTokenResult>>
{
    private readonly IValidator<RefreshAccessTokenCommand> validator;
    private readonly IRefreshTokenRotator refreshTokenRotator;
    private readonly IAccessTokenIssuer accessTokenIssuer;
    private readonly TimeProvider timeProvider;

    public RefreshAccessTokenHandler(
        IValidator<RefreshAccessTokenCommand> validator,
        IRefreshTokenRotator refreshTokenRotator,
        IAccessTokenIssuer accessTokenIssuer,
        TimeProvider timeProvider)
    {
        this.validator = validator;
        this.refreshTokenRotator = refreshTokenRotator;
        this.accessTokenIssuer = accessTokenIssuer;
        this.timeProvider = timeProvider;
    }

    public async Task<Result<RefreshAccessTokenResult>> HandleAsync(RefreshAccessTokenCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var input = command with { RefreshToken = command.RefreshToken ?? string.Empty };

        var validation = await validator.ValidateAsync(input, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var rotation = await refreshTokenRotator.RotateAsync(
            input.RefreshToken,
            nowUtc,
            userId => accessTokenIssuer.Issue(userId, nowUtc),
            cancellationToken);

        return rotation.Status switch
        {
            RefreshTokenRotationStatus.Rotated when rotation is { AccessToken: { } accessToken, RefreshToken: { } refreshToken } =>
                new RefreshAccessTokenResult(accessToken.Value, accessToken.ExpiresAtUtc, refreshToken),
            RefreshTokenRotationStatus.Invalid => RefreshAccessTokenErrors.Invalid,
            RefreshTokenRotationStatus.Revoked or RefreshTokenRotationStatus.ReuseDetected => RefreshAccessTokenErrors.Revoked,
            RefreshTokenRotationStatus.AccountDisabled => RefreshAccessTokenErrors.AccountDisabled,
            _ => throw new InvalidOperationException($"Unexpected refresh token rotation outcome {rotation.Status}."),
        };
    }
}
