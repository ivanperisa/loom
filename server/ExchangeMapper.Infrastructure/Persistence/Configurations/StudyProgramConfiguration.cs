using ExchangeMapper.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExchangeMapper.Infrastructure.Persistence.Configurations;

public class StudyProgramConfiguration : IEntityTypeConfiguration<StudyProgram>
{
    public void Configure(EntityTypeBuilder<StudyProgram> builder)
    {
        builder.ToTable("study_program");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.InstitutionId).HasColumnName("institution_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.NameEn).HasColumnName("name_en").IsRequired();
        builder.Property(x => x.Level).HasColumnName("level").HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.DurationSemesters).HasColumnName("duration_semesters");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()").IsRequired();

        builder.HasOne(x => x.Institution)
            .WithMany(x => x.StudyPrograms)
            .HasForeignKey(x => x.InstitutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.StudyProfiles)
            .WithOne(x => x.StudyProgram)
            .HasForeignKey(x => x.StudyProgramId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
