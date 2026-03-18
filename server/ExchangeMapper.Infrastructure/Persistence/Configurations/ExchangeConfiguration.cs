using ExchangeMapper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExchangeMapper.Infrastructure.Persistence.Configurations;

public class ExchangeConfiguration : IEntityTypeConfiguration<Exchange>
{
    public void Configure(EntityTypeBuilder<Exchange> builder)
    {
        builder.ToTable("exchange");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.StudyProfileId).HasColumnName("study_profile_id").IsRequired();
        builder.Property(x => x.ForeignProgramId).HasColumnName("foreign_program_id").IsRequired();
        builder.Property(x => x.CoordinatorId).HasColumnName("coordinator_id");
        builder.Property(x => x.Mentor).HasColumnName("mentor").HasMaxLength(255);
        builder.Property(x => x.AcademicYear).HasColumnName("academic_year").IsRequired();
        builder.Property(x => x.SemesterType).HasColumnName("semester_type").HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.StudySemester).HasColumnName("study_semester").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()").IsRequired();

        builder.HasOne(x => x.Student)
            .WithMany(x => x.StudentExchanges)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.StudyProfile)
            .WithMany()
            .HasForeignKey(x => x.StudyProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Coordinator)
            .WithMany()
            .HasForeignKey(x => x.CoordinatorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
