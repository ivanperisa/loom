using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

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
        builder.HasIndex(x => x.Jmbag).IsUnique().HasFilter("jmbag IS NOT NULL");
        builder.Property(x => x.Mentor).HasColumnName("mentor").HasMaxLength(255);
        builder.Property(x => x.InstitutionId).HasColumnName("institution_id");
        builder.Property(x => x.CoordinatorId).HasColumnName("coordinator_id");
        builder.Property(x => x.CoordinatorRequestStatus).HasColumnName("coordinator_request_status").HasMaxLength(20);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()").IsRequired();

        builder.HasIndex(x => x.ExternalId).IsUnique();
        builder.HasIndex(x => x.Email);
        builder.HasIndex(x => x.CoordinatorId);
        builder.HasIndex(x => x.InstitutionId);

        builder.HasOne(x => x.Institution)
            .WithMany()
            .HasForeignKey(x => x.InstitutionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Coordinator)
            .WithMany()
            .HasForeignKey(x => x.CoordinatorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.StudentExchanges)
            .WithOne(x => x.Student)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
