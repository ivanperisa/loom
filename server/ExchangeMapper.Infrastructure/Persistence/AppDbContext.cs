using ExchangeMapper.Application.Interfaces;
using ExchangeMapper.Domain.Common;
using ExchangeMapper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMapper.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Institution> Institutions => Set<Institution>();
    public DbSet<StudyProgram> StudyPrograms => Set<StudyProgram>();
    public DbSet<StudyProfile> StudyProfiles => Set<StudyProfile>();
    public DbSet<Exchange> Exchanges => Set<Exchange>();
    public DbSet<ForeignProgram> ForeignPrograms => Set<ForeignProgram>();
    public DbSet<ForeignCourse> ForeignCourses => Set<ForeignCourse>();
    public DbSet<CourseSlot> CourseSlots => Set<CourseSlot>();
    public DbSet<SlotState> SlotStates => Set<SlotState>();
    public DbSet<SlotMapping> SlotMappings => Set<SlotMapping>();
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
