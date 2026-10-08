using System.Globalization;
using Loom.Application.Features.Documents;
using Loom.Application.Interfaces;
using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Planning;

/// <summary>The learning agreement's content as one value: for versions, backups, "did anything change?" and diffs.</summary>
public sealed class LaContent(IAppDbContext db)
{
    /// <summary>The live components (not the ones taken out), with display copies.</summary>
    public async Task<LaVersionPayload> CaptureAsync(int exchangeId, CancellationToken ct)
    {
        var entries = await db.LearningAgreementEntries
            .AsNoTracking()
            .Where(e => e.LearningAgreement.ExchangeId == exchangeId && !e.IsDeleted)
            .Include(e => e.PartnerCourse)
            .Include(e => e.HomeSlot).ThenInclude(s => s.Course)
            .Include(e => e.HomeSlot).ThenInclude(s => s.CourseGroup)
            .OrderBy(e => e.HomeSlotId).ThenBy(e => e.Id)
            .ToListAsync(ct);
        return new LaVersionPayload(entries.Select(ToVersionEntry).ToList());
    }

    public static LaVersionEntry ToVersionEntry(LearningAgreementEntry e) => new(
        e.HomeSlotId, e.HomeSlot.Label, e.HomeSlot.Semester, e.HomeSlot.Ects, e.Mode.ToString(),
        e.PartnerCourseId, e.PartnerCourse?.Code, e.PartnerCourse?.Name, e.AwardedEcts,
        e.PartnerCourse?.NameHr, e.PartnerCourse?.Url);

    /// <summary>Rows "slot|mode|course|ects" (invariant culture). The migration computes the same hash in SQL for carried-over versions.</summary>
    public static string Hash(LaVersionPayload payload) =>
        VersionStore.Hash(payload.Entries.Select(e => Row(e.HomeSlotId, e.Mode, e.PartnerCourseId, e.AwardedEcts)));

    public static string Hash(SaveLearningAgreementRequest request) =>
        VersionStore.Hash(request.Entries.Select(e => Row(e.HomeSlotId, e.Mode, e.PartnerCourseId, e.AwardedEcts)));

    private static string Row(int slotId, string mode, int? courseId, decimal? ects) =>
        string.Create(CultureInfo.InvariantCulture, $"{slotId}|{mode}|{courseId}|{ects:0.0}");

    public static SaveLearningAgreementRequest ToRequest(LaVersionPayload payload) =>
        new(payload.Entries.Select(e => new LearningAgreementEntryUpsertDto(e.HomeSlotId, e.Mode, e.PartnerCourseId, e.AwardedEcts)).ToList());

    public static IEnumerable<DiffRow> DiffRows(LaVersionPayload payload) =>
        payload.Entries.Select(e => new DiffRow(
            e.HomeSlotId, e.HomeSlotLabel, e.PartnerCourseId, e.PartnerCourseCode, e.PartnerCourseName,
            new Dictionary<string, string?> { ["mode"] = e.Mode, ["ects"] = e.AwardedEcts?.ToString("0.0", CultureInfo.InvariantCulture) }));
}
