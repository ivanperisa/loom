using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class RecognitionConfiguration : IEntityTypeConfiguration<Recognition>
{
    public void Configure(EntityTypeBuilder<Recognition> builder)
    {
        builder.ToTable("recognition", "exchange", t =>
            t.HasCheckConstraint("recognition_status_check", "status IN ('Draft', 'Approved')"));
        builder.HasKey(x => x.Id);
        // Optimistic concurrency via PostgreSQL's xmin: concurrent writes to the same row fail instead of overwriting.
        builder.Property<uint>("Version").IsRowVersion();
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.ExchangeId).HasColumnName("exchange_id");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).HasDefaultValue(DocumentStatus.Draft);
        builder.Property(x => x.Message).HasColumnName("message");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(x => x.LastModifiedById).HasColumnName("updated_by");
        builder.Property(x => x.SignedById).HasColumnName("approved_by");
        builder.Property(x => x.SignedAt).HasColumnName("approved_at");
        builder.Property(x => x.StartedAt).HasColumnName("started_at");
        builder.Property(x => x.StartedById).HasColumnName("started_by");
        builder.Ignore(x => x.IsStarted);

        builder.HasOne(x => x.Exchange)
            .WithOne(x => x.Recognition)
            .HasForeignKey<Recognition>(x => x.ExchangeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.LastModifiedByUser)
            .WithMany()
            .HasForeignKey(x => x.LastModifiedById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.SignedByUser)
            .WithMany()
            .HasForeignKey(x => x.SignedById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.StartedByUser)
            .WithMany()
            .HasForeignKey(x => x.StartedById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.ExchangeId).IsUnique().HasDatabaseName("recognition_exchange_id_key");
    }
}
