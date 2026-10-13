namespace Leyi.Domain.Policies;

/// <summary>ReviewDebt level (ASE §23) by <see cref="SessionPolicy.ReviewDebt"/> thresholds.</summary>
public enum ReviewDebtLevel
{
    Low,
    Medium,
    High,
    Critical,
}
