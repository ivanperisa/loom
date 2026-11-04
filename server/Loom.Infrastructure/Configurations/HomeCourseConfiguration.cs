using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class HomeCourseConfiguration : IEntityTypeConfiguration<HomeCourse>
{
    public void Configure(EntityTypeBuilder<HomeCourse> builder)
    {
        builder.ToTable("course", "home");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.IsvuCode).HasColumnName("isvu_code").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.NameEn).HasColumnName("name_en").HasMaxLength(255);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

        builder.HasIndex(x => x.IsvuCode).IsUnique().HasDatabaseName("idx_home_course_isvu");
    }
}
