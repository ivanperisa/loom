using ExchangeMapper.Domain.Entities;
using ExchangeMapper.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExchangeMapper.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.ExternalId).HasColumnName("external_id").IsRequired();
        builder.Property(x => x.Email).HasColumnName("email").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasDefaultValue(UserRole.Student)
            .IsRequired();
        builder.Property(x => x.IsOnboarded).HasColumnName("is_onboarded").IsRequired();
        builder.Property(x => x.Jmbag).HasColumnName("jmbag").HasMaxLength(10);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()").IsRequired();

        builder.HasIndex(x => x.ExternalId).IsUnique();

        builder.HasMany(x => x.StudentExchanges)
            .WithOne(x => x.Student)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.MappingHistoryChanges)
            .WithOne(x => x.ChangedByUser)
            .HasForeignKey(x => x.ChangedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
