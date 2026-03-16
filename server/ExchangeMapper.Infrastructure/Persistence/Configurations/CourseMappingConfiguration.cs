using ExchangeMapper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExchangeMapper.Infrastructure.Persistence.Configurations;

public class CourseMappingConfiguration : IEntityTypeConfiguration<CourseMapping>
{
    public void Configure(EntityTypeBuilder<CourseMapping> builder)
    {
        builder.ToTable("course_mapping");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.ExchangeCourseId).HasColumnName("exchange_course_id").IsRequired();
        builder.Property(x => x.CourseId).HasColumnName("course_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.CoordinatorNote).HasColumnName("coordinator_note");
        builder.Property(x => x.AwardedEcts).HasColumnName("awarded_ects").HasColumnType("numeric(4,1)");
        builder.Property(x => x.ConvertedGrade).HasColumnName("converted_grade").HasMaxLength(10);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()").IsRequired();

        builder.HasIndex(x => new { x.ExchangeCourseId, x.CourseId }).IsUnique();

        builder.HasOne(x => x.ExchangeCourse)
            .WithMany(x => x.CourseMappings)
            .HasForeignKey(x => x.ExchangeCourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Course)
            .WithMany(x => x.CourseMappings)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
