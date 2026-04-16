using ErrorOr;
using Loom.Application.DTOs.Exchange;
using Loom.Application.DTOs.Recognition;
using Loom.Application.Interfaces;
using Loom.Application.Interfaces.Services;
using Loom.Application.Mappers;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Services;

public class RecognitionService(IAppDbContext db) : IRecognitionService
{
    public async Task<ErrorOr<RecognitionResponse>> GetOrCreateRecognitionAsync(Guid exchangeId, Guid requesterId, CancellationToken ct = default)
    {
        var exchange = await db.Exchanges.Include(e => e.Student).FirstOrDefaultAsync(e => e.Id == exchangeId, ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        var requester = await db.Users.FindAsync([requesterId], ct);
        if (requester is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        var canAccess = exchange.StudentId == requesterId
            || exchange.Student.CoordinatorId == requesterId
            || requester.Role == UserRole.Admin;
        if (!canAccess) return Error.Forbidden("ACCESS_DENIED", "Access denied.");

        var recognition = await db.Recognitions
            .Include(r => r.Entries)
                .ThenInclude(e => e.SlotMapping)
                    .ThenInclude(m => m.ForeignCourse)
            .Include(r => r.Entries)
                .ThenInclude(e => e.SlotMapping)
                    .ThenInclude(m => m.SlotState)
                        .ThenInclude(s => s.CourseSlot)
            .FirstOrDefaultAsync(r => r.ExchangeId == exchangeId, ct);

        if (recognition is null)
        {
            recognition = new Recognition
            {
                ExchangeId = exchangeId,
                Status = RecognitionStatus.Draft
            };
            db.Recognitions.Add(recognition);
            await db.SaveChangesAsync(ct);
            recognition.Entries = new List<RecognitionEntry>();
        }

        return recognition.ToResponse();
    }

    public async Task<ErrorOr<RecognitionResponse>> UpsertEntryAsync(Guid exchangeId, Guid studentId, UpsertRecognitionEntryRequest request, CancellationToken ct = default)
    {
        var exchange = await db.Exchanges.FindAsync([exchangeId], ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");
        if (exchange.StudentId != studentId) return Error.Forbidden("ACCESS_DENIED", "Access denied.");

        var recognition = await db.Recognitions
            .Include(r => r.Entries)
                .ThenInclude(e => e.SlotMapping)
                    .ThenInclude(m => m.ForeignCourse)
            .Include(r => r.Entries)
                .ThenInclude(e => e.SlotMapping)
                    .ThenInclude(m => m.SlotState)
                        .ThenInclude(s => s.CourseSlot)
            .FirstOrDefaultAsync(r => r.ExchangeId == exchangeId, ct);
        if (recognition is null) return Error.NotFound("RECOGNITION_NOT_FOUND", "Create recognition first.");
        if (recognition.Status == RecognitionStatus.Approved) return Error.Conflict("RECOGNITION_LOCKED", "Approved recognition cannot be modified.");

        var mapping = await db.SlotMappings.FindAsync([request.SlotMappingId], ct);
        if (mapping is null) return Error.NotFound("MAPPING_NOT_FOUND", "Slot mapping not found.");

        var existing = recognition.Entries.FirstOrDefault(e => e.SlotMappingId == request.SlotMappingId);
        if (existing is null)
        {
            var entry = new RecognitionEntry
            {
                RecognitionId = recognition.Id,
                SlotMappingId = request.SlotMappingId,
                EnrollmentStatus = request.EnrollmentStatus,
                OriginalGrade = request.OriginalGrade,
                EctsGrade = request.EctsGrade,
                HrGrade = request.HrGrade,
                ExamDate = request.ExamDate
            };
            db.RecognitionEntries.Add(entry);
        }
        else
        {
            existing.EnrollmentStatus = request.EnrollmentStatus;
            existing.OriginalGrade = request.OriginalGrade;
            existing.EctsGrade = request.EctsGrade;
            existing.HrGrade = request.HrGrade;
            existing.ExamDate = request.ExamDate;
        }

        recognition.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return await GetOrCreateRecognitionAsync(exchangeId, studentId, ct);
    }

    public async Task<ErrorOr<RecognitionResponse>> UpdateRecognitionStatusAsync(Guid exchangeId, Guid requesterId, UpdateExchangeStatusRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<RecognitionStatus>(request.Status, out var newStatus))
            return Error.Validation("INVALID_STATUS", "Invalid recognition status.");

        var exchange = await db.Exchanges.Include(e => e.Student).FirstOrDefaultAsync(e => e.Id == exchangeId, ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        var requester = await db.Users.FindAsync([requesterId], ct);
        if (requester is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        var isStudent = exchange.StudentId == requesterId;
        var isCoordinatorOrAdmin = exchange.Student.CoordinatorId == requesterId || requester.Role == UserRole.Admin;

        if (isStudent && newStatus != RecognitionStatus.Submitted)
            return Error.Forbidden("FORBIDDEN", "Students can only submit recognition.");
        if (!isStudent && !isCoordinatorOrAdmin)
            return Error.Forbidden("ACCESS_DENIED", "Access denied.");

        var recognition = await db.Recognitions
            .Include(r => r.Entries)
                .ThenInclude(e => e.SlotMapping)
                    .ThenInclude(m => m.ForeignCourse)
            .Include(r => r.Entries)
                .ThenInclude(e => e.SlotMapping)
                    .ThenInclude(m => m.SlotState)
                        .ThenInclude(s => s.CourseSlot)
            .FirstOrDefaultAsync(r => r.ExchangeId == exchangeId, ct);
        if (recognition is null) return Error.NotFound("RECOGNITION_NOT_FOUND", "Recognition not found.");

        recognition.Status = newStatus;
        recognition.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return recognition.ToResponse();
    }
}
