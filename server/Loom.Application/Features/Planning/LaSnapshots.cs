using System.Text.Json;
using Loom.Application.Common;
using Loom.Application.Interfaces;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Planning;

/// <summary>Point-in-time copies of a learning agreement (on approval, and backups before import/restore).</summary>
public sealed class LaSnapshots(IAppDbContext db)
{
    public async Task<LaSnapshotData> CaptureAsync(int exchangeId, CancellationToken ct)
    {
        var entries = await db.LearningAgreementEntries
            .AsNoTracking()
            .Where(e => e.LearningAgreement.ExchangeId == exchangeId)
            .OrderBy(e => e.Id)
            .Include(e => e.PartnerCourse)
            .Include(e => e.HomeSlot).ThenInclude(s => s.Course)
            .Include(e => e.HomeSlot).ThenInclude(s => s.CourseGroup)
            .ToListAsync(ct);

        return new LaSnapshotData(entries.Select(e => new LaSnapshotEntry(
            e.HomeSlotId,
            e.HomeSlot.Label,
            e.HomeSlot.Semester,
            e.HomeSlot.Ects,
            e.Mode.ToString(),
            e.PartnerCourseId,
            e.PartnerCourse?.Code,
            e.PartnerCourse?.Name,
            e.AwardedEcts,
            e.PartnerCourse?.NameHr,
            e.PartnerCourse?.Url)).ToList());
    }

    public void Add(int exchangeId, SnapshotType type, LaSnapshotData data, int changedById) =>
        db.ExchangeSnapshots.Add(new ExchangeSnapshot
        {
            ExchangeId = exchangeId,
            ChangedById = changedById,
            Phase = SnapshotPhase.LearningAgreement,
            Type = type,
            Snapshot = JsonSerializer.Serialize(data, JsonHelper.DefaultOptions),
        });

    /// <summary>Backup before a destructive change; skipped when there is nothing to back up.</summary>
    public async Task AddBackupAsync(int exchangeId, int changedById, CancellationToken ct)
    {
        var data = await CaptureAsync(exchangeId, ct);
        if (data.Entries.Count > 0) Add(exchangeId, SnapshotType.PreImport, data, changedById);
    }

    public static LaSnapshotData? Read(ExchangeSnapshot snapshot) =>
        JsonSerializer.Deserialize<LaSnapshotData>(snapshot.Snapshot, JsonHelper.DefaultOptions);
}
