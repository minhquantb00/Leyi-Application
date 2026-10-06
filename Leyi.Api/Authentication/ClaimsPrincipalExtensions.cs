using System.Security.Claims;
using Leyi.Infrastructure.Identity;

namespace Leyi.Api.Authentication;

internal static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The authenticated user id from the <c>sub</c> claim. Bearer authentication already rejects tokens without a GUID
    /// subject, so a missing value here means the endpoint is not protected by RequireAuthorization (a programming error).
    /// </summary>
    public static Guid GetRequiredUserId(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return Guid.TryParse(principal.FindFirst(JwtBearerAuthentication.UserIdClaimType)?.Value, out var userId) && userId != Guid.Empty
            ? userId
            : throw new InvalidOperationException("The request has no authenticated user id.");
    }
}
