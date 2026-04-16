using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class ForeignCourseConfiguration : IEntityTypeConfiguration<ForeignCourse>
{
    public void Configure(EntityTypeBuilder<ForeignCourse> builder)
    {
        builder.ToTable("foreign_course");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ForeignProgramId).HasColumnName("foreign_program_id");
        builder.Property(x => x.Code).HasColumnName("code").IsRequired();
        builder.Property(x => x.NameEn).HasColumnName("name_en").IsRequired();
        builder.Property(x => x.NameHr).HasColumnName("name_hr");
        builder.Property(x => x.Ects).HasColumnName("ects").HasPrecision(4, 1);
        builder.Property(x => x.LecturesH).HasColumnName("lectures_h");
        builder.Property(x => x.AuditoryH).HasColumnName("auditory_h");
        builder.Property(x => x.LabH).HasColumnName("lab_h");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.HasOne(x => x.ForeignProgram)
            .WithMany(x => x.Courses)
            .HasForeignKey(x => x.ForeignProgramId);
        builder.HasIndex(x => x.ForeignProgramId);
    }
}
