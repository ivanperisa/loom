using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class PartnerCourseConfiguration : IEntityTypeConfiguration<PartnerCourse>
{
    public void Configure(EntityTypeBuilder<PartnerCourse> builder)
    {
        builder.ToTable("course", "partner");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.ProgramId).HasColumnName("program_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").IsRequired();
        builder.Property(x => x.NameEn).HasColumnName("name_en").IsRequired();
        builder.Property(x => x.NameHr).HasColumnName("name_hr");
        builder.Property(x => x.Ects).HasColumnName("ects").HasPrecision(4, 1);
        builder.Property(x => x.LecturesH).HasColumnName("lectures_h");
        builder.Property(x => x.AuditoryH).HasColumnName("auditory_h");
        builder.Property(x => x.LabH).HasColumnName("lab_h");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasOne(x => x.Program)
            .WithMany(x => x.Courses)
            .HasForeignKey(x => x.ProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ProgramId);
    }
}
