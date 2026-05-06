using System.Text.Json;
using ErrorOr;
using Loom.Application.Helpers;
using Loom.Application.Mappers;
using Loom.Domain.Entities;
using Loom.Application.DTOs.CourseSlot;
using Loom.Application.DTOs.Exchange;
using Loom.Application.Interfaces;
using Loom.Application.Interfaces.Services;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Services;

public class ExchangeService(IAppDbContext db) : IExchangeService
{
    private IQueryable<Exchange> ExchangeWithIncludes() => db.Exchanges
        .AsNoTracking()
        .Include(e => e.Student)
        .Include(e => e.Coordinator)
        .Include(e => e.StudyProfile).ThenInclude(sp => sp.StudyProgram).ThenInclude(p => p.Institution)
        .Include(e => e.ForeignProgram).ThenInclude(p => p.Institution);

    private async Task<ErrorOr<(Exchange exchange, User requester)>> CheckExchangeAccessAsync(
        Guid exchangeId, Guid requesterId, bool requireStudentInclude = false, CancellationToken ct = default)
    {
        var query = db.Exchanges.AsQueryable();
        if (requireStudentInclude) query = query.Include(e => e.Student);

        var exchange = await query.FirstOrDefaultAsync(e => e.Id == exchangeId, ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        var requester = await db.Users.FindAsync([requesterId], ct);
        if (requester is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        if (exchange.StudentId != requesterId && !requester.IsCoordinatorFor(exchange.CoordinatorId))
            return Error.Forbidden("ACCESS_DENIED", "Access denied.");

        return (exchange, requester);
    }

    private static bool IsExchangeLocked(Exchange exchange) =>
        exchange.Status is ExchangeStatus.Approved or ExchangeStatus.Submitted;

    private async Task<LearningAgreement> GetOrCreateLaAsync(Guid exchangeId, CancellationToken ct)
    {
        var la = await db.LearningAgreements.FirstOrDefaultAsync(la => la.ExchangeId == exchangeId, ct);
        if (la is not null) return la;
        la = new LearningAgreement { ExchangeId = exchangeId };
        db.LearningAgreements.Add(la);
        await db.SaveChangesAsync(ct);
        la.Entries = new List<LearningAgreementEntry>();
        return la;
    }

    private IQueryable<LearningAgreement> LaWithEntries() => db.LearningAgreements
        .Include(la => la.Entries)
            .ThenInclude(e => e.ForeignCourse);

    public async Task<ErrorOr<ExchangeResponse>> CreateExchangeAsync(Guid studentId, CreateExchangeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.AcademicYear))
            return Error.Validation("INVALID_ACADEMIC_YEAR", "Academic year is required.");
        if (!Enum.TryParse<ExchangeSemester>(request.SemesterType, out var semesterType))
            return Error.Validation("INVALID_SEMESTER_TYPE", "Invalid semester type.");
        if (request.StudySemester < 1 || request.StudySemester > 4)
            return Error.Validation("INVALID_STUDY_SEMESTER", "Study semester must be between 1 and 4.");

        var student = await db.Users.FindAsync([studentId], ct);
        if (student is null) return Error.NotFound("USER_NOT_FOUND", "Student not found.");

        var studyProfile = await db.StudyProfiles.FindAsync([request.StudyProfileId], ct);
        if (studyProfile is null) return Error.NotFound("STUDY_PROFILE_NOT_FOUND", "Study profile not found.");

        var foreignProgram = await db.ForeignPrograms
            .AsNoTracking()
            .Include(p => p.Institution)
            .FirstOrDefaultAsync(p => p.Id == request.ForeignProgramId, ct);
        if (foreignProgram is null) return Error.NotFound("FOREIGN_PROGRAM_NOT_FOUND", "Foreign program not found.");

        var exchange = new Exchange
        {
            StudentId = studentId,
            CoordinatorId = student.CoordinatorId,
            StudyProfileId = request.StudyProfileId,
            ForeignProgramId = request.ForeignProgramId,
            AcademicYear = request.AcademicYear,
            SemesterType = semesterType,
            StudySemester = request.StudySemester,
            Status = ExchangeStatus.Draft
        };
        db.Exchanges.Add(exchange);
        await db.SaveChangesAsync(ct);

        db.LearningAgreements.Add(new LearningAgreement { ExchangeId = exchange.Id });
        await db.SaveChangesAsync(ct);

        var saved = await ExchangeWithIncludes()
            .FirstOrDefaultAsync(e => e.Id == exchange.Id, ct)
            ?? throw new InvalidOperationException();
        return saved.ToResponse();
    }

    public async Task<ErrorOr<ExchangeResponse>> GetExchangeAsync(Guid exchangeId, Guid requesterId, CancellationToken ct = default)
    {
        var exchange = await ExchangeWithIncludes().FirstOrDefaultAsync(e => e.Id == exchangeId, ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        var requester = await db.Users.FindAsync([requesterId], ct);
        if (requester is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        if (exchange.StudentId != requesterId && !requester.IsCoordinatorFor(exchange.CoordinatorId))
            return Error.Forbidden("ACCESS_DENIED", "Access denied.");

        return exchange.ToResponse();
    }

    public async Task<ErrorOr<List<ExchangeSummaryResponse>>> GetMyExchangesAsync(Guid studentId, CancellationToken ct = default)
    {
        var exchanges = await db.Exchanges
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.ForeignProgram).ThenInclude(p => p.Institution)
            .Include(e => e.StudyProfile).ThenInclude(sp => sp.StudyProgram).ThenInclude(p => p.Institution)
            .Include(e => e.Recognition)
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);
        return exchanges.Select(e => e.ToSummaryResponse()).ToList();
    }

    public async Task<ErrorOr<ExchangeResponse>> UpdateExchangeStatusAsync(Guid exchangeId, Guid requesterId, UpdateExchangeStatusRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<ExchangeStatus>(request.Status, out var newStatus))
            return Error.Validation("INVALID_STATUS", "Invalid exchange status.");

        var exchange = await db.Exchanges.Include(e => e.Student).FirstOrDefaultAsync(e => e.Id == exchangeId, ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        var requester = await db.Users.FindAsync([requesterId], ct);
        if (requester is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        var isStudent = exchange.StudentId == requesterId;
        var isCoordinatorOrAdmin = requester.IsCoordinatorFor(exchange.CoordinatorId);

        if (!isStudent && !isCoordinatorOrAdmin)
            return Error.Forbidden("ACCESS_DENIED", "Access denied.");
        if (isStudent && newStatus != ExchangeStatus.Submitted && newStatus != ExchangeStatus.Draft)
            return Error.Forbidden("FORBIDDEN", "Students can only submit or revert to draft.");
        if (isStudent && newStatus == ExchangeStatus.Draft && exchange.Status == ExchangeStatus.Approved)
            return Error.Forbidden("FORBIDDEN", "Cannot revert an approved exchange to draft.");

        if (newStatus == ExchangeStatus.Approved)
        {
            var hasApproved = await db.Exchanges
                .AnyAsync(e => e.StudentId == exchange.StudentId && e.Id != exchangeId && e.Status == ExchangeStatus.Approved, ct);
            if (hasApproved)
                return Error.Conflict("ALREADY_HAS_APPROVED", "Student already has an approved exchange.");
        }

        exchange.Status = newStatus;
        exchange.UpdatedAt = DateTime.UtcNow;

        if (isCoordinatorOrAdmin && request.Message is not null)
            exchange.CoordinatorMessage = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim();
        if (newStatus == ExchangeStatus.Approved)
            exchange.CoordinatorMessage = null;

        if (newStatus == ExchangeStatus.Approved)
        {
            var la = await LaWithEntries().AsNoTracking().FirstOrDefaultAsync(la => la.ExchangeId == exchangeId, ct);
            var snapshotData = new LearningAgreementSnapshotData(
                la?.Entries.Select(e => e.ToResponse()).ToList() ?? []);
            db.ExchangeSnapshots.Add(new ExchangeSnapshot
            {
                ExchangeId = exchangeId,
                ChangedById = requesterId,
                Phase = SnapshotPhase.LearningAgreement,
                Snapshot = JsonSerializer.Serialize(snapshotData, JsonHelper.DefaultOptions)
            });
        }

        await db.SaveChangesAsync(ct);

        var saved = await ExchangeWithIncludes().FirstOrDefaultAsync(e => e.Id == exchangeId, ct)
            ?? throw new InvalidOperationException();
        return saved.ToResponse();
    }

    public async Task<ErrorOr<LearningAgreementResponse>> GetLearningAgreementAsync(Guid exchangeId, Guid requesterId, CancellationToken ct = default)
    {
        var accessCheck = await CheckExchangeAccessAsync(exchangeId, requesterId, true, ct);
        if (accessCheck.IsError) return accessCheck.Errors;
        var (exchange, _) = accessCheck.Value;

        var slots = await db.CourseSlots
            .AsNoTracking()
            .Include(s => s.Category)
            .Where(s => s.StudyProfileId == exchange.StudyProfileId)
            .OrderBy(s => s.Semester).ThenBy(s => s.SlotPosition)
            .ToListAsync(ct);

        var la = await LaWithEntries().AsNoTracking().FirstOrDefaultAsync(la => la.ExchangeId == exchangeId, ct);

        return new LearningAgreementResponse(
            exchangeId,
            exchange.Status.ToString(),
            slots.Select(s => s.ToResponse()).ToList(),
            la?.Entries.Select(e => e.ToResponse()).ToList() ?? []
        );
    }

    public async Task<ErrorOr<LearningAgreementResponse>> SaveLearningAgreementAsync(Guid exchangeId, Guid requesterId, SaveLearningAgreementRequest request, CancellationToken ct = default)
    {
        var accessCheck = await CheckExchangeAccessAsync(exchangeId, requesterId, true, ct);
        if (accessCheck.IsError) return accessCheck.Errors;
        var (exchange, _) = accessCheck.Value;
        if (IsExchangeLocked(exchange))
            return Error.Conflict("EXCHANGE_LOCKED", "Exchange cannot be modified in current status.");

        var validationError = ValidateEntryRequest(request);
        if (validationError is not null) return validationError.Value;

        var profileSlotIds = await db.CourseSlots
            .AsNoTracking()
            .Where(s => s.StudyProfileId == exchange.StudyProfileId)
            .Select(s => s.Id)
            .ToHashSetAsync(ct);

        foreach (var dto in request.Entries)
        {
            if (!profileSlotIds.Contains(dto.CourseSlotId))
                return Error.Validation("SLOT_NOT_IN_PROFILE", $"Course slot {dto.CourseSlotId} does not belong to this study profile.");
        }

        var ectsError = await ValidateForeignCourseEctsAsync(request, ct);
        if (ectsError is not null) return ectsError.Value;

        var la = await GetOrCreateLaAsync(exchangeId, ct);
        await DeleteExistingEntriesAsync(la.Id, ct);
        await CreateNewEntriesAsync(la.Id, request, ct);

        await db.SaveChangesAsync(ct);
        return await GetLearningAgreementAsync(exchangeId, requesterId, ct);
    }

    private static ErrorOr<LearningAgreementResponse>? ValidateEntryRequest(SaveLearningAgreementRequest request)
    {
        foreach (var dto in request.Entries)
        {
            if (!Enum.TryParse<SlotMode>(dto.Mode, out var mode))
                return Error.Validation("INVALID_MODE", $"Invalid slot mode: {dto.Mode}.");
            if (mode == SlotMode.AtExchange && dto.ForeignCourseId is not null && dto.AwardedEcts <= 0)
                return Error.Validation("INVALID_ECTS", "Awarded ECTS must be greater than 0.");
            if (mode != SlotMode.AtExchange && dto.ForeignCourseId is not null)
                return Error.Validation("INVALID_MAPPING", "Foreign courses are only allowed on slots marked as AtExchange.");
        }
        return null;
    }

    private async Task<ErrorOr<LearningAgreementResponse>?> ValidateForeignCourseEctsAsync(
        SaveLearningAgreementRequest request, CancellationToken ct)
    {
        var allForeignCourseIds = request.Entries
            .Where(e => e.ForeignCourseId.HasValue)
            .Select(e => e.ForeignCourseId!.Value)
            .Distinct().ToList();

        if (allForeignCourseIds.Count == 0) return null;

        var foreignCourses = await db.ForeignCourses
            .AsNoTracking()
            .Where(fc => allForeignCourseIds.Contains(fc.Id))
            .ToDictionaryAsync(fc => fc.Id, fc => fc.Ects, ct);

        var ectsUsage = request.Entries
            .Where(e => e.ForeignCourseId.HasValue && e.AwardedEcts.HasValue)
            .GroupBy(e => e.ForeignCourseId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.AwardedEcts!.Value));

        foreach (var (courseId, totalUsed) in ectsUsage)
        {
            if (!foreignCourses.TryGetValue(courseId, out var maxEcts))
                return Error.NotFound("FOREIGN_COURSE_NOT_FOUND", $"Foreign course {courseId} not found.");
            if (totalUsed > maxEcts)
                return Error.Validation("ECTS_EXCEEDED", $"Awarded ECTS for course {courseId} exceeds available {maxEcts}.");
        }
        return null;
    }

    private async Task DeleteExistingEntriesAsync(Guid laId, CancellationToken ct)
    {
        var existingEntries = await db.LearningAgreementEntries
            .Where(e => e.LearningAgreementId == laId)
            .ToListAsync(ct);

        var entryIds = existingEntries.Select(e => e.Id).ToList();
        if (entryIds.Count > 0)
        {
            var recognitionEntries = await db.RecognitionEntries
                .Where(re => entryIds.Contains(re.LearningAgreementEntryId))
                .ToListAsync(ct);
            db.RecognitionEntries.RemoveRange(recognitionEntries);
        }

        db.LearningAgreementEntries.RemoveRange(existingEntries);
    }

    private async Task CreateNewEntriesAsync(Guid laId, SaveLearningAgreementRequest request, CancellationToken ct)
    {
        foreach (var dto in request.Entries)
        {
            Enum.TryParse<SlotMode>(dto.Mode, out var mode);
            db.LearningAgreementEntries.Add(new LearningAgreementEntry
            {
                LearningAgreementId = laId,
                CourseSlotId = dto.CourseSlotId,
                Mode = mode,
                ForeignCourseId = dto.ForeignCourseId,
                AwardedEcts = dto.AwardedEcts
            });
        }
        await Task.CompletedTask;
    }

    public async Task<ErrorOr<List<ExchangeSummaryResponse>>> GetMyStudentsExchangesAsync(Guid requesterId, CancellationToken ct = default)
    {
        var requester = await db.Users.FindAsync([requesterId], ct);
        if (requester is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        var query = db.Exchanges
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.ForeignProgram).ThenInclude(p => p.Institution)
            .Include(e => e.StudyProfile).ThenInclude(sp => sp.StudyProgram).ThenInclude(p => p.Institution)
            .Include(e => e.Recognition);

        var filtered = requester.IsAdmin()
            ? query
            : query.Where(e => e.Student.CoordinatorId == requesterId);

        var exchanges = await filtered.OrderByDescending(e => e.CreatedAt).ToListAsync(ct);
        return exchanges.Select(e => e.ToSummaryResponse()).ToList();
    }

    public async Task<ErrorOr<ExchangeResponse>> UpdateCoordinatorMessageAsync(Guid exchangeId, Guid requesterId, string? message, CancellationToken ct = default)
    {
        var exchange = await db.Exchanges.Include(e => e.Student).FirstOrDefaultAsync(e => e.Id == exchangeId, ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        var requester = await db.Users.FindAsync([requesterId], ct);
        if (requester is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        if (!requester.IsCoordinatorFor(exchange.CoordinatorId))
            return Error.Forbidden("ACCESS_DENIED", "Only coordinators can update the message.");

        exchange.CoordinatorMessage = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        exchange.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var saved = await ExchangeWithIncludes().FirstOrDefaultAsync(e => e.Id == exchangeId, ct)
            ?? throw new InvalidOperationException();
        return saved.ToResponse();
    }

    public async Task<ErrorOr<Deleted>> DeleteExchangeAsync(Guid exchangeId, Guid requesterId, CancellationToken ct = default)
    {
        var exchange = await db.Exchanges.FirstOrDefaultAsync(e => e.Id == exchangeId, ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");
        if (exchange.StudentId != requesterId) return Error.Forbidden("ACCESS_DENIED", "Access denied.");
        if (exchange.Status != ExchangeStatus.Draft) return Error.Conflict("NOT_DRAFT", "Only draft exchanges can be deleted.");

        db.ExchangeSnapshots.RemoveRange(
            await db.ExchangeSnapshots.Where(s => s.ExchangeId == exchangeId).ToListAsync(ct));

        var learningAgreement = await db.LearningAgreements
            .Include(la => la.Entries)
            .FirstOrDefaultAsync(la => la.ExchangeId == exchangeId, ct);
        if (learningAgreement is not null)
        {
            db.LearningAgreementEntries.RemoveRange(learningAgreement.Entries);
            db.LearningAgreements.Remove(learningAgreement);
        }

        var recognition = await db.Recognitions.
            Include(r => r.Entries).
            FirstOrDefaultAsync(r => r.ExchangeId == exchangeId, ct);
        if (recognition is not null)
        {
            db.RecognitionEntries.RemoveRange(recognition.Entries);
            db.Recognitions.Remove(recognition);
        }

        db.Exchanges.Remove(exchange);
        await db.SaveChangesAsync(ct);
        return Result.Deleted;
    }

    public async Task<ErrorOr<List<ExchangeSnapshotResponse>>> GetSnapshotsAsync(Guid exchangeId, Guid requesterId, CancellationToken ct = default)
    {
        var accessCheck = await CheckExchangeAccessAsync(exchangeId, requesterId, false, ct);
        if (accessCheck.IsError) return accessCheck.Errors;

        var snapshots = await db.ExchangeSnapshots
            .AsNoTracking()
            .Include(s => s.ChangedBy)
            .Where(s => s.ExchangeId == exchangeId)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(ct);

        return snapshots.Select(s => s.ToResponse()).ToList();
    }

    public async Task<ErrorOr<ExchangeSnapshotResponse>> GetSnapshotAsync(Guid exchangeId, Guid snapshotId, Guid requesterId, CancellationToken ct = default)
    {
        var accessCheck = await CheckExchangeAccessAsync(exchangeId, requesterId, false, ct);
        if (accessCheck.IsError) return accessCheck.Errors;

        var snapshot = await db.ExchangeSnapshots
            .AsNoTracking()
            .Include(s => s.ChangedBy)
            .FirstOrDefaultAsync(s => s.Id == snapshotId && s.ExchangeId == exchangeId, ct);
        if (snapshot is null) return Error.NotFound("SNAPSHOT_NOT_FOUND", "Snapshot not found.");

        var data = JsonSerializer.Deserialize<LearningAgreementSnapshotData>(snapshot.Snapshot, JsonHelper.DefaultOptions);

        return new ExchangeSnapshotResponse(
            snapshot.Id,
            snapshot.ExchangeId,
            snapshot.Phase.ToString(),
            snapshot.ChangedById,
            snapshot.ChangedBy.Name,
            snapshot.CreatedAt,
            data
        );
    }
}
