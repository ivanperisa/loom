using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class HomeProfileConfiguration : IEntityTypeConfiguration<HomeProfile>
{
    public void Configure(EntityTypeBuilder<HomeProfile> builder)
    {
        builder.ToTable("profile", "home");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.ProgramId).HasColumnName("program_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.NameEn).HasColumnName("name_en");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasOne(x => x.Program)
            .WithMany(x => x.Profiles)
            .HasForeignKey(x => x.ProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ProgramId);
    }
}
