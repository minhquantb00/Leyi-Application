using Leyi.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Leyi.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Sets UTC <c>CreatedAt</c>/<c>UpdatedAt</c> for <see cref="IHasTimestamps"/> entries before they are saved.
/// </summary>
internal sealed class TimestampInterceptor : SaveChangesInterceptor
{
    private readonly TimeProvider timeProvider;

    public TimestampInterceptor(TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyTimestamps(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyTimestamps(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void ApplyTimestamps(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        ApplyShadowTimestamps(context, now);

        foreach (var entry in context.ChangeTracker.Entries<IHasTimestamps>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Property(entity => entity.CreatedAt).IsModified = false;
                    break;
            }
        }
    }

    /// <summary>
    /// Domain entities keep CreatedAt/UpdatedAt as shadow properties (they are technical audit data).
    /// Domain-owned timestamps (e.g. LearningEvidence.CreatedAt) are CLR properties and are left untouched.
    /// </summary>
    private static void ApplyShadowTimestamps(DbContext context, DateTime now)
    {
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            var createdAt = entry.Metadata.FindProperty(ConfigurationExtensions.CreatedAt);
            if (createdAt is not null && createdAt.IsShadowProperty())
            {
                var property = entry.Property(createdAt);
                if (entry.State == EntityState.Added)
                {
                    property.CurrentValue = now;
                }
                else
                {
                    property.IsModified = false;
                }
            }

            var updatedAt = entry.Metadata.FindProperty(ConfigurationExtensions.UpdatedAt);
            if (updatedAt is not null && updatedAt.IsShadowProperty())
            {
                entry.Property(updatedAt).CurrentValue = now;
            }
        }
    }
}
