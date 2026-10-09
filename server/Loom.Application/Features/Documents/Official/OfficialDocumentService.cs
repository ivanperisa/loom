using ErrorOr;
using Loom.Application.Features.Completion;
using Loom.Application.Features.Exchanges;
using Loom.Application.Features.Planning;
using Loom.Application.Interfaces;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Documents.Official;

public sealed record OfficialDocumentFile(byte[] Content, string FileName);

/// <summary>
/// The official xlsx, generated on the server from what is stored (never from unsaved screen state): the LA as of its
/// latest approved version, table 1, and (after final recognition started) table 2 and the mapping scheme.
/// </summary>
public sealed class OfficialDocumentService(
    IAppDbContext db,
    ExchangeService exchanges,
    LearningAgreementService learningAgreements,
    RecognitionService recognitions,
    MappingSchemeService mappingSchemes)
{
    public async Task<ErrorOr<OfficialDocumentFile>> BuildAsync(Guid exchangeGuid, string? lang, CancellationToken ct)
    {
        var exchange = await exchanges.GetAsync(exchangeGuid, ct);   // also the access check
        if (exchange.IsError) return exchange.Errors;
        var la = await learningAgreements.GetAsync(exchangeGuid, ct);
        if (la.IsError) return la.Errors;
        var recognition = await recognitions.GetAsync(exchangeGuid, ct);
        if (recognition.IsError) return recognition.Errors;

        ResultsSheet? results = null;
        if (recognition.Value.IsStarted)
        {
            var scheme = await mappingSchemes.GetAsync(exchangeGuid, ct);
            if (scheme.IsError) return scheme.Errors;
            results = new ResultsSheet(scheme.Value.Entries.Select(ToLine).ToList(), SchemeGrid(scheme.Value, la.Value));
        }

        var text = new OfficialText(lang == "en" ? "en" : "hr");
        var data = new OfficialDocumentData(
            exchange.Value,
            la.Value.Slots,
            await LaSheetAsync(exchange.Value.Id, la.Value, text, ct),
            recognition.Value.Agreed.Select(ToLine).ToList(),
            results,
            Signatures(la.Value, recognition.Value));

        var content = new OfficialWorkbook(data, text).Build();
        var student = string.Join('_', exchange.Value.StudentName.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var fileName = $"{(text.IsEnglish ? "Exchange" : "Razmjena")}_{student}_{exchange.Value.AcademicYear.Replace('/', '-')}.xlsx";
        return new OfficialDocumentFile(content, fileName);
    }

    /// <summary>
    /// Components as of the latest approved version v: live in v (added ≤ v, not removed by v), plus those removed in an
    /// earlier amendment, struck through. Before the first approval: the draft.
    /// </summary>
    private async Task<LaSheet> LaSheetAsync(int exchangeId, LearningAgreementResponse la, OfficialText text, CancellationToken ct)
    {
        int? version = la.SignedCount > 0 ? la.SignedCount : null;
        var entries = await db.LearningAgreementEntries
            .AsNoTracking()
            .Include(e => e.PartnerCourse)
            .Include(e => e.HomeSlot).ThenInclude(s => s.Course)
            .Include(e => e.HomeSlot).ThenInclude(s => s.CourseGroup)
            .Where(e => e.LearningAgreement.ExchangeId == exchangeId)
            .OrderBy(e => e.Id)
            .ToListAsync(ct);

        bool Live(LearningAgreementEntry e) => version is int v
            ? e.AddedInVersion <= v && (e.RemovedInVersion is null || e.RemovedInVersion > v)
            : !e.IsDeleted;
        bool Removed(LearningAgreementEntry e) => version is int v && e.RemovedInVersion <= v;

        var grid = new Dictionary<int, GridSlot>();
        foreach (var entry in entries.Where(e => Live(e) || Removed(e)))
        {
            if (!grid.TryGetValue(entry.HomeSlotId, out var slot))
                grid[entry.HomeSlotId] = slot = new GridSlot(null, []);
            if (Live(entry)) grid[entry.HomeSlotId] = slot = slot with { Mode = entry.Mode.ToString() };
            if (entry.PartnerCourse is not { } course) continue;

            var removed = Removed(entry);
            var note = removed
                ? $"−{text.Amendment(entry.RemovedInVersion)}"
                : text.Amendment(entry.AddedInVersion);
            slot.Lines.Add(new GridLine(course.Code, course.Name, course.NameHr, entry.AwardedEcts ?? 0, removed, note));
        }

        var changes = new List<ChangeLine>();
        foreach (var entry in entries.Where(e => e.PartnerCourse is not null))
        {
            if (entry.AddedInVersion is int added and > 1 && (version is null || added <= version))
                changes.Add(new ChangeLine(text.Amendment(added)!, true, entry.PartnerCourse!.Code, entry.PartnerCourse.Name, entry.AwardedEcts, entry.HomeSlot.Label));
            if (Removed(entry))
                changes.Add(new ChangeLine(text.Amendment(entry.RemovedInVersion)!, false, entry.PartnerCourse!.Code, entry.PartnerCourse.Name, entry.AwardedEcts, entry.HomeSlot.Label));
        }
        return new LaSheet(version, grid, changes.OrderBy(c => c.Amendment.Length).ThenBy(c => c.Amendment).ThenBy(c => c.Added).ToList());
    }

    /// <summary>The mapping scheme on the same grid; courses marked not passed are struck through. Slot modes come from the LA.</summary>
    private static Dictionary<int, GridSlot> SchemeGrid(MappingSchemeResponse scheme, LearningAgreementResponse la)
    {
        var grid = la.Entries
            .Where(e => !e.IsDeleted)
            .GroupBy(e => e.HomeSlotId)
            .ToDictionary(g => g.Key, g => new GridSlot(g.First().Mode.ToString(), []));
        foreach (var entry in scheme.Entries)
        {
            if (!grid.TryGetValue(entry.HomeSlotId, out var slot) || slot.Mode != nameof(SlotMode.AtExchange))
                grid[entry.HomeSlotId] = slot = new GridSlot(nameof(SlotMode.AtExchange), slot?.Lines ?? []);
            slot.Lines.Add(new GridLine(entry.PartnerCourseCode, entry.PartnerCourseName, entry.PartnerCourseNameHr, entry.AwardedEcts,
                entry.EnrollmentStatus == EnrollmentStatus.NotPassed, null));
        }
        return grid;
    }

    private static List<SignatureLine> Signatures(LearningAgreementResponse la, RecognitionResponse recognition) =>
    [
        new("la", la.SignedCount > 0 ? la.SignedCount : null, la.Status.ToString(), la.SignedByName, la.SignedAt),
        new("recognition", recognition.ApprovedVersionCount > 0 ? recognition.ApprovedVersionCount : null,
            recognition.IsStarted ? recognition.Status.ToString() : "notStarted", recognition.SignedByName, recognition.SignedAt),
    ];

    private static ResultLine ToLine(AgreedEntryResponse e) => new(
        e.PartnerCourseId, e.PartnerCourseCode, e.PartnerCourseName, e.PartnerCourseNameHr, e.PartnerCourseHours, e.PartnerCourseEcts,
        e.HomeSlotCourseIsvuCode, e.HomeSlotCourseName, e.HomeSlotCourseGroupIsvuCode, e.HomeSlotCourseGroupName, e.HomeSlotColor,
        e.HomeSlotSemester, e.AwardedEcts);

    private static ResultLine ToLine(MappingSchemeEntryResponse e) => new(
        e.PartnerCourseId ?? 0, e.PartnerCourseCode, e.PartnerCourseName, e.PartnerCourseNameHr, e.PartnerCourseHours, e.PartnerCourseEcts,
        e.HomeSlotCourseIsvuCode, e.HomeSlotCourseName, e.HomeSlotCourseGroupIsvuCode, e.HomeSlotCourseGroupName, e.HomeSlotColor,
        e.HomeSlotSemester, e.AwardedEcts, e.EnrollmentStatus?.ToString(), e.OriginalGrade, e.EctsGrade, e.HrGrade, e.ExamDate);
}
