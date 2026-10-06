namespace Leyi.Domain.Subscriptions;

/// <summary>Subscription plan status (ERD §6.24).</summary>
public enum SubscriptionPlanStatus
{
    Active,
    Inactive,
}

/// <summary>User subscription status (ERD §6.25).</summary>
public enum SubscriptionStatus
{
    Active,
    Expired,
    Cancelled,
    GracePeriod,
}

/// <summary>Billing provider (ERD §6.25). The provider is the source of truth for billing state.</summary>
public enum SubscriptionProvider
{
    Apple,
    Google,
    Web,
}
