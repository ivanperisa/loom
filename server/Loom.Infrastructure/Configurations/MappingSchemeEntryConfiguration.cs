using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class MappingSchemeEntryConfiguration : IEntityTypeConfiguration<MappingSchemeEntry>
{
    public void Configure(EntityTypeBuilder<MappingSchemeEntry> builder)
    {
        builder.ToTable("mapping_scheme_entry", "exchange", t =>
            t.HasCheckConstraint("mapping_scheme_entry_enrollment_status_check", "enrollment_status IN ('Passed', 'NotPassed')"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.ExchangeId).HasColumnName("exchange_id");
        builder.Property(x => x.HomeSlotId).HasColumnName("home_slot_id");
        builder.Property(x => x.PartnerCourseId).HasColumnName("partner_course_id");
        builder.Property(x => x.AwardedEcts).HasColumnName("awarded_ects").HasPrecision(4, 1);
        builder.Property(x => x.EnrollmentStatus).HasColumnName("enrollment_status").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.OriginalGrade).HasColumnName("original_grade").HasMaxLength(20);
        builder.Property(x => x.EctsGrade).HasColumnName("ects_grade").HasMaxLength(5);
        builder.Property(x => x.HrGrade).HasColumnName("hr_grade").HasMaxLength(10);
        builder.Property(x => x.ExamDate).HasColumnName("exam_date");
        builder.Property(x => x.IsRecognized).HasColumnName("is_recognized");
        builder.Property(x => x.RecognizedAsCourseId).HasColumnName("recognized_as_course_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasOne(x => x.Exchange)
            .WithMany()
            .HasForeignKey(x => x.ExchangeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.HomeSlot)
            .WithMany()
            .HasForeignKey(x => x.HomeSlotId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.PartnerCourse)
            .WithMany()
            .HasForeignKey(x => x.PartnerCourseId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.RecognizedAsCourse)
            .WithMany()
            .HasForeignKey(x => x.RecognizedAsCourseId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.ExchangeId).HasDatabaseName("idx_mapping_scheme_entry_exchange");
        builder.HasIndex(x => x.HomeSlotId).HasDatabaseName("idx_mapping_scheme_entry_slot");
    }
}
