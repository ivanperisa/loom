using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class LearningAgreementConfiguration : IEntityTypeConfiguration<LearningAgreement>
{
    public void Configure(EntityTypeBuilder<LearningAgreement> builder)
    {
        builder.ToTable("learning_agreement");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ExchangeId).HasColumnName("exchange_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.HasOne(x => x.Exchange)
            .WithOne(x => x.LearningAgreement)
            .HasForeignKey<LearningAgreement>(x => x.ExchangeId);
        builder.HasIndex(x => x.ExchangeId).IsUnique();
    }
}
