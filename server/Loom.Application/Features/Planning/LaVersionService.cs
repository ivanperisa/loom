using ErrorOr;
using Loom.Application.Common.Security;
using Loom.Application.Interfaces;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Planning;

/// <summary>Approval history (with changes between approvals), restore points, and restoring one into the draft.</summary>
public sealed class LaVersionService(IAppDbContext db, ExchangeAccess access, ICurrentActor actor, LaEntryWriter writer, LaSnapshots snapshots)
{
    public async Task<ErrorOr<List<LaSnapshotSummary>>> GetApprovalHistoryAsync(Guid exchangeGuid, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;

        var approvals = await db.ExchangeSnapshots
            .AsNoTracking()
            .Include(s => s.ChangedBy)
            .Where(s => s.ExchangeId == context.Value.ExchangeId && s.Phase == SnapshotPhase.LearningAgreement && s.Type == SnapshotType.Auto)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(ct);

        var history = new List<LaSnapshotSummary>();
        var previous = new LaSnapshotData([]);
        foreach (var approval in approvals)
        {
            if (LaSnapshots.Read(approval) is not { } data) continue;
            history.Add(new LaSnapshotSummary(approval.Id, approval.CreatedAt, approval.ChangedBy.Name, data.Entries.Count, Diff(data, previous)));
            previous = data;
        }

        history.Reverse();
        return history;
    }

    public async Task<ErrorOr<List<SnapshotListItem>>> ListSnapshotsAsync(Guid exchangeGuid, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;

        var all = await db.ExchangeSnapshots
            .AsNoTracking()
            .Include(s => s.ChangedBy)
            .Where(s => s.ExchangeId == context.Value.ExchangeId && s.Phase == SnapshotPhase.LearningAgreement)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

        return all.Select(s => new SnapshotListItem(s.Id, s.Type, s.CreatedAt, s.ChangedBy.Name, LaSnapshots.Read(s)?.Entries.Count ?? 0)).ToList();
    }

    /// <summary>
    /// Loads a snapshot into the draft (never changes the status). Entries that no longer fit the exchange
    /// (slot left the profile, course gone) are dropped; a backup of the current draft is kept.
    /// </summary>
    public async Task<ErrorOr<Updated>> RestoreAsync(Guid exchangeGuid, int snapshotId, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        var exchange = context.Value;

        var snapshot = await db.ExchangeSnapshots.FirstOrDefaultAsync(
            s => s.Id == snapshotId && s.ExchangeId == exchange.ExchangeId && s.Phase == SnapshotPhase.LearningAgreement, ct);
        if (snapshot is null) return PlanningErrors.SnapshotNotFound;
        if (LaSnapshots.Read(snapshot) is not { } data) return PlanningErrors.SnapshotCorrupted;

        var status = await db.LearningAgreements.Where(l => l.ExchangeId == exchange.ExchangeId).Select(l => (DocumentStatus?)l.Status).FirstOrDefaultAsync(ct);
        if (status is not null and not DocumentStatus.Draft) return PlanningErrors.Locked;

        var request = await ToRestorableRequestAsync(data, exchange, ct);
        var valid = await writer.ValidateAsync(request, exchange, ct);
        if (valid.IsError) return valid.Errors;

        await snapshots.AddBackupAsync(exchange.ExchangeId, actor.UserId, ct);

        var learningAgreement = await writer.GetOrCreateAsync(exchange.ExchangeId, ct);
        var replaced = await writer.ReplaceEntriesAsync(learningAgreement.Id, request, ct);
        if (replaced.IsError) return replaced.Errors;

        learningAgreement.LastModifiedById = actor.UserId;
        await db.SaveChangesAsync(ct);
        return Result.Updated;
    }

    /// <summary>Keeps entries whose slot is in the profile and whose course still exists at the partner (by id, then code).</summary>
    private async Task<SaveLearningAgreementRequest> ToRestorableRequestAsync(LaSnapshotData data, ExchangeContext exchange, CancellationToken ct)
    {
        var profileSlotIds = await db.HomeSlots.Where(s => s.ProfileId == exchange.HomeProfileId).Select(s => s.Id).ToHashSetAsync(ct);
        var courses = await db.PartnerCourses
            .Where(c => c.InstitutionId == exchange.PartnerInstitutionId && !c.IsDeleted)
            .Select(c => new { c.Id, c.Code })
            .ToListAsync(ct);
        var courseIds = courses.Select(c => c.Id).ToHashSet();
        var courseIdByCode = courses.GroupBy(c => c.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        var entries = new List<LearningAgreementEntryUpsertDto>();
        foreach (var entry in data.Entries.Where(e => profileSlotIds.Contains(e.HomeSlotId)))
        {
            int? courseId = null;
            if (entry.PartnerCourseId is int id)
            {
                if (courseIds.Contains(id)) courseId = id;
                else if (entry.PartnerCourseCode is not null && courseIdByCode.TryGetValue(entry.PartnerCourseCode, out var byCode)) courseId = byCode;
                else continue;
            }
            entries.Add(new LearningAgreementEntryUpsertDto(entry.HomeSlotId, entry.Mode, courseId, entry.AwardedEcts));
        }
        return new SaveLearningAgreementRequest(entries);
    }

    private static LaSnapshotDiff Diff(LaSnapshotData current, LaSnapshotData previous)
    {
        var before = previous.Entries.Where(e => e.PartnerCourseId.HasValue).ToDictionary(e => (e.HomeSlotId, e.PartnerCourseId!.Value));
        var after = current.Entries.Where(e => e.PartnerCourseId.HasValue).ToDictionary(e => (e.HomeSlotId, e.PartnerCourseId!.Value));

        return new LaSnapshotDiff(
            Added: after.Where(kv => !before.ContainsKey(kv.Key)).Select(kv => kv.Value).ToList(),
            Removed: before.Where(kv => !after.ContainsKey(kv.Key)).Select(kv => kv.Value).ToList(),
            Modified: after
                .Where(kv => before.TryGetValue(kv.Key, out var old) && (old.AwardedEcts != kv.Value.AwardedEcts || old.Mode != kv.Value.Mode))
                .Select(kv => new LaSnapshotEntryChange(before[kv.Key], kv.Value))
                .ToList());
    }
}
