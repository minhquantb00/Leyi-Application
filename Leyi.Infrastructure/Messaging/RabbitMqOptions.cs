namespace Leyi.Infrastructure.Messaging;

/// <summary>
/// Bound to the <c>RabbitMq</c> section. <see cref="Password"/> must come from user-secrets or environment variables.
/// </summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = string.Empty;

    public ushort Port { get; set; } = 5672;

    public string VirtualHost { get; set; } = "/";

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>INFRA-02 default: immediate retries after 1s, 5s and 15s, then the <c>&lt;endpoint&gt;_error</c> queue.</summary>
    public static IReadOnlyList<int> DefaultRetryIntervalsSeconds { get; } = [1, 5, 15];

    /// <summary>
    /// Optional override of the bounded immediate retry intervals. Null means <see cref="DefaultRetryIntervalsSeconds"/>.
    /// (Kept null by default because the configuration binder appends configured elements to a pre-filled array.)
    /// After the last retry the message is moved to the MassTransit <c>&lt;endpoint&gt;_error</c> queue (dead letter).
    /// V1 uses no delayed redelivery.
    /// </summary>
    public int[]? RetryIntervalsSeconds { get; set; }

    public IReadOnlyList<int> EffectiveRetryIntervalsSeconds => RetryIntervalsSeconds ?? DefaultRetryIntervalsSeconds;
}
