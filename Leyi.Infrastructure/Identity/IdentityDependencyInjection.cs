using Leyi.Application.Abstractions.Authentication;
using Leyi.Application.Features.Identity.Login;
using Leyi.Application.Features.Identity.Logout;
using Leyi.Application.Features.Identity.RefreshAccessToken;
using Leyi.Application.Features.Identity.RegisterAccount;
using Leyi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Leyi.Infrastructure.Identity;

internal static class IdentityDependencyInjection
{
    /// <summary>
    /// Identity (user/role stores on <see cref="LeyiDbContext"/>), token policy/JWT options and the adapters of the
    /// Register, Login, RefreshAccessToken and Logout features. Lockout follows the explicit V1 policy in <see cref="IdentitySecurityDefaults"/> (Login.md §4).
    /// No email confirmation or external login is implemented here.
    /// </summary>
    public static IServiceCollection AddIdentityFoundation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // V1: UserName = Email. The email is already validated (FluentValidation + Identity email validation),
                // so the default username character whitelist would only reject valid addresses such as o'brien@….
                options.User.AllowedUserNameCharacters = string.Empty;

                // I3: email confirmation is not required in V1; EmailConfirmed stays false and is not checked at login.
                options.SignIn.RequireConfirmedEmail = false;

                // Leyi V1 lockout policy, explicit rather than the framework defaults.
                options.Lockout.AllowedForNewUsers = IdentitySecurityDefaults.LockoutAllowedForNewUsers;
                options.Lockout.MaxFailedAccessAttempts = IdentitySecurityDefaults.MaxFailedAccessAttempts;
                options.Lockout.DefaultLockoutTimeSpan = IdentitySecurityDefaults.LockoutDuration;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<LeyiDbContext>();

        services.AddOptions<AuthenticationTokenOptions>()
            .Bind(configuration.GetSection(AuthenticationTokenOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AuthenticationTokenOptions>, AuthenticationTokenOptionsValidator>();

        // Validated when first used. Only the API issues tokens, so the API host adds ValidateOnStart;
        // the Worker does not need a signing key.
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();

        services.AddScoped<IAccountRegistrar, IdentityAccountRegistrar>();
        services.AddScoped<ICredentialVerifier, IdentityCredentialVerifier>();
        services.AddScoped<ILoginSessionStore, RefreshTokenLoginSessionStore>();
        services.AddScoped<IRefreshTokenRotator, RefreshTokenRotator>();
        services.AddScoped<IRefreshTokenRevoker, RefreshTokenRevoker>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

        return services;
    }

    private sealed class AuthenticationTokenOptionsValidator : IValidateOptions<AuthenticationTokenOptions>
    {
        public ValidateOptionsResult Validate(string? name, AuthenticationTokenOptions options)
        {
            var failures = options.Validate();
            return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
        }
    }

    private sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
    {
        public ValidateOptionsResult Validate(string? name, JwtOptions options)
        {
            var failures = options.Validate();
            return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
        }
    }
}
