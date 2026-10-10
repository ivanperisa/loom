using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class RecognitionEntryConfiguration : IEntityTypeConfiguration<RecognitionEntry>
{
    public void Configure(EntityTypeBuilder<RecognitionEntry> builder)
    {
        builder.ToTable("recognition_entry", "exchange", t =>
            t.HasCheckConstraint("recognition_entry_enrollment_status_check", "enrollment_status IN ('Passed', 'NotPassed')"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.RecognitionId).HasColumnName("recognition_id");
        builder.Property(x => x.PartnerCourseId).HasColumnName("partner_course_id");
        builder.Property(x => x.EnrollmentStatus).HasColumnName("enrollment_status").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.OriginalGrade).HasColumnName("original_grade").HasMaxLength(20);
        builder.Property(x => x.EctsGrade).HasColumnName("ects_grade").HasMaxLength(5);
        builder.Property(x => x.HrGrade).HasColumnName("hr_grade").HasMaxLength(10);
        builder.Property(x => x.ExamDate).HasColumnName("exam_date");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasOne(x => x.Recognition)
            .WithMany(x => x.Entries)
            .HasForeignKey(x => x.RecognitionId)
            .OnDelete(DeleteBehavior.Cascade);

        // A course with results is never hard-deleted (see PartnerCourseService.DeleteAsync); merging moves the rows.
        builder.HasOne(x => x.PartnerCourse)
            .WithMany()
            .HasForeignKey(x => x.PartnerCourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.RecognitionId, x.PartnerCourseId }).IsUnique().HasDatabaseName("recognition_entry_course_key");
    }
}
