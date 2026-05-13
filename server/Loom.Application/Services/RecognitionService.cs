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

public class RecognitionService(IAppDbContext db) : IRecognitionService
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
            .ThenInclude(e => e.RecognizedAsCourse);

    private async Task<ErrorOr<int>> ResolveExchangeIdAsync(Guid guid, CancellationToken ct)
    {
        var id = await db.Exchanges.Where(e => e.Guid == guid).Select(e => e.Id).FirstOrDefaultAsync(ct);
        return id == 0 ? Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.") : id;
    }

    public async Task<ErrorOr<RecognitionResponse>> GetOrCreateRecognitionAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default)
    {
        var idResult = await ResolveExchangeIdAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var exchange = await db.Exchanges.Include(e => e.Student).FirstOrDefaultAsync(e => e.Id == exchangeId, ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        var requester = await db.Users.FindAsync([requesterId], ct);
        if (requester is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        var canAccess = exchange.StudentId == requesterId || requester.IsCoordinatorFor(exchange.CoordinatorId);
        if (!canAccess) return Error.Forbidden("ACCESS_DENIED", "Access denied.");

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

        return recognition.ToResponse();
    }

    public async Task<ErrorOr<RecognitionResponse>> SaveRecognitionAsync(Guid exchangeGuid, int studentId, SaveRecognitionRequest request, CancellationToken ct = default)
    {
        var idResult = await ResolveExchangeIdAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var exchange = await db.Exchanges.FindAsync([exchangeId], ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");
        if (exchange.StudentId != studentId) return Error.Forbidden("ACCESS_DENIED", "Access denied.");

        var recognition = await RecognitionsWithIncludes()
            .FirstOrDefaultAsync(r => r.ExchangeId == exchangeId, ct);
        if (recognition is null) return Error.NotFound("RECOGNITION_NOT_FOUND", "Create recognition first.");
        if (recognition.Status == DocumentStatus.Approved) return Error.Conflict("RECOGNITION_LOCKED", "Approved recognition cannot be modified.");

        var entryIds = request.Entries.Select(e => e.LearningAgreementEntryId).ToList();
        var entries = await db.LearningAgreementEntries.Where(e => entryIds.Contains(e.Id)).ToListAsync(ct);
        if (entries.Count != entryIds.Count) return Error.NotFound("ENTRY_NOT_FOUND", "Some learning agreement entries were not found.");

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

        recognition.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return await GetOrCreateRecognitionAsync(exchangeGuid, studentId, ct);
    }

    public async Task<ErrorOr<RecognitionResponse>> UpdateRecognitionStatusAsync(Guid exchangeGuid, int requesterId, UpdateRecognitionStatusRequest request, CancellationToken ct = default)
    {
        var idResult = await ResolveExchangeIdAsync(exchangeGuid, ct);
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

        await db.SaveChangesAsync(ct);

        var statusResponse = recognition.ToResponse();
        if (newStatus == DocumentStatus.Approved)
            await SaveSnapshotAsync(exchangeId, requesterId, SnapshotPhase.Recognition, statusResponse, ct);
        return statusResponse;
    }

    public async Task<ErrorOr<RecognitionResponse>> SetEntryRecognizedAsync(Guid exchangeGuid, int entryId, int coordinatorId, SetEntryRecognizedRequest request, CancellationToken ct = default)
    {
        var idResult = await ResolveExchangeIdAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var exchange = await db.Exchanges.FindAsync([exchangeId], ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        var requester = await db.Users.FindAsync([coordinatorId], ct);
        if (requester is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        if (!requester.IsCoordinatorFor(exchange.CoordinatorId))
            return Error.Forbidden("ACCESS_DENIED", "Only coordinators can mark entries.");

        var recognition = await db.Recognitions
            .FirstOrDefaultAsync(r => r.ExchangeId == exchangeId, ct);
        if (recognition is null) return Error.NotFound("RECOGNITION_NOT_FOUND", "Recognition not found.");

        var entry = await db.RecognitionEntries.FindAsync([entryId], ct);
        if (entry is null || entry.RecognitionId != recognition.Id)
            return Error.NotFound("ENTRY_NOT_FOUND", "Recognition entry not found.");

        entry.IsRecognized = request.IsRecognized;
        await db.SaveChangesAsync(ct);

        return await GetOrCreateRecognitionAsync(exchangeGuid, coordinatorId, ct);
    }

    private async Task SaveSnapshotAsync(int exchangeId, int changedById, SnapshotPhase phase, object payload, CancellationToken ct)
    {
        db.ExchangeSnapshots.Add(new ExchangeSnapshot
        {
            ExchangeId = exchangeId,
            ChangedById = changedById,
            Phase = phase,
            Snapshot = JsonSerializer.Serialize(payload, JsonHelper.DefaultOptions),
        });
        await db.SaveChangesAsync(ct);
    }
}
