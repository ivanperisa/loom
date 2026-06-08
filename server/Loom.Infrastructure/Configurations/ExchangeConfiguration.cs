using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class ExchangeConfiguration : IEntityTypeConfiguration<Exchange>
{
    public void Configure(EntityTypeBuilder<Exchange> builder)
    {
        builder.ToTable("exchange", "exchange");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.Guid).HasColumnName("guid").HasDefaultValueSql("gen_random_uuid()").IsRequired();
        builder.HasIndex(x => x.Guid).IsUnique();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.HomeProfileId).HasColumnName("home_profile_id").IsRequired();
        builder.Property(x => x.PartnerInstitutionId).HasColumnName("partner_institution_id").IsRequired();
        builder.Property(x => x.CoordinatorId).HasColumnName("coordinator_id");
        builder.Property(x => x.AcademicYear).HasColumnName("academic_year").IsRequired();
        builder.Property(x => x.SemesterType).HasColumnName("semester_type").HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.StudySemesters).HasColumnName("study_semesters").IsRequired();
        builder.Property(x => x.CoordinatorMessage).HasColumnName("coordinator_message");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()").IsRequired();

        builder.HasOne(x => x.Student)
            .WithMany(x => x.StudentExchanges)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.HomeProfile)
            .WithMany()
            .HasForeignKey(x => x.HomeProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PartnerInstitution)
            .WithMany()
            .HasForeignKey(x => x.PartnerInstitutionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Coordinator)
            .WithMany()
            .HasForeignKey(x => x.CoordinatorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.StudentId);
        builder.HasIndex(x => x.CoordinatorId);
        builder.HasIndex(x => x.HomeProfileId);
        builder.HasIndex(x => x.PartnerInstitutionId);
    }
}
