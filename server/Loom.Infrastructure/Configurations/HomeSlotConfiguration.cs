using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class HomeSlotConfiguration : IEntityTypeConfiguration<HomeSlot>
{
    public void Configure(EntityTypeBuilder<HomeSlot> builder)
    {
        builder.ToTable("slot", "home", tb => tb.HasCheckConstraint(
            "chk_slot_exactly_one_source",
            "(course_id IS NOT NULL AND course_group_id IS NULL) OR (course_id IS NULL AND course_group_id IS NOT NULL)"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.ProfileId).HasColumnName("profile_id").IsRequired();
        builder.Property(x => x.Semester).HasColumnName("semester");
        builder.Property(x => x.SlotPosition).HasColumnName("slot_position");
        builder.Property(x => x.Ects).HasColumnName("ects");
        builder.Property(x => x.SlotTypeId).HasColumnName("slot_type_id").IsRequired();
        builder.Property(x => x.CourseId).HasColumnName("course_id");
        builder.Property(x => x.CourseGroupId).HasColumnName("course_group_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasOne(x => x.Profile)
            .WithMany(x => x.Slots)
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SlotType)
            .WithMany(x => x.Slots)
            .HasForeignKey(x => x.SlotTypeId);

        builder.HasOne(x => x.Course)
            .WithMany(x => x.Slots)
            .HasForeignKey(x => x.CourseId)
            .IsRequired(false);

        builder.HasOne(x => x.CourseGroup)
            .WithMany(x => x.Slots)
            .HasForeignKey(x => x.CourseGroupId)
            .IsRequired(false);

        builder.HasIndex(x => x.ProfileId);
    }
}
