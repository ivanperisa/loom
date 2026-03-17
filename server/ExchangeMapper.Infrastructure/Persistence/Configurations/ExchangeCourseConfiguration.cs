using ExchangeMapper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExchangeMapper.Infrastructure.Persistence.Configurations;

public class ExchangeCourseConfiguration : IEntityTypeConfiguration<ExchangeCourse>
{
    public void Configure(EntityTypeBuilder<ExchangeCourse> builder)
    {
        builder.ToTable("exchange_course");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.ExchangeId).HasColumnName("exchange_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50);
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.NameEn).HasColumnName("name_en").IsRequired();
        builder.Property(x => x.Ects).HasColumnName("ects").HasColumnType("numeric(4,1)");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.LecturesHours).HasColumnName("lectures_hours");
        builder.Property(x => x.AuditoryHours).HasColumnName("auditory_hours");
        builder.Property(x => x.LabHours).HasColumnName("lab_hours");
        builder.Property(x => x.OriginalGrade).HasColumnName("original_grade").HasMaxLength(20);
        builder.Property(x => x.EctsGrade).HasColumnName("ects_grade").HasMaxLength(5);
        builder.Property(x => x.ExamDate).HasColumnName("exam_date");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()").IsRequired();

        builder.HasOne(x => x.Exchange)
            .WithMany(x => x.ExchangeCourses)
            .HasForeignKey(x => x.ExchangeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
