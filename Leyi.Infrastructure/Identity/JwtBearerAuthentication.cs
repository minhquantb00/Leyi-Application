using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Leyi.Infrastructure.Identity;

/// <summary>
/// Validation of the access tokens issued by <see cref="JwtAccessTokenIssuer"/>, with the same <see cref="JwtOptions"/>.
/// Only the API host authenticates requests; the Worker does not call this.
/// </summary>
public static class JwtBearerAuthentication
{
    /// <summary>Access tokens must carry the user id as a GUID <c>sub</c> claim.</summary>
    public const string UserIdClaimType = JwtRegisteredClaimNames.Sub;

    /// <summary>
    /// Signature (HS256 only), issuer, audience and lifetime are validated with no clock skew, so a token is rejected
    /// exactly at its <c>exp</c> (= the <c>expiresAtUtc</c> returned to the client). Inbound claims are not mapped,
    /// so <c>sub</c> stays <c>sub</c>. A token without a GUID <c>sub</c> fails authentication.
    /// </summary>
    public static IServiceCollection AddJwtBearerAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                bearer.MapInboundClaims = false;

                // Explicit (the framework default is true): WWW-Authenticate must not tell a client why its token
                // was rejected (signature, issuer, audience, expiry...).
                bearer.IncludeErrorDetails = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidateIssuerSigningKey = true,
                    RequireSignedTokens = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = UserIdClaimType,
                };
                bearer.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (!HasUserId(context.Principal?.FindFirst(UserIdClaimType)?.Value))
                        {
                            context.Fail("The access token has no valid subject.");
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        return services;
    }

    private static bool HasUserId(string? subject) => Guid.TryParse(subject, out var userId) && userId != Guid.Empty;
}
