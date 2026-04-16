using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class RecognitionEntryConfiguration : IEntityTypeConfiguration<RecognitionEntry>
{
    public void Configure(EntityTypeBuilder<RecognitionEntry> builder)
    {
        builder.ToTable("recognition_entry");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.RecognitionId).HasColumnName("recognition_id");
        builder.Property(x => x.SlotMappingId).HasColumnName("slot_mapping_id");
        builder.Property(x => x.EnrollmentStatus).HasColumnName("enrollment_status");
        builder.Property(x => x.OriginalGrade).HasColumnName("original_grade");
        builder.Property(x => x.EctsGrade).HasColumnName("ects_grade");
        builder.Property(x => x.HrGrade).HasColumnName("hr_grade");
        builder.Property(x => x.ExamDate).HasColumnName("exam_date");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.HasOne(x => x.Recognition)
            .WithMany(x => x.Entries)
            .HasForeignKey(x => x.RecognitionId);
        builder.HasOne(x => x.SlotMapping)
            .WithOne(x => x.RecognitionEntry)
            .HasForeignKey<RecognitionEntry>(x => x.SlotMappingId);
        builder.HasIndex(x => x.RecognitionId);
        builder.HasIndex(x => x.SlotMappingId).IsUnique();
    }
}
