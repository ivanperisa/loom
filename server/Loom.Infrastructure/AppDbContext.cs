using Loom.Application.Common.Querying;
using Loom.Application.Interfaces;
using Loom.Domain.Common;
using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Loom.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Institution> Institutions => Set<Institution>();
    public DbSet<HomeProgram> HomePrograms => Set<HomeProgram>();
    public DbSet<HomeProfile> HomeProfiles => Set<HomeProfile>();
    public DbSet<HomeCourse> HomeCourses => Set<HomeCourse>();
    public DbSet<HomeCourseGroup> HomeCourseGroups => Set<HomeCourseGroup>();
    public DbSet<HomeSlot> HomeSlots => Set<HomeSlot>();
    public DbSet<HomeSlotType> HomeSlotTypes => Set<HomeSlotType>();
    public DbSet<PartnerCourse> PartnerCourses => Set<PartnerCourse>();
    public DbSet<Exchange> Exchanges => Set<Exchange>();
    public DbSet<LearningAgreement> LearningAgreements => Set<LearningAgreement>();
    public DbSet<LearningAgreementEntry> LearningAgreementEntries => Set<LearningAgreementEntry>();
    public DbSet<Recognition> Recognitions => Set<Recognition>();
    public DbSet<RecognitionEntry> RecognitionEntries => Set<RecognitionEntry>();
    public DbSet<MappingSchemeEntry> MappingSchemeEntries => Set<MappingSchemeEntry>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<CoordinatorWhitelist> CoordinatorWhitelist => Set<CoordinatorWhitelist>();
    public DbSet<ExchangeAccessLink> ExchangeAccessLinks => Set<ExchangeAccessLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.HasDbFunction(typeof(TextSearch).GetMethod(nameof(TextSearch.Unaccent))!)
            .HasName("f_unaccent").HasSchema("public");
        modelBuilder.UseSerialColumns();
        ApplyPostgresNaming(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Names keys and indexes the way PostgreSQL names them by default (the schema predates EF migrations),
    /// so migrations can reference existing objects by their real names.
    /// </summary>
    private static void ApplyPostgresNaming(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var table = entity.GetTableName();
            if (table is null) continue;

            entity.FindPrimaryKey()?.SetName($"{table}_pkey");

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var columns = string.Join("_", foreignKey.Properties.Select(p => p.GetColumnName()));
                foreignKey.SetConstraintName($"{table}_{columns}_fkey");
            }

            foreach (var index in entity.GetIndexes().Where(i => i.FindAnnotation(RelationalAnnotationNames.Name) is null))
            {
                var columns = string.Join("_", index.Properties.Select(p => p.GetColumnName()));
                index.SetDatabaseName($"idx_{table}_{columns}");
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
