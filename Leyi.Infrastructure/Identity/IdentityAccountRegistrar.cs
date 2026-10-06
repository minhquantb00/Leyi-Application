using Leyi.Application.Features.Identity.RegisterAccount;
using Leyi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Leyi.Infrastructure.Identity;

/// <summary>
/// <see cref="IAccountRegistrar"/> backed by ASP.NET Core Identity. Identity normalizes the email, checks
/// uniqueness and hashes the password; the unique indexes on <c>Users</c> (<c>UX_Users_NormalizedEmail</c>,
/// <c>UserNameIndex</c>) catch concurrent registrations that pass the Identity pre-check.
/// Identity error descriptions are never returned to the caller, and neither the email nor the password is logged.
/// </summary>
internal sealed partial class IdentityAccountRegistrar : IAccountRegistrar
{
    private static readonly string[] UserUniqueIndexes = ["UX_Users_NormalizedEmail", "UserNameIndex"];

    private readonly UserManager<ApplicationUser> userManager;
    private readonly LeyiDbContext dbContext;
    private readonly ILogger<IdentityAccountRegistrar> logger;

    public IdentityAccountRegistrar(UserManager<ApplicationUser> userManager, LeyiDbContext dbContext, ILogger<IdentityAccountRegistrar> logger)
    {
        this.userManager = userManager;
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<AccountRegistration> RegisterAsync(string email, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            Email = email,
            EmailConfirmed = false,
        };

        IdentityResult result;
        try
        {
            // UserManager has no CancellationToken parameter; creation is a single short SaveChanges.
            result = await userManager.CreateAsync(user, password);
        }
        catch (DbUpdateException exception) when (IsDuplicateUser(exception))
        {
            // Leave the scoped context clean: the rejected user must not be inserted by a later SaveChanges.
            dbContext.Entry(user).State = EntityState.Detached;
            LogDuplicateRegistrationRace(logger);
            return AccountRegistration.EmailAlreadyExists();
        }

        if (result.Succeeded)
        {
            LogAccountRegistered(logger, user.Id);
            return AccountRegistration.Registered(user.Id, email);
        }

        return Map(result.Errors.Select(error => error.Code).ToList());
    }

    private static AccountRegistration Map(IReadOnlyCollection<string> errorCodes)
    {
        var describer = new IdentityErrorDescriber();

        if (errorCodes.Contains(describer.DuplicateEmail(string.Empty).Code) || errorCodes.Contains(describer.DuplicateUserName(string.Empty).Code))
        {
            return AccountRegistration.EmailAlreadyExists();
        }

        // InvalidUserName is a fallback: UserName = Email and the username character restriction is disabled.
        if (errorCodes.Contains(describer.InvalidEmail(string.Empty).Code) || errorCodes.Contains(describer.InvalidUserName(string.Empty).Code))
        {
            return AccountRegistration.InvalidEmail();
        }

        var passwordRequirements = new Dictionary<string, PasswordRequirement>(StringComparer.Ordinal)
        {
            [describer.PasswordTooShort(0).Code] = PasswordRequirement.MinimumLength,
            [describer.PasswordRequiresDigit().Code] = PasswordRequirement.Digit,
            [describer.PasswordRequiresLower().Code] = PasswordRequirement.Lowercase,
            [describer.PasswordRequiresUpper().Code] = PasswordRequirement.Uppercase,
            [describer.PasswordRequiresNonAlphanumeric().Code] = PasswordRequirement.NonAlphanumeric,
            [describer.PasswordRequiresUniqueChars(0).Code] = PasswordRequirement.UniqueCharacters,
        };

        var unmet = errorCodes.Where(passwordRequirements.ContainsKey).Select(code => passwordRequirements[code]).ToList();
        if (unmet.Count == errorCodes.Count && unmet.Count > 0)
        {
            return AccountRegistration.PasswordRejected(unmet);
        }

        // Unexpected Identity failure: surface only the error codes (never input values) as a server error.
        throw new InvalidOperationException($"Account registration failed with unexpected Identity errors: {string.Join(", ", errorCodes)}.");
    }

    private static bool IsDuplicateUser(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 } sqlException
        && UserUniqueIndexes.Any(index => sqlException.Message.Contains(index, StringComparison.Ordinal));

    [LoggerMessage(Level = LogLevel.Information, Message = "Account {UserId} registered")]
    private static partial void LogAccountRegistered(ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Concurrent registration with an existing email was rejected by the database")]
    private static partial void LogDuplicateRegistrationRace(ILogger logger);
}
