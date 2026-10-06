using FluentValidation;
using Leyi.Application.Abstractions.Authentication;
using Leyi.Application.Abstractions.Messaging;
using Leyi.Application.Abstractions.Results;

namespace Leyi.Application.Features.Identity.Login;

/// <summary>
/// Verifies credentials, then issues the access token and persists the new refresh token family in one transaction.
/// The token pair is returned only after the session has been persisted. Login does not create accounts, check
/// email confirmation (I3) or touch onboarding/learning plans.
/// Author: Trần Minh Quân
/// Created date: 2026-10-06
/// Last modified date: 2026-10-06
/// </summary>
internal sealed class LoginHandler : ICommandHandler<LoginCommand, Result<LoginResult>>
{
    private readonly IValidator<LoginCommand> validator;
    private readonly ICredentialVerifier credentialVerifier;
    private readonly IAccessTokenIssuer accessTokenIssuer;
    private readonly ILoginSessionStore loginSessionStore;
    private readonly TimeProvider timeProvider;

    public LoginHandler(
        IValidator<LoginCommand> validator,
        ICredentialVerifier credentialVerifier,
        IAccessTokenIssuer accessTokenIssuer,
        ILoginSessionStore loginSessionStore,
        TimeProvider timeProvider)
    {
        this.validator = validator;
        this.credentialVerifier = credentialVerifier;
        this.accessTokenIssuer = accessTokenIssuer;
        this.loginSessionStore = loginSessionStore;
        this.timeProvider = timeProvider;
    }

    public async Task<Result<LoginResult>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var input = command with
        {
            Email = command.Email?.Trim() ?? string.Empty,
            DeviceId = string.IsNullOrWhiteSpace(command.DeviceId) ? null : command.DeviceId.Trim(),
        };

        var validation = await validator.ValidateAsync(input, cancellationToken);
        if (!validation.IsValid)
        {
            return validation.ToError();
        }

        var verification = await credentialVerifier.VerifyAsync(input.Email, input.Password, cancellationToken);

        switch (verification.Status)
        {
            case CredentialVerificationStatus.Authenticated:
                break;
            case CredentialVerificationStatus.InvalidCredentials:
                return LoginErrors.InvalidCredentials;
            case CredentialVerificationStatus.AccountDisabled:
                return LoginErrors.AccountDisabled;
            default:
                throw new InvalidOperationException($"Unknown credential verification status {verification.Status}.");
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        // Signing has no side effects; persisting the session last means a failure leaves no token pair behind.
        var accessToken = accessTokenIssuer.Issue(verification.UserId, nowUtc);
        var refreshToken = await loginSessionStore.StartSessionAsync(verification.UserId, input.DeviceId, nowUtc, cancellationToken);

        return new LoginResult(accessToken.Value, accessToken.ExpiresAtUtc, refreshToken);
    }
}
