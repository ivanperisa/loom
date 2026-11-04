using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("user", t =>
        {
            t.HasCheckConstraint("user_role_check", "role IN ('Student', 'Coordinator', 'Admin')");
            t.HasCheckConstraint("user_coordinator_request_status_check", "coordinator_request_status IN ('Pending', 'Rejected')");
        });
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.IsPlaceholder);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.ExternalId).HasColumnName("external_id").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(UserRole.Student)
            .IsRequired();
        builder.Property(x => x.IsOnboarded).HasColumnName("is_onboarded").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.Jmbag).HasColumnName("jmbag").HasMaxLength(10);
        builder.Property(x => x.Mentor).HasColumnName("mentor").HasMaxLength(255);
        builder.Property(x => x.InstitutionId).HasColumnName("institution_id");
        builder.Property(x => x.CoordinatorId).HasColumnName("coordinator_id");
        builder.Property(x => x.CoordinatorRequestStatus).HasColumnName("coordinator_request_status").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        // Exists in the database but is not used by the domain yet.
        builder.Property<DateTime>("UpdatedAt").HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();

        builder.HasOne(x => x.Institution)
            .WithMany()
            .HasForeignKey(x => x.InstitutionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Coordinator)
            .WithMany()
            .HasForeignKey(x => x.CoordinatorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.ExternalId).IsUnique().HasDatabaseName("user_external_id_key");
        builder.HasIndex(x => x.Jmbag).IsUnique().HasFilter("jmbag IS NOT NULL").HasDatabaseName("idx_user_jmbag_not_null");
        builder.HasIndex(x => x.Email).HasDatabaseName("idx_user_email");
        builder.HasIndex(x => x.CoordinatorId).HasDatabaseName("idx_user_coordinator");
        builder.HasIndex(x => x.InstitutionId).HasDatabaseName("idx_user_institution");
    }
}
