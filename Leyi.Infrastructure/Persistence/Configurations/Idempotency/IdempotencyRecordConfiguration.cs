using Leyi.Infrastructure.Idempotency;
using Leyi.Infrastructure.Identity;
using Leyi.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Idempotency;

/// <summary>
/// ERD §6.28 IdempotencyRecords (baseline).
/// </summary>
internal sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");

        builder.HasKey(record => record.Id);

        builder.Property(record => record.Endpoint)
            .HasMaxLength(100)
            .IsUnicode(false);

        builder.Property(record => record.IdempotencyKey)
            .HasMaxLength(100)
            .IsUnicode(false);

        builder.Property(record => record.RequestHash)
            .HasMaxLength(128)
            .IsUnicode(false);

        builder.Property(record => record.Status)
            .HasConversion<UpperSnakeCaseEnumConverter<IdempotencyRecordStatus>>()
            .HasMaxLength(20)
            .IsUnicode(false);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(record => record.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(record => new { record.UserId, record.Endpoint, record.IdempotencyKey })
            .HasDatabaseName("UX_IdempotencyRecords_User_Endpoint_Key")
            .IsUnique();

        builder.HasIndex(record => record.ExpiresAt)
            .HasDatabaseName("IX_IdempotencyRecords_ExpiresAt");
    }
}
