using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class StudyProfileConfiguration : IEntityTypeConfiguration<StudyProfile>
{
    public void Configure(EntityTypeBuilder<StudyProfile> builder)
    {
        builder.ToTable("study_profile");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.StudyProgramId).HasColumnName("study_program_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.NameEn).HasColumnName("name_en").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()").IsRequired();

        builder.HasOne(x => x.StudyProgram)
            .WithMany(x => x.StudyProfiles)
            .HasForeignKey(x => x.StudyProgramId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
