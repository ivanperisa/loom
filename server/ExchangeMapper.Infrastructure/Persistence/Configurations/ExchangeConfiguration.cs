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
        builder.Property(x => x.UserInstitutionId).HasColumnName("user_institution_id").IsRequired();
        builder.Property(x => x.ForeignInstitutionId).HasColumnName("foreign_institution_id").IsRequired();
        builder.Property(x => x.AcademicYear).HasColumnName("academic_year").IsRequired();
        builder.Property(x => x.Semester).HasColumnName("semester").HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.DurationMonths).HasColumnName("duration_months");
        builder.Property(x => x.Mentor).HasColumnName("mentor").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()").IsRequired();

        builder.HasOne(x => x.Student)
            .WithMany(x => x.StudentExchanges)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.UserInstitution)
            .WithMany(x => x.Exchanges)
            .HasForeignKey(x => x.UserInstitutionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ForeignInstitution)
            .WithMany(x => x.ForeignExchanges)
            .HasForeignKey(x => x.ForeignInstitutionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
