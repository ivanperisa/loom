using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class CoordinatorWhitelistConfiguration : IEntityTypeConfiguration<CoordinatorWhitelist>
{
    public void Configure(EntityTypeBuilder<CoordinatorWhitelist> builder)
    {
        builder.ToTable("coordinator_whitelist");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        builder.Property(x => x.InstitutionId).HasColumnName("institution_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()").IsRequired();

        builder.HasOne(x => x.Institution)
            .WithMany()
            .HasForeignKey(x => x.InstitutionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.Email).IsUnique();
    }
}
