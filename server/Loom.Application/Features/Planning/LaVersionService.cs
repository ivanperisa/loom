using ErrorOr;
using Loom.Application.Common.Security;
using Loom.Application.Features.Documents;
using Loom.Application.Interfaces;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Planning;

/// <summary>The LA's history (approvals with what changed, and backups) and restoring a version into the draft.</summary>
public sealed class LaVersionService(
    IAppDbContext db, ExchangeAccess access, ICurrentActor actor, LaEntryWriter writer, LaContent content, VersionStore versions)
{
    public async Task<ErrorOr<List<DocumentVersionResponse>>> ListAsync(Guid exchangeGuid, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;

        var all = await db.DocumentVersions
            .AsNoTracking()
            .Include(v => v.CreatedBy)
            .Where(v => v.ExchangeId == context.Value.ExchangeId && v.Document == DocumentKind.LearningAgreement)
            .OrderBy(v => v.CreatedAt).ThenBy(v => v.Id)
            .ToListAsync(ct);

        var result = new List<DocumentVersionResponse>();
        var previous = new LaVersionPayload([]);
        foreach (var version in all)
        {
            var payload = VersionStore.Deserialize<LaVersionPayload>(version) ?? new LaVersionPayload([]);
            List<DocumentChange>? changes = null;
            if (version.Kind == VersionKind.Approved)
            {
                changes = DocumentDiff.Compare(LaContent.DiffRows(previous), LaContent.DiffRows(payload));
                previous = payload;
            }
            result.Add(new DocumentVersionResponse(
                version.Id, version.Kind.ToString(), version.VersionNo, VersionStore.AmendmentLabel(version.VersionNo),
                version.CreatedAt, version.CreatedBy?.Name, payload.Entries.Count(e => e.PartnerCourseId is not null), changes));
        }
        result.Reverse();
        return result;
    }

    /// <summary>
    /// Loads a version (approval or backup) into the draft. Never changes the status; only while the LA is an editable
    /// draft. Courses are matched by id only: anything that no longer exists is reported, not replaced by a look-alike.
    /// The current draft is kept as a backup first.
    /// </summary>
    public async Task<ErrorOr<RestoreResult>> RestoreAsync(Guid exchangeGuid, int versionId, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        var exchange = context.Value;

        var version = await db.DocumentVersions.AsNoTracking().FirstOrDefaultAsync(
            v => v.Id == versionId && v.ExchangeId == exchange.ExchangeId && v.Document == DocumentKind.LearningAgreement, ct);
        if (version is null) return PlanningErrors.VersionNotFound;
        if (VersionStore.Deserialize<LaVersionPayload>(version) is not { } payload) return PlanningErrors.VersionUnreadable;

        var editable = await writer.CheckEditableAsync(exchange.ExchangeId, ct);
        if (editable.IsError) return editable.Errors;

        var (request, missing) = await ResolveAsync(payload, exchange, ct);
        var valid = await writer.ValidateAsync(request, exchange, ct);
        if (valid.IsError) return valid.Errors;

        var current = await content.CaptureAsync(exchange.ExchangeId, ct);
        var currentHash = LaContent.Hash(current);
        if (currentHash != LaContent.Hash(request))
        {
            await versions.AddBackupAsync(exchange.ExchangeId, DocumentKind.LearningAgreement, VersionStore.Serialize(current), currentHash,
                current.Entries.Count == 0, actor.UserId, ct);
            var learningAgreement = await writer.GetOrCreateAsync(exchange.ExchangeId, ct);
            await writer.ApplyAsync(learningAgreement.Id, request, ct);
            learningAgreement.LastModifiedById = actor.UserId;
            learningAgreement.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        return new RestoreResult(request.Entries.Count(e => e.PartnerCourseId is not null), missing);
    }

    private async Task<(SaveLearningAgreementRequest, List<RestoreMissing>)> ResolveAsync(LaVersionPayload payload, ExchangeContext exchange, CancellationToken ct)
    {
        var profileSlotIds = await db.HomeSlots.Where(s => s.ProfileId == exchange.HomeProfileId).Select(s => s.Id).ToHashSetAsync(ct);
        var courseIds = payload.Entries.Where(e => e.PartnerCourseId.HasValue).Select(e => e.PartnerCourseId!.Value).ToList();
        var existingCourses = await db.PartnerCourses
            .Where(c => courseIds.Contains(c.Id) && c.InstitutionId == exchange.PartnerInstitutionId)
            .Select(c => c.Id)
            .ToHashSetAsync(ct);

        var entries = new List<LearningAgreementEntryUpsertDto>();
        var missing = new List<RestoreMissing>();
        foreach (var entry in payload.Entries)
        {
            if (!profileSlotIds.Contains(entry.HomeSlotId))
                missing.Add(Missing(entry, "SlotNotInProfile"));
            else if (entry.PartnerCourseId is int id && !existingCourses.Contains(id))
                missing.Add(Missing(entry, "CourseNotFound"));
            else
                entries.Add(new LearningAgreementEntryUpsertDto(entry.HomeSlotId, entry.Mode, entry.PartnerCourseId, entry.AwardedEcts));
        }
        return (new SaveLearningAgreementRequest(entries), missing);
    }

    private static RestoreMissing Missing(LaVersionEntry entry, string reason) =>
        new(entry.HomeSlotId, entry.HomeSlotLabel, entry.PartnerCourseCode, entry.PartnerCourseName, reason);
}
