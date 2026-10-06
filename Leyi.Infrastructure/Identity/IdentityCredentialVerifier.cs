using Leyi.Application.Features.Identity.Login;
using Leyi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Leyi.Infrastructure.Identity;

/// <summary>
/// <see cref="ICredentialVerifier"/> backed by ASP.NET Core Identity (lookup by normalized email, password hash
/// verification, lockout). Order of checks (Login.md §4):
/// <list type="number">
/// <item>unknown email → invalid credentials (a dummy hash is verified so the timing matches a wrong password);</item>
/// <item>locked out → disabled, before the password is checked, so lockout cannot be probed with a correct password;</item>
/// <item>wrong password → invalid credentials and a failed attempt is recorded (Identity lockout);</item>
/// <item>correct password but <see cref="ApplicationUser.Status"/> ≠ ACTIVE → disabled.</item>
/// </list>
/// <see cref="ApplicationUser.EmailConfirmed"/> is not checked (I3). Neither the email nor the password is logged.
/// </summary>
internal sealed partial class IdentityCredentialVerifier : ICredentialVerifier
{
    private static string? dummyPasswordHash;

    private readonly UserManager<ApplicationUser> userManager;
    private readonly LeyiDbContext dbContext;
    private readonly ILogger<IdentityCredentialVerifier> logger;

    public IdentityCredentialVerifier(UserManager<ApplicationUser> userManager, LeyiDbContext dbContext, ILogger<IdentityCredentialVerifier> logger)
    {
        this.userManager = userManager;
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<CredentialVerification> VerifyAsync(string email, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // UserManager has no CancellationToken parameters; each call is a single short query/update.
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            VerifyDummyPassword(password);
            return CredentialVerification.InvalidCredentials();
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            LogLockedOutLogin(logger, user.Id);
            return CredentialVerification.AccountDisabled();
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await RecordFailedAttemptAsync(user);
            return CredentialVerification.InvalidCredentials();
        }

        if (user.Status != UserStatus.Active)
        {
            LogInactiveLogin(logger, user.Id);
            return CredentialVerification.AccountDisabled();
        }

        if (await userManager.GetAccessFailedCountAsync(user) > 0)
        {
            await ResetFailedAttemptsAsync(user);
        }

        return CredentialVerification.Authenticated(user.Id);
    }

    private void VerifyDummyPassword(string password)
    {
        var hasher = userManager.PasswordHasher;
        var hash = dummyPasswordHash ??= hasher.HashPassword(new ApplicationUser(), Guid.NewGuid().ToString("N"));
        _ = hasher.VerifyHashedPassword(new ApplicationUser(), hash, password);
    }

    private async Task RecordFailedAttemptAsync(ApplicationUser user)
    {
        var result = await userManager.AccessFailedAsync(user);
        if (!result.Succeeded)
        {
            // Concurrent failed attempts may hit a concurrency stamp conflict; the login still fails as invalid.
            LogFailedAttemptNotRecorded(logger, user.Id, string.Join(", ", result.Errors.Select(error => error.Code)));
        }
    }

    private async Task ResetFailedAttemptsAsync(ApplicationUser user)
    {
        var result = await userManager.ResetAccessFailedCountAsync(user);
        if (!result.Succeeded)
        {
            // Keep the failed count (conservative) and make sure the stale change is not saved with the new session.
            dbContext.Entry(user).State = EntityState.Detached;
            LogFailedAttemptsNotReset(logger, user.Id, string.Join(", ", result.Errors.Select(error => error.Code)));
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Login rejected for locked-out account {UserId}")]
    private static partial void LogLockedOutLogin(ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Login rejected for inactive account {UserId}")]
    private static partial void LogInactiveLogin(ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed login attempt for {UserId} was not recorded: {ErrorCodes}")]
    private static partial void LogFailedAttemptNotRecorded(ILogger logger, Guid userId, string errorCodes);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed login count for {UserId} was not reset: {ErrorCodes}")]
    private static partial void LogFailedAttemptsNotReset(ILogger logger, Guid userId, string errorCodes);
}
