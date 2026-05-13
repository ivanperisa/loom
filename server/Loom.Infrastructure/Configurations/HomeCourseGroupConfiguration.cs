using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class HomeCourseGroupConfiguration : IEntityTypeConfiguration<HomeCourseGroup>
{
    public void Configure(EntityTypeBuilder<HomeCourseGroup> builder)
    {
        builder.ToTable("course_group", "home");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.CourseTypeId).HasColumnName("slot_type_id").IsRequired();
        builder.Property(x => x.IsvuCode).HasColumnName("isvu_code");
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.NameEn).HasColumnName("name_en");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasOne(x => x.CourseType)
            .WithMany(x => x.CourseGroups)
            .HasForeignKey(x => x.CourseTypeId);

        builder.HasIndex(x => x.CourseTypeId);
    }
}
