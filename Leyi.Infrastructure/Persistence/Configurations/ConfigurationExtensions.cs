using System.Linq.Expressions;
using Leyi.Infrastructure.Identity;
using Leyi.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations;

/// <summary>Mapping conventions shared by the domain entity configurations.</summary>
internal static class ConfigurationExtensions
{
    public const string CreatedAt = "CreatedAt";
    public const string UpdatedAt = "UpdatedAt";
    public const string RowVersion = "RowVersion";

    /// <summary>
    /// Technical UTC audit columns kept out of the domain model as shadow properties, maintained by
    /// <see cref="Interceptors.TimestampInterceptor"/>.
    /// </summary>
    public static EntityTypeBuilder<TEntity> HasAuditTimestamps<TEntity>(this EntityTypeBuilder<TEntity> builder, bool createdAt = true, bool updatedAt = true)
        where TEntity : class
    {
        if (createdAt)
        {
            builder.Property<DateTime>(CreatedAt);
        }

        if (updatedAt)
        {
            builder.Property<DateTime>(UpdatedAt);
        }

        return builder;
    }

    /// <summary>SQL Server rowversion optimistic-concurrency token (C3), kept out of the domain model.</summary>
    public static EntityTypeBuilder<TEntity> HasRowVersion<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Property<byte[]>(RowVersion).IsRowVersion();
        return builder;
    }

    /// <summary>Required FK to the Identity user (<c>Users</c>).</summary>
    public static EntityTypeBuilder<TEntity> HasUser<TEntity>(this EntityTypeBuilder<TEntity> builder, Expression<Func<TEntity, object?>> foreignKey, DeleteBehavior deleteBehavior)
        where TEntity : class
    {
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(foreignKey)
            .IsRequired()
            .OnDelete(deleteBehavior);

        return builder;
    }

    public static PropertyBuilder<TEnum> HasStringEnum<TEnum>(this PropertyBuilder<TEnum> property, int maxLength)
        where TEnum : struct, Enum =>
        property.HasConversion<UpperSnakeCaseEnumConverter<TEnum>>().HasMaxLength(maxLength).IsUnicode(false);

    public static PropertyBuilder<TEnum?> HasStringEnum<TEnum>(this PropertyBuilder<TEnum?> property, int maxLength)
        where TEnum : struct, Enum =>
        property.HasConversion<UpperSnakeCaseEnumConverter<TEnum>>().HasMaxLength(maxLength).IsUnicode(false);

    /// <summary>ASCII code/identifier column (<c>varchar(n)</c>), for <see cref="string"/> properties.</summary>
    public static PropertyBuilder<TString> IsCode<TString>(this PropertyBuilder<TString> property, int maxLength) =>
        property.HasMaxLength(maxLength).IsUnicode(false);
}
