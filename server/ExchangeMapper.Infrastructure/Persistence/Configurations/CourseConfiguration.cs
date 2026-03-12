using ExchangeMapper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExchangeMapper.Infrastructure.Persistence.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("course");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.StudyProfileId).HasColumnName("study_profile_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50);
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.NameEn).HasColumnName("name_en").IsRequired();
        builder.Property(x => x.Ects).HasColumnName("ects").HasColumnType("integer").IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.LecturesHours).HasColumnName("lectures_hours");
        builder.Property(x => x.AuditoryHours).HasColumnName("auditory_hours");
        builder.Property(x => x.LabHours).HasColumnName("lab_hours");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()").IsRequired();

        builder.HasOne(x => x.StudyProfile)
            .WithMany(x => x.Courses)
            .HasForeignKey(x => x.StudyProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
