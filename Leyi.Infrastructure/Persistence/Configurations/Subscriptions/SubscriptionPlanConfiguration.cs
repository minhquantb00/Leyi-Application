using Leyi.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Subscriptions;

/// <summary>ERD §6.24 SubscriptionPlans.</summary>
internal sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans");
        builder.HasKey(plan => plan.Id);
        builder.Property(plan => plan.Id).ValueGeneratedNever();

        builder.Property(plan => plan.Code).IsCode(20);
        builder.Property(plan => plan.Name).HasMaxLength(100);
        builder.Property(plan => plan.MonthlyPrice).HasPrecision(18, 2);
        builder.Property(plan => plan.YearlyPrice).HasPrecision(18, 2);
        builder.Property(plan => plan.Status).HasStringEnum(16);
        builder.HasAuditTimestamps();

        builder.HasIndex(plan => plan.Code).HasDatabaseName("UX_SubscriptionPlans_Code").IsUnique();
    }
}
