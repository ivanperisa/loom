using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class LearningAgreementEntryConfiguration : IEntityTypeConfiguration<LearningAgreementEntry>
{
    public void Configure(EntityTypeBuilder<LearningAgreementEntry> builder)
    {
        builder.ToTable("learning_agreement_entry");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.LearningAgreementId).HasColumnName("learning_agreement_id");
        builder.Property(x => x.CourseSlotId).HasColumnName("course_slot_id");
        builder.Property(x => x.Mode).HasColumnName("mode").HasConversion<string>();
        builder.Property(x => x.ForeignCourseId).HasColumnName("foreign_course_id");
        builder.Property(x => x.AwardedEcts).HasColumnName("awarded_ects").HasPrecision(4, 1);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.HasOne(x => x.LearningAgreement)
            .WithMany(x => x.Entries)
            .HasForeignKey(x => x.LearningAgreementId);
        builder.HasOne(x => x.CourseSlot)
            .WithMany()
            .HasForeignKey(x => x.CourseSlotId);
        builder.HasOne(x => x.ForeignCourse)
            .WithMany()
            .HasForeignKey(x => x.ForeignCourseId)
            .IsRequired(false);
        builder.HasIndex(x => x.LearningAgreementId);
        builder.HasIndex(x => x.CourseSlotId);
    }
}
