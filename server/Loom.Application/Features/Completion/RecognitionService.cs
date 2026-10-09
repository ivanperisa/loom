using ErrorOr;
using Loom.Application.Common;
using Loom.Application.Common.Security;
using Loom.Application.Features.Catalog;
using Loom.Application.Features.Documents;
using Loom.Application.Features.Planning;
using Loom.Application.Interfaces;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Completion;

/// <summary>
/// Recognition after the exchange. "Start final recognition" freezes the LA and table 1 and creates the results
/// (table 2 + mapping scheme) from the latest approved LA version. From then on only the results change: grades here,
/// placement in the mapping scheme. The coordinator approves the results; each approval with changes is a version.
/// </summary>
public sealed class RecognitionService(
    IAppDbContext db, ExchangeAccess access, ICurrentActor actor, ResultsGuard guard, VersionStore versions)
{
    public async Task<ErrorOr<RecognitionResponse>> GetAsync(Guid exchangeGuid, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        return await BuildResponseAsync(context.Value.ExchangeId, ct);
    }

    /// <summary>
    /// Anyone who can edit the exchange may start it, once the LA is approved. It cannot be undone: the LA and table 1
    /// are frozen for good, and the results start as a copy of the latest approved LA version.
    /// </summary>
    public async Task<ErrorOr<RecognitionResponse>> StartAsync(Guid exchangeGuid, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        var exchangeId = context.Value.ExchangeId;

        var recognition = await db.Recognitions.Include(r => r.Entries).FirstOrDefaultAsync(r => r.ExchangeId == exchangeId, ct);
        if (recognition?.IsStarted == true) return CompletionErrors.AlreadyStarted;
        if (!await db.LearningAgreements.AnyAsync(l => l.ExchangeId == exchangeId && l.Status == DocumentStatus.Approved, ct)) return CompletionErrors.LaNotApproved;
        var agreed = await versions.LatestApprovedAsync(exchangeId, DocumentKind.LearningAgreement, ct);
        if (agreed is null || VersionStore.Deserialize<LaVersionPayload>(agreed) is not { } payload) return CompletionErrors.LaNotApproved;

        if (recognition is null)
        {
            recognition = new Recognition { ExchangeId = exchangeId, Status = DocumentStatus.Draft };
            db.Recognitions.Add(recognition);
        }
        recognition.StartedAt = DateTime.UtcNow;
        recognition.StartedById = actor.UserId;
        recognition.LastModifiedById = actor.UserId;

        // One result per course, placed where the LA put it. Exchanges from before this step may already have results; keep them.
        if (recognition.Entries.Count == 0)
        {
            var courseIds = payload.Entries.Where(e => e.PartnerCourseId.HasValue).Select(e => e.PartnerCourseId!.Value).ToList();
            var existing = await db.PartnerCourses.Where(c => courseIds.Contains(c.Id)).Select(c => c.Id).ToHashSetAsync(ct);
            foreach (var course in payload.Entries.Where(e => e.PartnerCourseId is int id && existing.Contains(id)).GroupBy(e => e.PartnerCourseId!.Value))
            {
                recognition.Entries.Add(new RecognitionEntry
                {
                    PartnerCourseId = course.Key,
                    Placements = course
                        .GroupBy(e => e.HomeSlotId)
                        .Select(slot => new MappingSchemeEntry { HomeSlotId = slot.Key, AwardedEcts = slot.Sum(e => e.AwardedEcts ?? 0) })
                        .ToList(),
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return await BuildResponseAsync(exchangeId, ct);
    }

    /// <summary>Table 2: one result per partner course, however many slots the mapping scheme places it in.</summary>
    public async Task<ErrorOr<RecognitionResponse>> SaveGradesAsync(Guid exchangeGuid, SaveGradesRequest request, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        var exchangeId = context.Value.ExchangeId;

        var recognition = await guard.EditableAsync(exchangeId, ct);
        if (recognition.IsError) return recognition.Errors;

        var entries = await db.RecognitionEntries.Where(e => e.RecognitionId == recognition.Value.Id).ToDictionaryAsync(e => e.PartnerCourseId, ct);
        foreach (var grades in request.Entries)
        {
            if (!TryParseStatus(grades.EnrollmentStatus, out var status)) return CompletionErrors.InvalidEnrollmentStatus(grades.EnrollmentStatus);
            var valid = ValidateGrades(grades.OriginalGrade, grades.EctsGrade, grades.HrGrade);
            if (valid.IsError) return valid.Errors;
            if (!entries.TryGetValue(grades.PartnerCourseId, out var entry)) return CompletionErrors.CourseNotInScheme(grades.PartnerCourseId);

            entry.EnrollmentStatus = status;
            entry.OriginalGrade = grades.OriginalGrade.NullIfBlank();
            entry.EctsGrade = grades.EctsGrade.NullIfBlank();
            entry.HrGrade = grades.HrGrade.NullIfBlank();
            entry.ExamDate = grades.ExamDate;
        }

        recognition.Value.LastModifiedById = actor.UserId;
        recognition.Value.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await BuildResponseAsync(exchangeId, ct);
    }

    /// <summary>The assigned coordinator approves the results (a new version when something changed) or reopens them.</summary>
    public async Task<ErrorOr<RecognitionResponse>> SetStatusAsync(Guid exchangeGuid, UpdateRecognitionStatusRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<DocumentStatus>(request.Status, out var status) || !Enum.IsDefined(status)) return CompletionErrors.InvalidStatus;

        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        if (!context.Value.IsAssignedCoordinator || actor.IsGuest) return CompletionErrors.NotAssignedCoordinator;
        var exchangeId = context.Value.ExchangeId;

        var recognition = await db.Recognitions.FirstOrDefaultAsync(r => r.ExchangeId == exchangeId, ct);
        if (recognition is not { IsStarted: true }) return CompletionErrors.NotStarted;
        if (recognition.Status == status) return CompletionErrors.StatusUnchanged(status);

        recognition.Status = status;
        recognition.LastModifiedById = actor.UserId;
        recognition.UpdatedAt = DateTime.UtcNow;
        if (status == DocumentStatus.Approved)
        {
            recognition.SignedAt = DateTime.UtcNow;
            recognition.SignedById = actor.UserId;
            await RecordVersionAsync(exchangeId, recognition.Id, ct);
        }
        else
        {
            recognition.SignedAt = null;
            recognition.SignedById = null;
        }

        await db.SaveChangesAsync(ct);
        return await BuildResponseAsync(exchangeId, ct);
    }

    /// <summary>Notes work before final recognition too, so the header is created on demand.</summary>
    public async Task<ErrorOr<RecognitionResponse>> UpdateMessageAsync(Guid exchangeGuid, string? message, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        var exchangeId = context.Value.ExchangeId;

        var recognition = await db.Recognitions.FirstOrDefaultAsync(r => r.ExchangeId == exchangeId, ct);
        if (recognition is null)
        {
            recognition = new Recognition { ExchangeId = exchangeId, Status = DocumentStatus.Draft };
            db.Recognitions.Add(recognition);
        }
        recognition.Message = message.NullIfBlank();
        recognition.LastModifiedById = actor.UserId;
        recognition.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await BuildResponseAsync(exchangeId, ct);
    }

    public async Task<ErrorOr<List<DocumentVersionResponse>>> ListVersionsAsync(Guid exchangeGuid, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;

        var all = await db.DocumentVersions
            .AsNoTracking()
            .Include(v => v.CreatedBy)
            .Where(v => v.ExchangeId == context.Value.ExchangeId && v.Document == DocumentKind.Recognition)
            .OrderBy(v => v.VersionNo).ThenBy(v => v.CreatedAt)
            .ToListAsync(ct);

        var result = new List<DocumentVersionResponse>();
        var previous = new RecognitionVersionPayload([]);
        foreach (var version in all)
        {
            // Versions carried over from the old snapshots stored labels only; they are listed without a diff.
            var payload = version.SchemaVersion >= 1 ? VersionStore.Deserialize<RecognitionVersionPayload>(version) : null;
            var changes = payload is null ? null : DocumentDiff.Compare(CompletionMappers.DiffRows(previous), CompletionMappers.DiffRows(payload));
            if (payload is not null) previous = payload;
            result.Add(new DocumentVersionResponse(
                version.Id, version.Kind, version.VersionNo, null, version.CreatedAt, version.CreatedBy?.Name,
                payload?.Entries.Count ?? 0, changes));
        }
        result.Reverse();
        return result;
    }

    private async Task RecordVersionAsync(int exchangeId, int recognitionId, CancellationToken ct)
    {
        var entries = await db.MappingSchemeEntries
            .AsNoTracking()
            .Include(e => e.RecognitionEntry).ThenInclude(r => r.PartnerCourse)
            .Include(e => e.HomeSlot).ThenInclude(s => s.Course)
            .Include(e => e.HomeSlot).ThenInclude(s => s.CourseGroup)
            .Where(e => e.RecognitionEntry.RecognitionId == recognitionId)
            .OrderBy(e => e.HomeSlotId).ThenBy(e => e.Id)
            .ToListAsync(ct);
        var payload = new RecognitionVersionPayload(entries.Select(e => e.ToVersionEntry()).ToList());
        var hash = CompletionMappers.Hash(payload);

        var latest = await versions.LatestApprovedAsync(exchangeId, DocumentKind.Recognition, ct);
        if (latest?.ContentHash == hash) return;
        versions.AddApproved(exchangeId, DocumentKind.Recognition, (latest?.VersionNo ?? 0) + 1, VersionStore.Serialize(payload), hash, actor.UserId);
    }

    private async Task<RecognitionResponse> BuildResponseAsync(int exchangeId, CancellationToken ct)
    {
        var laStatus = await db.LearningAgreements
            .Where(l => l.ExchangeId == exchangeId)
            .Select(l => (DocumentStatus?)l.Status)
            .FirstOrDefaultAsync(ct);
        var recognition = await db.Recognitions
            .AsNoTracking()
            .Include(r => r.LastModifiedByUser)
            .Include(r => r.SignedByUser)
            .Include(r => r.StartedByUser)
            .FirstOrDefaultAsync(r => r.ExchangeId == exchangeId, ct);
        var agreed = await versions.LatestApprovedAsync(exchangeId, DocumentKind.LearningAgreement, ct);
        var isStarted = recognition?.IsStarted ?? false;

        return new RecognitionResponse(
            exchangeId,
            recognition?.Status ?? DocumentStatus.Draft,
            recognition?.Message,
            isStarted,
            recognition?.StartedAt,
            recognition?.StartedByUser?.Name,
            CanStart: !isStarted && laStatus == DocumentStatus.Approved && agreed is not null,
            agreed?.VersionNo,
            agreed is null ? [] : await AgreedEntriesAsync(agreed, ct),
            recognition?.UpdatedAt,
            recognition?.LastModifiedByUser?.Name,
            recognition?.SignedAt,
            recognition?.SignedByUser?.Name,
            await versions.ApprovedCountAsync(exchangeId, DocumentKind.Recognition, ct));
    }

    /// <summary>Table 1: the approved LA version's courses, with today's slot and course details (falling back to the stored copy).</summary>
    private async Task<List<AgreedEntryResponse>> AgreedEntriesAsync(DocumentVersion agreed, CancellationToken ct)
    {
        var payload = VersionStore.Deserialize<LaVersionPayload>(agreed);
        if (payload is null) return [];
        var rows = payload.Entries.Where(e => e.PartnerCourseId.HasValue).ToList();

        var slotIds = rows.Select(e => e.HomeSlotId).Distinct().ToList();
        var slots = await db.HomeSlots.AsNoTracking()
            .Include(s => s.SlotType).Include(s => s.Course).Include(s => s.CourseGroup)
            .Where(s => slotIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, ct);
        var courseIds = rows.Select(e => e.PartnerCourseId!.Value).Distinct().ToList();
        var courses = await db.PartnerCourses.AsNoTracking().Where(c => courseIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);

        return rows
            .Where(e => slots.ContainsKey(e.HomeSlotId))
            .Select(e =>
            {
                var slot = slots[e.HomeSlotId];
                var course = courses.GetValueOrDefault(e.PartnerCourseId!.Value);
                return new AgreedEntryResponse(
                    $"{e.HomeSlotId}-{e.PartnerCourseId}",
                    e.HomeSlotId,
                    e.PartnerCourseId!.Value,
                    course?.Code ?? e.PartnerCourseCode ?? string.Empty,
                    course?.Name ?? e.PartnerCourseName ?? string.Empty,
                    course?.NameHr ?? e.PartnerCourseNameHr,
                    course?.Url ?? e.PartnerCourseUrl,
                    course?.Hours(),
                    course?.Ects ?? 0,
                    slot.Course?.IsvuCode,
                    slot.Course?.Name ?? string.Empty,
                    slot.CourseGroup?.IsvuCode,
                    slot.CourseGroup?.Name ?? string.Empty,
                    slot.SlotType.Color,
                    slot.Semester,
                    e.AwardedEcts ?? 0);
            })
            .ToList();
    }

    /// <summary>Blank means "no status yet"; anything else must be Passed or NotPassed.</summary>
    public static bool TryParseStatus(string? value, out EnrollmentStatus? status)
    {
        status = null;
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (!Enum.TryParse<EnrollmentStatus>(value, out var parsed) || !Enum.IsDefined(parsed)) return false;
        status = parsed;
        return true;
    }

    /// <summary>Column limits: original grade 20, ECTS grade 5, Croatian grade 10 characters.</summary>
    public static ErrorOr<Success> ValidateGrades(string? original, string? ects, string? hr)
    {
        if (original?.Trim().Length > 20) return CompletionErrors.GradeTooLong("Original grade", 20);
        if (ects?.Trim().Length > 5) return CompletionErrors.GradeTooLong("ECTS grade", 5);
        if (hr?.Trim().Length > 10) return CompletionErrors.GradeTooLong("Croatian grade", 10);
        return Result.Success;
    }
}
