using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class ExchangeSnapshotConfiguration : IEntityTypeConfiguration<ExchangeSnapshot>
{
    public void Configure(EntityTypeBuilder<ExchangeSnapshot> builder)
    {
        builder.ToTable("snapshot", "exchange", t =>
        {
            t.HasCheckConstraint("snapshot_phase_check", "phase IN ('LearningAgreement', 'Recognition')");
            t.HasCheckConstraint("snapshot_type_check", "type IN ('Auto', 'PreImport')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.ExchangeId).HasColumnName("exchange_id");
        builder.Property(x => x.ChangedById).HasColumnName("changed_by_id");
        builder.Property(x => x.Phase).HasColumnName("phase").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20).HasDefaultValue(SnapshotType.Auto);
        builder.Property(x => x.Snapshot).HasColumnName("snapshot").HasColumnType("jsonb");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

        builder.HasOne(x => x.Exchange)
            .WithMany(x => x.Snapshots)
            .HasForeignKey(x => x.ExchangeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ChangedBy)
            .WithMany()
            .HasForeignKey(x => x.ChangedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.ExchangeId, x.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_snapshot_exchange_created");
        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("idx_snapshot_created");
    }
}
