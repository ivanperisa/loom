using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class HomeSlotTypeConfiguration : IEntityTypeConfiguration<HomeSlotType>
{
    public void Configure(EntityTypeBuilder<HomeSlotType> builder)
    {
        builder.ToTable("slot_type", "home");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.NameEn).HasColumnName("name_en").HasMaxLength(100);
        builder.Property(x => x.Color).HasColumnName("color").HasMaxLength(7).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
    }
}
