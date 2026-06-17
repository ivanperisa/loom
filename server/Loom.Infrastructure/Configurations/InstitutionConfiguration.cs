using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class InstitutionConfiguration : IEntityTypeConfiguration<Institution>
{
    public void Configure(EntityTypeBuilder<Institution> builder)
    {
        builder.ToTable("institution");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired();
        builder.Property(x => x.NameHr).HasColumnName("name_hr");
        builder.Property(x => x.Country).HasColumnName("country");
        builder.Property(x => x.City).HasColumnName("city");
        builder.Property(x => x.ErasmusCode).HasColumnName("erasmus_code");
        builder.Property(x => x.Type)
            .HasColumnName("institution_type")
            .HasConversion<string>()
            .HasMaxLength(10)
            .HasDefaultValue(InstitutionType.Partner)
            .IsRequired();
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()").IsRequired();
    }
}
