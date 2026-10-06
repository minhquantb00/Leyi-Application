using System.Globalization;
using System.Text;

namespace Leyi.Application.Features.Identity.Login;

/// <summary>Token pair of a successful login. Token values are never written by <see cref="object.ToString"/>.</summary>
public sealed record LoginResult(string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken)
{
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("AccessToken = ***, AccessTokenExpiresAtUtc = ")
            .Append(AccessTokenExpiresAtUtc.ToString("O", CultureInfo.InvariantCulture))
            .Append(", RefreshToken = ***");
        return true;
    }
}
