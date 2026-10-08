using ErrorOr;
using Loom.Application.Common.Security;
using Loom.Application.Features.Documents;
using Loom.Application.Interfaces;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Planning;

/// <summary>
/// "Export for import" (JSON) and importing such a file. Import replaces the draft with the file's mappings, after a
/// preview of exactly what will be added, removed and changed. It goes through the same writer as a normal save.
/// </summary>
public sealed class LaTransferService(
    IAppDbContext db, ExchangeAccess access, ICurrentActor actor, LaEntryWriter writer, LaContent content, VersionStore versions)
{
    public async Task<ErrorOr<MappingExportDto>> ExportAsync(Guid exchangeGuid, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        var exchange = context.Value;

        var payload = await content.CaptureAsync(exchange.ExchangeId, ct);
        var courseIds = payload.Entries.Where(e => e.PartnerCourseId.HasValue).Select(e => e.PartnerCourseId!.Value).ToList();
        var courseEcts = await db.PartnerCourses.Where(c => courseIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Ects, ct);
        var exportedBy = await db.Users.Where(u => u.Id == actor.UserId).Select(u => u.Name).FirstAsync(ct);

        return new MappingExportDto(
            Version: MappingExportDto.CurrentVersion,
            ExportedAt: DateTime.UtcNow,
            ExportedByName: exportedBy,
            Institution: await PartnerAsync(exchange, ct),
            Home: await HomeAsync(exchange, ct),
            Mappings: payload.Entries.Select(e => new MappingExportEntry(
                e.HomeSlotId, e.HomeSlotLabel, e.HomeSlotSemester, e.HomeSlotEcts, e.Mode,
                e.PartnerCourseId is int id ? new MappingExportCourse(id, e.PartnerCourseCode!, e.PartnerCourseName!, courseEcts.GetValueOrDefault(id)) : null,
                e.AwardedEcts)).ToList());
    }

    public async Task<ErrorOr<ImportPreviewResponse>> PreviewAsync(Guid exchangeGuid, MappingExportDto file, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        var plan = await BuildPlanAsync(context.Value, file, ct);
        if (plan.IsError) return plan.Errors;
        return plan.Value.Preview;
    }

    public async Task<ErrorOr<ImportResult>> ApplyAsync(Guid exchangeGuid, MappingExportDto file, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        var exchange = context.Value;

        var plan = await BuildPlanAsync(exchange, file, ct);
        if (plan.IsError) return plan.Errors;
        if (plan.Value.Error is { } blocked) return blocked;
        var preview = plan.Value.Preview;

        if (preview.Added.Count + preview.Removed.Count + preview.Changed.Count > 0)
        {
            var current = await content.CaptureAsync(exchange.ExchangeId, ct);
            await versions.AddBackupAsync(exchange.ExchangeId, DocumentKind.LearningAgreement, VersionStore.Serialize(current),
                LaContent.Hash(current), current.Entries.Count == 0, actor.UserId, ct);

            var learningAgreement = await writer.GetOrCreateAsync(exchange.ExchangeId, ct);
            await writer.ApplyAsync(learningAgreement.Id, plan.Value.Request, ct);
            learningAgreement.LastModifiedById = actor.UserId;
            learningAgreement.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        return new ImportResult(preview.Added.Count, preview.Removed.Count, preview.Changed.Count, preview.Skipped);
    }

    private sealed record ImportPlan(ImportPreviewResponse Preview, SaveLearningAgreementRequest Request, Error? Error);

    /// <summary>
    /// Matches the file to this exchange: slots by id (must be in the profile), courses by id when the course belongs to
    /// this partner, otherwise by code when exactly one course has it. Then validates like a save and diffs against the draft.
    /// </summary>
    private async Task<ErrorOr<ImportPlan>> BuildPlanAsync(ExchangeContext exchange, MappingExportDto file, CancellationToken ct)
    {
        if (file is not { Version: 1 or 2, Mappings: not null } || file.Format is not (null or MappingExportDto.FormatName))
            return PlanningErrors.InvalidImportFile;

        var slots = await db.HomeSlots.AsNoTracking()
            .Include(s => s.Course).Include(s => s.CourseGroup)
            .Where(s => s.ProfileId == exchange.HomeProfileId)
            .ToDictionaryAsync(s => s.Id, ct);
        var courses = await db.PartnerCourses.AsNoTracking()
            .Where(c => c.InstitutionId == exchange.PartnerInstitutionId)
            .Select(c => new { c.Id, c.Code, c.Name })
            .ToListAsync(ct);
        var courseById = courses.ToDictionary(c => c.Id);
        var courseIdsByCode = courses.GroupBy(c => c.Code.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(c => c.Id).ToList(), StringComparer.OrdinalIgnoreCase);

        var entries = new List<LearningAgreementEntryUpsertDto>();
        var skipped = new List<ImportSkip>();
        var seen = new HashSet<(int, int?)>();
        foreach (var row in file.Mappings)
        {
            ImportSkip Skip(string reason) => new(row.HomeSlotId, row.HomeSlotLabel, row.PartnerCourse?.Code, reason);

            if (!slots.ContainsKey(row.HomeSlotId)) { skipped.Add(Skip("SlotNotInProfile")); continue; }
            if (!Enum.TryParse<SlotMode>(row.Mode, out var mode) || !Enum.IsDefined(mode)) { skipped.Add(Skip("InvalidMode")); continue; }

            int? courseId = null;
            if (row.PartnerCourse is { } course)
            {
                if (courseById.ContainsKey(course.Id)) courseId = course.Id;
                else if (courseIdsByCode.TryGetValue(course.Code?.Trim() ?? "", out var ids))
                {
                    if (ids.Count > 1) { skipped.Add(Skip("AmbiguousCourse")); continue; }
                    courseId = ids[0];
                }
                else { skipped.Add(Skip("CourseNotFound")); continue; }
            }
            if (!seen.Add((row.HomeSlotId, courseId))) { skipped.Add(Skip("Duplicate")); continue; }
            entries.Add(new LearningAgreementEntryUpsertDto(row.HomeSlotId, mode.ToString(), courseId, row.AwardedEcts));
        }
        var request = new SaveLearningAgreementRequest(entries);

        Error? error = null;
        var editable = await writer.CheckEditableAsync(exchange.ExchangeId, ct);
        if (editable.IsError) error = editable.FirstError;
        else if (await writer.ValidateAsync(request, exchange, ct) is { IsError: true } invalid) error = invalid.FirstError;

        var current = (await content.CaptureAsync(exchange.ExchangeId, ct)).Entries
            .GroupBy(e => (e.HomeSlotId, e.PartnerCourseId))
            .ToDictionary(g => g.Key, g => g.First());
        string SlotLabel(int id) => slots.TryGetValue(id, out var s) ? s.Label : $"#{id}";
        ImportRow Row(LearningAgreementEntryUpsertDto e) => new(
            e.HomeSlotId, SlotLabel(e.HomeSlotId), e.Mode, e.PartnerCourseId,
            e.PartnerCourseId is int id ? courseById[id].Code : null, e.PartnerCourseId is int id2 ? courseById[id2].Name : null, e.AwardedEcts);

        var added = new List<ImportRow>();
        var changed = new List<ImportRow>();
        var unchanged = 0;
        foreach (var entry in entries)
        {
            if (!current.TryGetValue((entry.HomeSlotId, entry.PartnerCourseId), out var existing)) added.Add(Row(entry));
            else if (existing.Mode != entry.Mode || existing.AwardedEcts != entry.AwardedEcts)
                changed.Add(Row(entry) with { PreviousEcts = existing.AwardedEcts, PreviousMode = existing.Mode });
            else unchanged++;
        }
        var removed = current.Values
            .Where(e => !seen.Contains((e.HomeSlotId, e.PartnerCourseId)))
            .Select(e => new ImportRow(e.HomeSlotId, e.HomeSlotLabel, e.Mode, e.PartnerCourseId, e.PartnerCourseCode, e.PartnerCourseName, e.AwardedEcts))
            .ToList();

        var preview = new ImportPreviewResponse(
            error is null, error?.Code, error?.Description, await ContextWarningsAsync(exchange, file, ct),
            added, removed, changed, unchanged, skipped);
        return new ImportPlan(preview, request, error);
    }

    private async Task<List<ImportContextWarning>> ContextWarningsAsync(ExchangeContext exchange, MappingExportDto file, CancellationToken ct)
    {
        var warnings = new List<ImportContextWarning>();
        var partner = await PartnerAsync(exchange, ct);
        var samePartner = file.Institution is not null && (file.Institution.Id == partner.Id
            || (!string.IsNullOrWhiteSpace(partner.ErasmusCode) && string.Equals(file.Institution.ErasmusCode?.Trim(), partner.ErasmusCode.Trim(), StringComparison.OrdinalIgnoreCase)));
        if (!samePartner)
            warnings.Add(new ImportContextWarning("partnerInstitution", Describe(file.Institution?.Name, file.Institution?.ErasmusCode), Describe(partner.Name, partner.ErasmusCode)));

        var home = await HomeAsync(exchange, ct);
        if (file.Home is null || file.Home.ProfileId != home.ProfileId)
            warnings.Add(new ImportContextWarning("homeProfile", file.Home?.ProfileName ?? "?", home.ProfileName));
        return warnings;
    }

    private static string Describe(string? name, string? code) => string.IsNullOrWhiteSpace(code) ? name ?? "?" : $"{name} ({code})";

    private Task<MappingExportInstitution> PartnerAsync(ExchangeContext exchange, CancellationToken ct) =>
        db.Institutions.AsNoTracking()
            .Where(i => i.Id == exchange.PartnerInstitutionId)
            .Select(i => new MappingExportInstitution(i.Id, i.Name, i.ErasmusCode))
            .FirstAsync(ct);

    private Task<MappingExportHomeContext> HomeAsync(ExchangeContext exchange, CancellationToken ct) =>
        db.HomeProfiles.AsNoTracking()
            .Where(p => p.Id == exchange.HomeProfileId)
            .Select(p => new MappingExportHomeContext(p.Id, p.Name, p.Program.Name, p.Program.Institution.Name))
            .FirstAsync(ct);
}
