namespace Leyi.Infrastructure.Configuration;

/// <summary>
/// Bound to the <c>ConnectionStrings</c> section. Values contain credentials:
/// supply them through user-secrets or environment variables, never commit them, never log them.
/// </summary>
public sealed class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    public string SqlServer { get; set; } = string.Empty;

    public string Redis { get; set; } = string.Empty;
}
