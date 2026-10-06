using System.Text;

namespace Leyi.Infrastructure.Identity;

/// <summary>
/// JWT signing configuration, bound to the <c>Jwt</c> section. The signing key is a secret: it comes from
/// user-secrets (Development) or environment variables, never from committed configuration.
/// Token lifetimes are not here; they belong to <see cref="AuthenticationTokenOptions"/>.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 needs a key of at least 256 bits.</summary>
    public const int MinimumSigningKeyBytes = 32;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Returns the validation failures (empty when the options are usable). Never includes the key value.</summary>
    public IReadOnlyList<string> Validate()
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(Issuer))
        {
            failures.Add($"{SectionName}:{nameof(Issuer)} is required.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            failures.Add($"{SectionName}:{nameof(Audience)} is required.");
        }

        if (Encoding.UTF8.GetByteCount(SigningKey ?? string.Empty) < MinimumSigningKeyBytes)
        {
            failures.Add($"{SectionName}:{nameof(SigningKey)} must be configured (user-secrets or environment variable) with at least {MinimumSigningKeyBytes} bytes.");
        }

        return failures;
    }
}
