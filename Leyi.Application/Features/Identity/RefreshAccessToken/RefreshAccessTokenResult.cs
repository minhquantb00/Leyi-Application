using System.Globalization;
using System.Text;

namespace Leyi.Application.Features.Identity.RefreshAccessToken;

/// <summary>New token pair of a successful refresh. Token values are never written by <see cref="object.ToString"/>.</summary>
public sealed record RefreshAccessTokenResult(string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken)
{
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("AccessToken = ***, AccessTokenExpiresAtUtc = ")
            .Append(AccessTokenExpiresAtUtc.ToString("O", CultureInfo.InvariantCulture))
            .Append(", RefreshToken = ***");
        return true;
    }
}
