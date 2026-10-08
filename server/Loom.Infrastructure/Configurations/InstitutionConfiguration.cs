using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loom.Infrastructure.Configurations;

public class InstitutionConfiguration : IEntityTypeConfiguration<Institution>
{
    public void Configure(EntityTypeBuilder<Institution> builder)
    {
        builder.ToTable("institution", t =>
            t.HasCheckConstraint("institution_institution_type_check", "institution_type IN ('Home', 'Partner')"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.NameHr).HasColumnName("name_hr").HasMaxLength(255);
        builder.Property(x => x.Country).HasColumnName("country").HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.City).HasColumnName("city").HasMaxLength(100);
        builder.Property(x => x.ErasmusCode).HasColumnName("erasmus_code").HasMaxLength(20);
        builder.Property(x => x.Type)
            .HasColumnName("institution_type")
            .HasConversion<string>()
            .HasMaxLength(10)
            .HasDefaultValue(InstitutionType.Partner)
            // Without a sentinel, EF would treat Home (the CLR default) as "unset" and the DB default would turn it into Partner.
            .HasSentinel((InstitutionType)(-1))
            .IsRequired();
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
    }
}
