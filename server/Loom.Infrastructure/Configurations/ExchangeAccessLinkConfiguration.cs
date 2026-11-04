using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class ExchangeAccessLinkConfiguration : IEntityTypeConfiguration<ExchangeAccessLink>
{
    public void Configure(EntityTypeBuilder<ExchangeAccessLink> builder)
    {
        builder.ToTable("access_link", "exchange");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.ExchangeId).HasColumnName("exchange_id");
        builder.Property(x => x.Token).HasColumnName("token").HasMaxLength(64);
        builder.Property(x => x.CreatedById).HasColumnName("created_by_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at");
        builder.Ignore(x => x.IsActive);

        builder.HasOne(x => x.Exchange)
            .WithMany(x => x.AccessLinks)
            .HasForeignKey(x => x.ExchangeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.Token).IsUnique();
        // At most one live link per exchange.
        builder.HasIndex(x => x.ExchangeId).IsUnique().HasFilter("revoked_at IS NULL").HasDatabaseName("idx_access_link_exchange_active");
    }
}
