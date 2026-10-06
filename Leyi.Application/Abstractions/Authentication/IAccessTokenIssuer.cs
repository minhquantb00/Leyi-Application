using System.Globalization;
using System.Text;

namespace Leyi.Application.Abstractions.Authentication;

/// <summary>Issues short-lived access tokens (JWT) for an authenticated user. Lifetime comes from the token policy options.</summary>
public interface IAccessTokenIssuer
{
    AccessToken Issue(Guid userId, DateTime issuedAtUtc);
}

/// <summary>A signed access token. The token value is never written by <see cref="object.ToString"/>.</summary>
public sealed record AccessToken(string Value, DateTime ExpiresAtUtc)
{
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Value = ***, ExpiresAtUtc = ").Append(ExpiresAtUtc.ToString("O", CultureInfo.InvariantCulture));
        return true;
    }
}
