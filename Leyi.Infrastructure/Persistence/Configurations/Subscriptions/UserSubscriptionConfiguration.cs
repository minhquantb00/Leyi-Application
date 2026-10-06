using Leyi.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Subscriptions;

/// <summary>ERD §6.25 UserSubscriptions (history; RESTRICT from Users and plans).</summary>
internal sealed class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        builder.ToTable("UserSubscriptions");
        builder.HasKey(subscription => subscription.Id);
        builder.Property(subscription => subscription.Id).ValueGeneratedNever();

        builder.Property(subscription => subscription.Status).HasStringEnum(20);
        builder.Property(subscription => subscription.Provider).HasStringEnum(20);
        builder.Property(subscription => subscription.ProviderSubscriptionId).HasMaxLength(300);
        builder.HasAuditTimestamps();

        builder.HasUser(subscription => subscription.UserId, DeleteBehavior.Restrict);

        builder.HasOne<SubscriptionPlan>()
            .WithMany()
            .HasForeignKey(subscription => subscription.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
