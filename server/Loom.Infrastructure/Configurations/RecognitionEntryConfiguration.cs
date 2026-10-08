using Loom.Domain.Entities;
using Loom.Domain.Enums;
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
        builder.Property(x => x.EnrollmentStatus).HasColumnName("enrollment_status").HasMaxLength(50);
        builder.Property(x => x.OriginalGrade).HasColumnName("original_grade").HasMaxLength(20);
        builder.Property(x => x.EctsGrade).HasColumnName("ects_grade").HasMaxLength(5);
        builder.Property(x => x.HrGrade).HasColumnName("hr_grade").HasMaxLength(10);
        builder.Property(x => x.ExamDate).HasColumnName("exam_date");
        builder.Property(x => x.IsRecognized).HasColumnName("is_recognized");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

        builder.HasOne(x => x.Recognition)
            .WithMany(x => x.Entries)
            .HasForeignKey(x => x.RecognitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.LearningAgreementEntry)
            .WithOne(x => x.RecognitionEntry)
            .HasForeignKey<RecognitionEntry>(x => x.LearningAgreementEntryId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.RecognizedAsCourse)
            .WithMany()
            .HasForeignKey(x => x.RecognizedAsCourseId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.RecognitionId).HasDatabaseName("idx_recognition_entry_recognition");
        builder.HasIndex(x => x.LearningAgreementEntryId).IsUnique().HasDatabaseName("recognition_entry_learning_agreement_entry_id_key");
    }
}
