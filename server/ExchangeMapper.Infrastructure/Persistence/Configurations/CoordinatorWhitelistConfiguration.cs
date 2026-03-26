using ExchangeMapper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExchangeMapper.Infrastructure.Persistence.Configurations;

public class CoordinatorWhitelistConfiguration : IEntityTypeConfiguration<CoordinatorWhitelist>
{
    public void Configure(EntityTypeBuilder<CoordinatorWhitelist> builder)
    {
        builder.ToTable("coordinator_whitelist");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()").IsRequired();

        builder.HasIndex(x => x.Email).IsUnique();
    }
}
