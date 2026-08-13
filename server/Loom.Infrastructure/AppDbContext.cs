using Loom.Application.DTOs.Admin;
using Loom.Application.Interfaces;
using Loom.Domain.Common;
using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;

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

    public async Task<SqlExecutionResult> ExecuteSqlAsync(string sql, CancellationToken cancellationToken = default)
    {
        var connection = Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (reader.FieldCount == 0)
        {
            var rowsAffected = reader.RecordsAffected;
            return new SqlExecutionResult(rowsAffected, null);
        }

        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }
        return new SqlExecutionResult(reader.RecordsAffected, rows);
    }
}
