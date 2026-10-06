using System.Text;
using Leyi.Application.Abstractions.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Leyi.Infrastructure.Identity;

/// <summary>
/// Signs HS256 access tokens. Claims are kept minimal: <c>sub</c> (user id) and <c>jti</c>, plus the registered
/// <c>iss</c>, <c>aud</c>, <c>iat</c>, <c>nbf</c> and <c>exp</c>. No email, profile, role or subscription data.
/// </summary>
internal sealed class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly JsonWebTokenHandler tokenHandler = new() { SetDefaultTimesOnTokenCreation = false };
    private readonly IOptions<JwtOptions> jwtOptions;
    private readonly IOptions<AuthenticationTokenOptions> tokenOptions;

    public JwtAccessTokenIssuer(IOptions<JwtOptions> jwtOptions, IOptions<AuthenticationTokenOptions> tokenOptions)
    {
        this.jwtOptions = jwtOptions;
        this.tokenOptions = tokenOptions;
    }

    public AccessToken Issue(Guid userId, DateTime issuedAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        if (issuedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamps must be UTC.", nameof(issuedAtUtc));
        }

        var jwt = jwtOptions.Value;

        // JWT time claims have one-second precision; the returned expiry must equal the "exp" claim.
        var issuedAt = new DateTime(issuedAtUtc.Ticks - (issuedAtUtc.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
        var expiresAt = issuedAt + tokenOptions.Value.AccessTokenLifetime;

        var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = expiresAt,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = userId.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        });

        return new AccessToken(token, expiresAt);
    }
}
