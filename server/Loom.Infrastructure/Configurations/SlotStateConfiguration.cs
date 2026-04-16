using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class SlotStateConfiguration : IEntityTypeConfiguration<SlotState>
{
    public void Configure(EntityTypeBuilder<SlotState> builder)
    {
        builder.ToTable("slot_state");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ExchangeId).HasColumnName("exchange_id");
        builder.Property(x => x.CourseSlotId).HasColumnName("course_slot_id");
        builder.Property(x => x.Mode).HasColumnName("mode").HasConversion<string>();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.HasOne(x => x.Exchange)
            .WithMany(x => x.SlotStates)
            .HasForeignKey(x => x.ExchangeId);
        builder.HasOne(x => x.CourseSlot)
            .WithMany(x => x.SlotStates)
            .HasForeignKey(x => x.CourseSlotId);
        builder.HasIndex(x => x.ExchangeId);
        builder.HasIndex(x => x.CourseSlotId);
        builder.HasIndex(x => new { x.ExchangeId, x.CourseSlotId }).IsUnique();
    }
}
