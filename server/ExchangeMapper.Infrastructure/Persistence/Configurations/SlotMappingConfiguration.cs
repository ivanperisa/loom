using ExchangeMapper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExchangeMapper.Infrastructure.Persistence.Configurations;

public class SlotMappingConfiguration : IEntityTypeConfiguration<SlotMapping>
{
    public void Configure(EntityTypeBuilder<SlotMapping> builder)
    {
        builder.ToTable("slot_mapping");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SlotStateId).HasColumnName("slot_state_id");
        builder.Property(x => x.ForeignCourseId).HasColumnName("foreign_course_id");
        builder.Property(x => x.AwardedEcts).HasColumnName("awarded_ects").HasPrecision(4, 1);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.HasOne(x => x.SlotState)
            .WithMany(x => x.SlotMappings)
            .HasForeignKey(x => x.SlotStateId);
        builder.HasOne(x => x.ForeignCourse)
            .WithMany()
            .HasForeignKey(x => x.ForeignCourseId);
        builder.HasIndex(x => x.SlotStateId);
        builder.HasIndex(x => x.ForeignCourseId);
    }
}
