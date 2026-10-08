using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class LearningAgreementEntryConfiguration : IEntityTypeConfiguration<LearningAgreementEntry>
{
    public void Configure(EntityTypeBuilder<LearningAgreementEntry> builder)
    {
        builder.ToTable("learning_agreement_entry", "exchange", t =>
            t.HasCheckConstraint("learning_agreement_entry_mode_check", "mode IN ('AtHome', 'AtExchange', 'AfterExchange')"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.LearningAgreementId).HasColumnName("learning_agreement_id");
        builder.Property(x => x.HomeSlotId).HasColumnName("home_slot_id");
        builder.Property(x => x.Mode).HasColumnName("mode").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.PartnerCourseId).HasColumnName("partner_course_id");
        builder.Property(x => x.AwardedEcts).HasColumnName("awarded_ects").HasPrecision(4, 1);
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

        builder.HasOne(x => x.LearningAgreement)
            .WithMany(x => x.Entries)
            .HasForeignKey(x => x.LearningAgreementId)
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

        builder.HasIndex(x => x.LearningAgreementId).HasDatabaseName("idx_la_entry_la");
        builder.HasIndex(x => x.HomeSlotId).HasDatabaseName("idx_la_entry_slot");
        builder.HasIndex(x => x.PartnerCourseId).HasDatabaseName("idx_la_entry_partner_course");
    }
}
