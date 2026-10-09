using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class MappingSchemeEntryConfiguration : IEntityTypeConfiguration<MappingSchemeEntry>
{
    public void Configure(EntityTypeBuilder<MappingSchemeEntry> builder)
    {
        builder.ToTable("mapping_scheme_entry", "exchange", t =>
            t.HasCheckConstraint("mapping_scheme_entry_awarded_ects_check", "awarded_ects >= 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.RecognitionEntryId).HasColumnName("recognition_entry_id");
        builder.Property(x => x.HomeSlotId).HasColumnName("home_slot_id");
        builder.Property(x => x.AwardedEcts).HasColumnName("awarded_ects").HasPrecision(4, 1);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasOne(x => x.RecognitionEntry)
            .WithMany(x => x.Placements)
            .HasForeignKey(x => x.RecognitionEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.HomeSlot)
            .WithMany()
            .HasForeignKey(x => x.HomeSlotId)
            .OnDelete(DeleteBehavior.NoAction);

        // A course sits in a slot at most once; moving more of it there adds to that placement.
        builder.HasIndex(x => new { x.RecognitionEntryId, x.HomeSlotId }).IsUnique().HasDatabaseName("mapping_scheme_entry_slot_key");
        builder.HasIndex(x => x.HomeSlotId).HasDatabaseName("idx_mapping_scheme_entry_slot");
    }
}
