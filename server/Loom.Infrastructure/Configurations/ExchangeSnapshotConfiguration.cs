using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class ExchangeSnapshotConfiguration : IEntityTypeConfiguration<ExchangeSnapshot>
{
    public void Configure(EntityTypeBuilder<ExchangeSnapshot> builder)
    {
        builder.ToTable("snapshot", "exchange");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.ExchangeId).HasColumnName("exchange_id");
        builder.Property(x => x.ChangedById).HasColumnName("changed_by_id");
        builder.Property(x => x.Phase).HasColumnName("phase").HasConversion<string>();
        builder.Property(x => x.Snapshot).HasColumnName("snapshot").HasColumnType("jsonb");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasOne(x => x.Exchange)
            .WithMany(x => x.Snapshots)
            .HasForeignKey(x => x.ExchangeId);

        builder.HasOne(x => x.ChangedBy)
            .WithMany()
            .HasForeignKey(x => x.ChangedById);

        builder.HasIndex(x => x.ExchangeId);
        builder.HasIndex(x => new { x.ExchangeId, x.CreatedAt });
        builder.HasIndex(x => x.CreatedAt);
    }
}
