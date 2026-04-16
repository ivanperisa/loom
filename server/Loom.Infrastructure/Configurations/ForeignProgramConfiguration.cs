using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class ForeignProgramConfiguration : IEntityTypeConfiguration<ForeignProgram>
{
    public void Configure(EntityTypeBuilder<ForeignProgram> builder)
    {
        builder.ToTable("foreign_program");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.InstitutionId).HasColumnName("institution_id");
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.NameEn).HasColumnName("name_en");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.HasOne(x => x.Institution)
            .WithMany(x => x.ForeignPrograms)
            .HasForeignKey(x => x.InstitutionId);
        builder.HasIndex(x => x.InstitutionId);
    }
}
