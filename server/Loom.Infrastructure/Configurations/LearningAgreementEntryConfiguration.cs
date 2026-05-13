using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class LearningAgreementEntryConfiguration : IEntityTypeConfiguration<LearningAgreementEntry>
{
    public void Configure(EntityTypeBuilder<LearningAgreementEntry> builder)
    {
        builder.ToTable("learning_agreement_entry", "exchange");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.LearningAgreementId).HasColumnName("learning_agreement_id");
        builder.Property(x => x.HomeSlotId).HasColumnName("home_slot_id");
        builder.Property(x => x.Mode).HasColumnName("mode").HasConversion<string>();
        builder.Property(x => x.PartnerCourseId).HasColumnName("partner_course_id");
        builder.Property(x => x.AwardedEcts).HasColumnName("awarded_ects").HasPrecision(4, 1);
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasOne(x => x.LearningAgreement)
            .WithMany(x => x.Entries)
            .HasForeignKey(x => x.LearningAgreementId);

        builder.HasOne(x => x.HomeSlot)
            .WithMany()
            .HasForeignKey(x => x.HomeSlotId);

        builder.HasOne(x => x.PartnerCourse)
            .WithMany()
            .HasForeignKey(x => x.PartnerCourseId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.LearningAgreementId);
        builder.HasIndex(x => x.HomeSlotId);
    }
}
