using System.Text.Json;
using ErrorOr;
using Loom.Application.DTOs.Recognition;
using Loom.Application.Helpers;
using Loom.Application.Interfaces;
using Loom.Application.Interfaces.Services;
using Loom.Application.Mappers;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Services;

public class RecognitionService(IAppDbContext db, IMappingSchemeService mappingSchemeService) : IRecognitionService
{
    private IQueryable<Recognition> RecognitionsWithIncludes() => db.Recognitions
        .Include(r => r.Entries)
            .ThenInclude(e => e.LearningAgreementEntry)
                .ThenInclude(e => e.PartnerCourse)
        .Include(r => r.Entries)
            .ThenInclude(e => e.LearningAgreementEntry)
                .ThenInclude(e => e.HomeSlot)
                    .ThenInclude(s => s.SlotType)
        .Include(r => r.Entries)
            .ThenInclude(e => e.LearningAgreementEntry)
                .ThenInclude(e => e.HomeSlot)
                    .ThenInclude(s => s.Course)
        .Include(r => r.Entries)
            .ThenInclude(e => e.LearningAgreementEntry)
                .ThenInclude(e => e.HomeSlot)
                    .ThenInclude(s => s.CourseGroup)
        .Include(r => r.Entries)
            .ThenInclude(e => e.RecognizedAsCourse)
        .Include(r => r.LastModifiedByUser)
        .Include(r => r.SignedByUser);

    public async Task<ErrorOr<RecognitionResponse>> GetOrCreateRecognitionAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default)
    {
        var idResult = await db.ResolveExchangeIdAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var accessCheck = await db.CheckExchangeAccessAsync(exchangeId, requesterId, ct: ct);
        if (accessCheck.IsError) return accessCheck.Errors;

        var recognition = await RecognitionsWithIncludes()
            .FirstOrDefaultAsync(r => r.ExchangeId == exchangeId, ct);

        if (recognition is null)
        {
            recognition = new Recognition
            {
                ExchangeId = exchangeId,
                Status = DocumentStatus.Draft
            };
            db.Recognitions.Add(recognition);
            await db.SaveChangesAsync(ct);
            recognition.Entries = new List<RecognitionEntry>();
        }

        var existingEntryIds = recognition.Entries.Select(e => e.LearningAgreementEntryId).ToHashSet();
        var allEntryIds = await db.LearningAgreementEntries
            .AsNoTracking()
            .Where(e => e.LearningAgreement.ExchangeId == exchangeId && e.PartnerCourseId != null)
            .Select(e => e.Id)
            .ToListAsync(ct);

        var missingIds = allEntryIds.Where(id => !existingEntryIds.Contains(id)).ToList();
        if (missingIds.Count > 0)
        {
            var newEntries = missingIds.Select(id => new RecognitionEntry
            {
                RecognitionId = recognition.Id,
                LearningAgreementEntryId = id,
            }).ToList();
            db.RecognitionEntries.AddRange(newEntries);
            await db.SaveChangesAsync(ct);

            recognition = await RecognitionsWithIncludes()
                .FirstAsync(r => r.ExchangeId == exchangeId, ct);
        }

        var response = recognition.ToResponse();
        return await OverlayMappingGradesAsync(exchangeId, response, ct);
    }

    private async Task<RecognitionResponse> OverlayMappingGradesAsync(int exchangeId, RecognitionResponse response, CancellationToken ct)
    {
        var byCode = await GetMappingGradesByCodeAsync(exchangeId, ct);
        if (byCode.Count == 0) return response;

        var entries = response.Entries.Select(e =>
            byCode.TryGetValue(e.PartnerCourseCode, out var ms)
                ? e with
                {
                    EnrollmentStatus = ms.EnrollmentStatus?.ToString(),
                    OriginalGrade = ms.OriginalGrade,
                    EctsGrade = ms.EctsGrade,
                    HrGrade = ms.HrGrade,
                    ExamDate = ms.ExamDate,
                }
                : e
        ).ToList();

        return response with { Entries = entries };
    }

    private async Task<Dictionary<string, MappingSchemeEntry>> GetMappingGradesByCodeAsync(int exchangeId, CancellationToken ct)
    {
        var msEntries = await db.MappingSchemeEntries
            .AsNoTracking()
            .Include(e => e.PartnerCourse)
            .Where(e => e.ExchangeId == exchangeId && e.PartnerCourseId != null)
            .ToListAsync(ct);

        return msEntries
            .Where(e => e.PartnerCourse != null)
            .GroupBy(e => e.PartnerCourse!.Code)
            .ToDictionary(g => g.Key, g => g.First());
    }

    private static bool HasGrade(UpsertRecognitionEntryRequest e) =>
        !string.IsNullOrWhiteSpace(e.EnrollmentStatus)
        || !string.IsNullOrWhiteSpace(e.OriginalGrade)
        || !string.IsNullOrWhiteSpace(e.EctsGrade)
        || !string.IsNullOrWhiteSpace(e.HrGrade)
        || e.ExamDate is not null;

    public async Task<ErrorOr<RecognitionResponse>> SaveRecognitionAsync(Guid exchangeGuid, int studentId, SaveRecognitionRequest request, CancellationToken ct = default)
    {
        var idResult = await db.ResolveExchangeIdAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var accessCheck = await db.CheckExchangeAccessAsync(exchangeId, studentId, ct: ct);
        if (accessCheck.IsError) return accessCheck.Errors;
        var requester = accessCheck.Value.Requester;

        var recognition = await RecognitionsWithIncludes()
            .FirstOrDefaultAsync(r => r.ExchangeId == exchangeId, ct);
        if (recognition is null) return Error.NotFound("RECOGNITION_NOT_FOUND", "Create recognition first.");

        var entryIds = request.Entries.Select(e => e.LearningAgreementEntryId).ToList();
        var entries = await db.LearningAgreementEntries.Where(e => entryIds.Contains(e.Id)).ToListAsync(ct);
        if (entries.Count != entryIds.Count) return Error.NotFound("ENTRY_NOT_FOUND", "Some learning agreement entries were not found.");

        recognition.UpdatedAt = DateTime.UtcNow;
        recognition.LastModifiedById = studentId;
        recognition.LastModifiedByUser = requester;

        var mappingExists = await db.MappingSchemeEntries.AnyAsync(e => e.ExchangeId == exchangeId, ct);

        if (mappingExists)
        {
            await ApplyGradesToMappingSchemeAsync(exchangeId, request, entries, ct);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            foreach (var entryReq in request.Entries)
            {
                var existing = recognition.Entries.FirstOrDefault(e => e.LearningAgreementEntryId == entryReq.LearningAgreementEntryId);
                if (existing is null)
                {
                    db.RecognitionEntries.Add(new RecognitionEntry
                    {
                        RecognitionId = recognition.Id,
                        LearningAgreementEntryId = entryReq.LearningAgreementEntryId,
                        EnrollmentStatus = entryReq.EnrollmentStatus,
                        OriginalGrade = entryReq.OriginalGrade,
                        EctsGrade = entryReq.EctsGrade,
                        HrGrade = entryReq.HrGrade,
                        ExamDate = entryReq.ExamDate
                    });
                }
                else
                {
                    existing.EnrollmentStatus = entryReq.EnrollmentStatus;
                    existing.OriginalGrade = entryReq.OriginalGrade;
                    existing.EctsGrade = entryReq.EctsGrade;
                    existing.HrGrade = entryReq.HrGrade;
                    existing.ExamDate = entryReq.ExamDate;
                }
            }

            await db.SaveChangesAsync(ct);

            if (request.Entries.Any(HasGrade))
            {
                await mappingSchemeService.EnsureMappingSchemeInitializedAsync(exchangeId, ct);
                await ApplyGradesToMappingSchemeAsync(exchangeId, request, entries, ct);
            }
        }

        return await GetOrCreateRecognitionAsync(exchangeGuid, studentId, ct);
    }

    private async Task ApplyGradesToMappingSchemeAsync(int exchangeId, SaveRecognitionRequest request, List<LearningAgreementEntry> laEntries, CancellationToken ct)
    {
        var laById = laEntries.ToDictionary(e => e.Id);
        var gradesByPartner = new Dictionary<int, UpsertRecognitionEntryRequest>();
        foreach (var r in request.Entries)
            if (laById.TryGetValue(r.LearningAgreementEntryId, out var la) && la.PartnerCourseId is int pcId)
                gradesByPartner[pcId] = r;

        if (gradesByPartner.Count == 0) return;

        var msEntries = await db.MappingSchemeEntries.Where(e => e.ExchangeId == exchangeId).ToListAsync(ct);
        foreach (var ms in msEntries)
            if (ms.PartnerCourseId is int pid && gradesByPartner.TryGetValue(pid, out var g))
            {
                ms.EnrollmentStatus = MappingSchemeService.ParseStatus(g.EnrollmentStatus);
                ms.OriginalGrade = g.OriginalGrade;
                ms.EctsGrade = g.EctsGrade;
                ms.HrGrade = g.HrGrade;
                ms.ExamDate = g.ExamDate;
            }

        await db.SaveChangesAsync(ct);
    }

    public async Task<ErrorOr<RecognitionResponse>> UpdateRecognitionStatusAsync(Guid exchangeGuid, int requesterId, UpdateRecognitionStatusRequest request, CancellationToken ct = default)
    {
        var idResult = await db.ResolveExchangeIdAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        if (!Enum.TryParse<DocumentStatus>(request.Status, out var newStatus))
            return Error.Validation("INVALID_STATUS", "Invalid recognition status.");

        var exchange = await db.Exchanges.Include(e => e.Student).FirstOrDefaultAsync(e => e.Id == exchangeId, ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        var requester = await db.Users.FindAsync([requesterId], ct);
        if (requester is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        var isStudent = exchange.StudentId == requesterId;
        var isCoordinatorOrAdmin = requester.IsCoordinatorFor(exchange.CoordinatorId);

        if (isStudent && newStatus != DocumentStatus.Submitted && newStatus != DocumentStatus.Draft)
            return Error.Forbidden("FORBIDDEN", "Students can only submit or revert recognition to draft.");
        if (!isStudent && !isCoordinatorOrAdmin)
            return Error.Forbidden("ACCESS_DENIED", "Access denied.");

        var recognition = await RecognitionsWithIncludes()
            .FirstOrDefaultAsync(r => r.ExchangeId == exchangeId, ct);
        if (recognition is null) return Error.NotFound("RECOGNITION_NOT_FOUND", "Recognition not found.");

        if (isStudent && newStatus == DocumentStatus.Draft && recognition.Status == DocumentStatus.Approved)
            return Error.Forbidden("FORBIDDEN", "Cannot revert an approved recognition to draft.");

        recognition.Status = newStatus;
        recognition.UpdatedAt = DateTime.UtcNow;
        recognition.LastModifiedById = requesterId;
        recognition.LastModifiedByUser = requester;

        if (newStatus == DocumentStatus.Approved)
        {
            recognition.SignedAt = DateTime.UtcNow;
            recognition.SignedById = requesterId;
            recognition.SignedByUser = requester;
        }
        else if (newStatus == DocumentStatus.Draft)
        {
            recognition.SignedAt = null;
            recognition.SignedById = null;
            recognition.SignedByUser = null;
        }

        if (newStatus == DocumentStatus.Approved)
        {
            var recWithEntries = await RecognitionsWithIncludes()
                .FirstOrDefaultAsync(r => r.ExchangeId == exchangeId, ct);

            var byCode = await GetMappingGradesByCodeAsync(exchangeId, ct);

            var snapshotData = new RecognitionSnapshotData(
                recWithEntries?.Entries.Select(e =>
                {
                    var code = e.LearningAgreementEntry.PartnerCourse?.Code;
                    var ms = code is not null && byCode.TryGetValue(code, out var m) ? m : null;
                    return new RecognitionSnapshotEntry(
                        e.LearningAgreementEntry.HomeSlot.Course?.Name
                            ?? e.LearningAgreementEntry.HomeSlot.CourseGroup?.Name
                            ?? $"Slot {e.LearningAgreementEntry.HomeSlotId}",
                        code,
                        e.LearningAgreementEntry.PartnerCourse?.Name,
                        ms?.EnrollmentStatus?.ToString() ?? e.EnrollmentStatus,
                        ms?.OriginalGrade ?? e.OriginalGrade,
                        ms?.EctsGrade ?? e.EctsGrade,
                        ms?.HrGrade ?? e.HrGrade,
                        ms?.ExamDate ?? e.ExamDate,
                        ms?.IsRecognized ?? e.IsRecognized,
                        e.RecognizedAsCourse?.Name
                    );
                }).ToList() ?? []);

            db.ExchangeSnapshots.Add(new ExchangeSnapshot
            {
                ExchangeId = exchangeId,
                ChangedById = requesterId,
                Phase = SnapshotPhase.Recognition,
                Type = SnapshotType.Auto,
                Snapshot = JsonSerializer.Serialize(snapshotData, JsonHelper.DefaultOptions),
            });
        }

        await db.SaveChangesAsync(ct);
        return recognition.ToResponse();
    }

    public async Task<ErrorOr<RecognitionResponse>> UpdateRecognitionMessageAsync(Guid exchangeGuid, int requesterId, string? message, CancellationToken ct = default)
    {
        var idResult = await db.ResolveExchangeIdAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var accessCheck = await db.CheckExchangeAccessAsync(exchangeId, requesterId, ct: ct);
        if (accessCheck.IsError) return accessCheck.Errors;
        var requester = accessCheck.Value.Requester;

        var recognition = await db.Recognitions.FirstOrDefaultAsync(r => r.ExchangeId == exchangeId, ct);
        if (recognition is null) return Error.NotFound("RECOGNITION_NOT_FOUND", "Recognition not found.");

        recognition.Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        recognition.UpdatedAt = DateTime.UtcNow;
        recognition.LastModifiedById = requesterId;
        recognition.LastModifiedByUser = requester;
        await db.SaveChangesAsync(ct);

        return await GetOrCreateRecognitionAsync(exchangeGuid, requesterId, ct);
    }

    public async Task<ErrorOr<List<RecognitionSnapshotSummary>>> GetRecognitionHistoryAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default)
    {
        var idResult = await db.ResolveExchangeIdAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var accessCheck = await db.CheckExchangeAccessAsync(exchangeId, requesterId, ct: ct);
        if (accessCheck.IsError) return accessCheck.Errors;

        var snapshots = await db.ExchangeSnapshots
            .AsNoTracking()
            .Include(s => s.ChangedBy)
            .Where(s => s.ExchangeId == exchangeId && s.Phase == SnapshotPhase.Recognition && s.Type == SnapshotType.Auto)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(ct);

        var result = new List<RecognitionSnapshotSummary>();
        RecognitionSnapshotData? previous = null;

        foreach (var snapshot in snapshots)
        {
            var data = JsonSerializer.Deserialize<RecognitionSnapshotData>(snapshot.Snapshot, JsonHelper.DefaultOptions);
            if (data is null) continue;

            var diff = previous is not null ? ComputeRecognitionDiff(data, previous) : null;
            result.Add(new RecognitionSnapshotSummary(snapshot.Id, snapshot.CreatedAt, snapshot.ChangedBy.Name, data.Entries.Count, diff));
            previous = data;
        }

        result.Reverse();
        return result;
    }

    private static RecognitionSnapshotDiff ComputeRecognitionDiff(RecognitionSnapshotData current, RecognitionSnapshotData previous)
    {
        static string Key(RecognitionSnapshotEntry e) => $"{e.HomeSlotLabel}|{e.PartnerCourseCode}";

        var prevByKey = previous.Entries.ToDictionary(Key);
        var currByKey = current.Entries.ToDictionary(Key);

        var added = currByKey.Where(kv => !prevByKey.ContainsKey(kv.Key)).Select(kv => kv.Value).ToList();
        var removed = prevByKey.Where(kv => !currByKey.ContainsKey(kv.Key)).Select(kv => kv.Value).ToList();
        return new RecognitionSnapshotDiff(added, removed, []);
    }
}
