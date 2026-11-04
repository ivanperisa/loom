using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.ToTable("document_version", "exchange", t =>
        {
            t.HasCheckConstraint("document_version_document_check", "document IN ('LearningAgreement', 'Recognition')");
            t.HasCheckConstraint("document_version_kind_check", "kind IN ('Approved', 'Backup')");
            t.HasCheckConstraint("document_version_number_check", "(kind = 'Approved') = (version_no IS NOT NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.ExchangeId).HasColumnName("exchange_id");
        builder.Property(x => x.Document).HasColumnName("document").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Kind).HasColumnName("kind").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.VersionNo).HasColumnName("version_no");
        builder.Property(x => x.SchemaVersion).HasColumnName("schema_version");
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb");
        builder.Property(x => x.ContentHash).HasColumnName("content_hash").HasMaxLength(64);
        builder.Property(x => x.CreatedById).HasColumnName("created_by_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

        builder.HasOne(x => x.Exchange)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.ExchangeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.ExchangeId, x.Document, x.VersionNo })
            .IsUnique()
            .HasFilter("version_no IS NOT NULL")
            .HasDatabaseName("document_version_number_key");
        builder.HasIndex(x => new { x.ExchangeId, x.Document, x.CreatedAt }).HasDatabaseName("idx_document_version_exchange");
    }
}
