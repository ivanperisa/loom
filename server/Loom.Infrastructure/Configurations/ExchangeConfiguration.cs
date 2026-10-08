using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class ExchangeConfiguration : IEntityTypeConfiguration<Exchange>
{
    public void Configure(EntityTypeBuilder<Exchange> builder)
    {
        builder.ToTable("exchange", "exchange", t =>
            t.HasCheckConstraint("exchange_semester_type_check", "semester_type IN ('Winter', 'Summer', 'Both')"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.Guid).HasColumnName("guid").HasDefaultValueSql("gen_random_uuid()").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.HomeProfileId).HasColumnName("home_profile_id").IsRequired();
        builder.Property(x => x.PartnerInstitutionId).HasColumnName("partner_institution_id").IsRequired();
        builder.Property(x => x.CoordinatorId).HasColumnName("coordinator_id");
        builder.Property(x => x.AcademicYear).HasColumnName("academic_year").HasMaxLength(10).IsRequired();
        builder.Property(x => x.SemesterType).HasColumnName("semester_type").HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.StudySemesters).HasColumnName("study_semesters").IsRequired();
        builder.Property(x => x.CoordinatorMessage).HasColumnName("coordinator_message");
        builder.Property(x => x.EwpLink).HasColumnName("ewp_link").HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired();

        builder.HasOne(x => x.Student)
            .WithMany(x => x.StudentExchanges)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.HomeProfile)
            .WithMany()
            .HasForeignKey(x => x.HomeProfileId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.PartnerInstitution)
            .WithMany()
            .HasForeignKey(x => x.PartnerInstitutionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Coordinator)
            .WithMany()
            .HasForeignKey(x => x.CoordinatorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.Guid).IsUnique().HasDatabaseName("exchange_guid_key");
        builder.HasIndex(x => x.StudentId).HasDatabaseName("idx_exchange_student");
        builder.HasIndex(x => x.CoordinatorId).HasDatabaseName("idx_exchange_coordinator");
        builder.HasIndex(x => x.HomeProfileId).HasDatabaseName("idx_exchange_home_profile");
        builder.HasIndex(x => x.PartnerInstitutionId).HasDatabaseName("idx_exchange_partner_institution");
    }
}
