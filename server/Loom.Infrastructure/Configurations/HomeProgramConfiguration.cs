using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class HomeProgramConfiguration : IEntityTypeConfiguration<HomeProgram>
{
    public void Configure(EntityTypeBuilder<HomeProgram> builder)
    {
        builder.ToTable("program", "home");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.InstitutionId).HasColumnName("institution_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.NameEn).HasColumnName("name_en");
        builder.Property(x => x.Level).HasColumnName("level").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.DurationSemesters).HasColumnName("duration_semesters");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasOne(x => x.Institution)
            .WithMany(x => x.HomePrograms)
            .HasForeignKey(x => x.InstitutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.InstitutionId);
    }
}
