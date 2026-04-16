using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class CourseSlotConfiguration : IEntityTypeConfiguration<CourseSlot>
{
    public void Configure(EntityTypeBuilder<CourseSlot> builder)
    {
        builder.ToTable("course_slot");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.StudyProfileId).HasColumnName("study_profile_id");
        builder.Property(x => x.Semester).HasColumnName("semester");
        builder.Property(x => x.ColStart).HasColumnName("col_start");
        builder.Property(x => x.Ects).HasColumnName("ects");
        builder.Property(x => x.Category).HasColumnName("category").HasConversion<string>();
        builder.Property(x => x.CourseCode).HasColumnName("course_code");
        builder.Property(x => x.CourseName).HasColumnName("course_name").IsRequired();
        builder.Property(x => x.CourseNameEn).HasColumnName("course_name_en");
        builder.Property(x => x.Color).HasColumnName("color").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.HasOne(x => x.StudyProfile)
            .WithMany(x => x.CourseSlots)
            .HasForeignKey(x => x.StudyProfileId);
        builder.HasIndex(x => x.StudyProfileId);
    }
}
