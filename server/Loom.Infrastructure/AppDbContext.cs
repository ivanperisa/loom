using Loom.Application.Interfaces;
using Loom.Domain.Common;
using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
    public DbSet<ExchangeSnapshot> ExchangeSnapshots => Set<ExchangeSnapshot>();
    public DbSet<CoordinatorWhitelist> CoordinatorWhitelist => Set<CoordinatorWhitelist>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
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
