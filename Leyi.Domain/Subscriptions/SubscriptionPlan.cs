using Leyi.Domain.Common;

namespace Leyi.Domain.Subscriptions;

/// <summary>
/// Subscription plan reference data (ERD §6.24), e.g. BASIC / PLUS / PRO. Prices are reference values only
/// (the billing provider is the source of truth). Subscription never changes how mastery is calculated.
/// </summary>
public sealed class SubscriptionPlan : Entity
{
    private SubscriptionPlan()
    {
    }

    private SubscriptionPlan(Guid id)
        : base(id)
    {
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public decimal? MonthlyPrice { get; private set; }

    public decimal? YearlyPrice { get; private set; }

    public SubscriptionPlanStatus Status { get; private set; }

    public static SubscriptionPlan Create(string code, string name, decimal? monthlyPrice = null, decimal? yearlyPrice = null)
    {
        if (monthlyPrice is < 0m || yearlyPrice is < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(monthlyPrice), "Prices must not be negative.");
        }

        return new SubscriptionPlan(NewId())
        {
            Code = Guard.NotBlank(code),
            Name = Guard.NotBlank(name),
            MonthlyPrice = monthlyPrice,
            YearlyPrice = yearlyPrice,
            Status = SubscriptionPlanStatus.Active,
        };
    }

    public void Deactivate() => Status = SubscriptionPlanStatus.Inactive;
}
