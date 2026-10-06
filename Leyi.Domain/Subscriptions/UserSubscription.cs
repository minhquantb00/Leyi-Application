using Leyi.Domain.Common;

namespace Leyi.Domain.Subscriptions;

/// <summary>
/// A user's subscription history entry (ERD §6.25). Changed only by a verified billing workflow (not in V1 scope);
/// clients never set a plan themselves.
/// </summary>
public sealed class UserSubscription : Entity
{
    private UserSubscription()
    {
    }

    private UserSubscription(Guid id)
        : base(id)
    {
    }

    public Guid UserId { get; private set; }

    public Guid SubscriptionPlanId { get; private set; }

    public SubscriptionStatus Status { get; private set; }

    public SubscriptionProvider Provider { get; private set; }

    public string? ProviderSubscriptionId { get; private set; }

    public DateTime StartedAt { get; private set; }

    public DateTime? ExpiresAt { get; private set; }

    public bool AutoRenew { get; private set; }

    public static UserSubscription Start(
        Guid userId,
        Guid subscriptionPlanId,
        SubscriptionProvider provider,
        DateTime startedAtUtc,
        DateTime? expiresAtUtc,
        bool autoRenew,
        string? providerSubscriptionId = null)
    {
        Guard.Utc(startedAtUtc);
        if (Guard.Utc(expiresAtUtc) <= startedAtUtc)
        {
            throw new ArgumentException("A subscription must expire after it starts.", nameof(expiresAtUtc));
        }

        return new UserSubscription(NewId())
        {
            UserId = Guard.NotEmpty(userId),
            SubscriptionPlanId = Guard.NotEmpty(subscriptionPlanId),
            Provider = Guard.Defined(provider),
            Status = SubscriptionStatus.Active,
            StartedAt = startedAtUtc,
            ExpiresAt = expiresAtUtc,
            AutoRenew = autoRenew,
            ProviderSubscriptionId = Guard.NullIfBlank(providerSubscriptionId),
        };
    }
}
