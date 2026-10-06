using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Leyi.Infrastructure.Identity;

/// <summary>
/// Raw refresh token generation and hashing (I1): 256-bit from <see cref="RandomNumberGenerator"/>, base64url for
/// the client, SHA-256 lowercase hex for storage. The raw value is never persisted or logged.
/// </summary>
internal static class RefreshTokenSecret
{
    public const int SizeInBytes = 32;

    public static string Generate() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(SizeInBytes));

    public static string Hash(string rawToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(rawToken);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }
}
