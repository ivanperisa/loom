using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class RecognitionEntryConfiguration : IEntityTypeConfiguration<RecognitionEntry>
{
    public void Configure(EntityTypeBuilder<RecognitionEntry> builder)
    {
        builder.ToTable("recognition_entry", "exchange");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.RecognitionId).HasColumnName("recognition_id");
        builder.Property(x => x.LearningAgreementEntryId).HasColumnName("learning_agreement_entry_id");
        builder.Property(x => x.RecognizedAsCourseId).HasColumnName("recognized_as_course_id");
        builder.Property(x => x.EnrollmentStatus).HasColumnName("enrollment_status");
        builder.Property(x => x.OriginalGrade).HasColumnName("original_grade");
        builder.Property(x => x.EctsGrade).HasColumnName("ects_grade");
        builder.Property(x => x.HrGrade).HasColumnName("hr_grade");
        builder.Property(x => x.ExamDate).HasColumnName("exam_date");
        builder.Property(x => x.IsRecognized).HasColumnName("is_recognized");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasOne(x => x.Recognition)
            .WithMany(x => x.Entries)
            .HasForeignKey(x => x.RecognitionId);

        builder.HasOne(x => x.LearningAgreementEntry)
            .WithOne(x => x.RecognitionEntry)
            .HasForeignKey<RecognitionEntry>(x => x.LearningAgreementEntryId);

        builder.HasOne(x => x.RecognizedAsCourse)
            .WithMany()
            .HasForeignKey(x => x.RecognizedAsCourseId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.RecognitionId);
        builder.HasIndex(x => x.LearningAgreementEntryId).IsUnique();
    }
}
