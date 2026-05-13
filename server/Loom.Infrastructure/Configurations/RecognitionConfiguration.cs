using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class RecognitionConfiguration : IEntityTypeConfiguration<Recognition>
{
    public void Configure(EntityTypeBuilder<Recognition> builder)
    {
        builder.ToTable("recognition", "exchange");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.ExchangeId).HasColumnName("exchange_id");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasOne(x => x.Exchange)
            .WithOne(x => x.Recognition)
            .HasForeignKey<Recognition>(x => x.ExchangeId);

        builder.HasIndex(x => x.ExchangeId).IsUnique();
    }
}
