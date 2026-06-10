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
        builder.Property(x => x.InstitutionId).HasColumnName("institution_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.NameHr).HasColumnName("name_hr");
        builder.Property(x => x.Ects).HasColumnName("ects").HasPrecision(4, 1);
        builder.Property(x => x.LecturesH).HasColumnName("lectures_h");
        builder.Property(x => x.AuditoryH).HasColumnName("auditory_h");
        builder.Property(x => x.LabH).HasColumnName("lab_h");
        builder.Property(x => x.Semester).HasColumnName("semester").HasConversion<string>().HasMaxLength(10);
        builder.Property(x => x.Level).HasColumnName("level").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasOne(x => x.Institution)
            .WithMany(x => x.PartnerCourses)
            .HasForeignKey(x => x.InstitutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.InstitutionId, x.Code }).IsUnique();
    }
}
