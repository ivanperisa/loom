using ErrorOr;
using Loom.Application.DTOs.Exchange;
using Loom.Application.DTOs.LearningAgreement;
using Loom.Application.Helpers;
using Loom.Application.Interfaces;
using Loom.Application.Interfaces.Services;
using Loom.Application.Mappers;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;
using System.Text.Json;

namespace Loom.Application.Services;

public class ExchangeService(IAppDbContext db) : IExchangeService
{
    public async Task<ErrorOr<ExchangeResponse>> CreateExchangeAsync(int studentId, CreateExchangeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.AcademicYear))
            return Error.Validation("INVALID_ACADEMIC_YEAR", "Academic year is required.");
        if (!Enum.TryParse<ExchangeSemester>(request.SemesterType, out var semesterType))
            return Error.Validation("INVALID_SEMESTER_TYPE", "Invalid semester type.");
        if (request.StudySemesters is not { Count: > 0 } || request.StudySemesters.Any(s => s < 1 || s > 10))
            return Error.Validation("INVALID_STUDY_SEMESTER", "Study semesters must be between 1 and 10.");

        var student = await db.Users.FindAsync([studentId], ct);
        if (student is null) return Error.NotFound("USER_NOT_FOUND", "Student not found.");

        var homeProfile = await db.HomeProfiles.FindAsync([request.HomeProfileId], ct);
        if (homeProfile is null) return Error.NotFound("HOME_PROFILE_NOT_FOUND", "Home profile not found.");

        var partnerProgram = await db.PartnerPrograms
            .AsNoTracking()
            .Include(p => p.Institution)
            .FirstOrDefaultAsync(p => p.Id == request.PartnerProgramId, ct);
        if (partnerProgram is null) return Error.NotFound("PARTNER_PROGRAM_NOT_FOUND", "Partner profile not found.");

        var exchange = new Exchange
        {
            StudentId = studentId,
            CoordinatorId = student.CoordinatorId,
            HomeProfileId = request.HomeProfileId,
            PartnerProgramId = request.PartnerProgramId,
            AcademicYear = request.AcademicYear,
            SemesterType = semesterType,
            StudySemesters = request.StudySemesters,
        };
        db.Exchanges.Add(exchange);
        await db.SaveChangesAsync(ct);

        db.LearningAgreements.Add(new LearningAgreement { ExchangeId = exchange.Id, Status = DocumentStatus.Draft });
        await db.SaveChangesAsync(ct);

        var saved = await ExchangeWithIncludes()
            .FirstOrDefaultAsync(e => e.Id == exchange.Id, ct)
            ?? throw new InvalidOperationException();
        return saved.ToResponse();
    }

    public async Task<ErrorOr<ExchangeResponse>> GetExchangeAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default)
    {
        var idResult = await ResolveExchangeGuidAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var exchange = await ExchangeWithIncludes().FirstOrDefaultAsync(e => e.Id == exchangeId, ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        var requester = await db.Users.FindAsync([requesterId], ct);
        if (requester is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        if (exchange.StudentId != requesterId && !requester.IsCoordinatorFor(exchange.CoordinatorId))
            return Error.Forbidden("ACCESS_DENIED", "Access denied.");

        return exchange.ToResponse();
    }

    public async Task<ErrorOr<List<ExchangeSummaryResponse>>> GetMyExchangesAsync(int studentId, CancellationToken ct = default)
    {
        var exchanges = await db.Exchanges
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.PartnerProgram).ThenInclude(p => p.Institution)
            .Include(e => e.HomeProfile).ThenInclude(hp => hp.Program).ThenInclude(p => p.Institution)
            .Include(e => e.LearningAgreement)
            .Include(e => e.Recognition)
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);
        return exchanges.Select(e => e.ToSummaryResponse()).ToList();
    }

    public async Task<ErrorOr<ExchangeResponse>> UpdateLearningAgreementStatusAsync(Guid exchangeGuid, int requesterId, UpdateLearningAgreementStatusRequest request, CancellationToken ct = default)
    {
        var idResult = await ResolveExchangeGuidAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        if (!Enum.TryParse<DocumentStatus>(request.Status, out var newStatus))
            return Error.Validation("INVALID_STATUS", "Invalid status.");

        var exchange = await db.Exchanges.Include(e => e.Student).FirstOrDefaultAsync(e => e.Id == exchangeId, ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        var requester = await db.Users.FindAsync([requesterId], ct);
        if (requester is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        var isStudent = exchange.StudentId == requesterId;
        var isCoordinatorOrAdmin = requester.IsCoordinatorFor(exchange.CoordinatorId);

        if (!isStudent && !isCoordinatorOrAdmin)
            return Error.Forbidden("ACCESS_DENIED", "Access denied.");

        var la = await db.LearningAgreements.FirstOrDefaultAsync(la => la.ExchangeId == exchangeId, ct);
        if (la is null) return Error.NotFound("LA_NOT_FOUND", "Learning agreement not found.");

        if (isStudent && newStatus != DocumentStatus.Submitted && newStatus != DocumentStatus.Draft)
            return Error.Forbidden("FORBIDDEN", "Students can only submit or revert to draft.");
        if (isStudent && newStatus == DocumentStatus.Draft && la.Status == DocumentStatus.Approved)
            return Error.Forbidden("FORBIDDEN", "Cannot revert an approved learning agreement to draft.");

        la.Status = newStatus;
        la.UpdatedAt = DateTime.UtcNow;

        if (isCoordinatorOrAdmin && request.Message is not null)
            exchange.CoordinatorMessage = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim();
        if (newStatus == DocumentStatus.Approved)
            exchange.CoordinatorMessage = null;

        exchange.UpdatedAt = DateTime.UtcNow;

        if (newStatus == DocumentStatus.Approved)
        {
            var laWithEntries = await db.LearningAgreements
                .AsNoTracking()
                .Include(la => la.Entries)
                    .ThenInclude(e => e.PartnerCourse)
                .FirstOrDefaultAsync(l => l.ExchangeId == exchangeId, ct);

            var snapshotData = new LearningAgreementSnapshotData(
                laWithEntries?.Entries.Select(e => e.ToResponse()).ToList() ?? []);

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

    public async Task<ErrorOr<LearningAgreementResponse>> GetLearningAgreementAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default)
    {
        var idResult = await ResolveExchangeGuidAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var accessCheck = await CheckExchangeAccessAsync(exchangeId, requesterId, true, ct);
        if (accessCheck.IsError) return accessCheck.Errors;
        var (exchange, _) = accessCheck.Value;

        var slots = await db.HomeSlots
            .AsNoTracking()
            .Include(s => s.SlotType)
            .Include(s => s.Course)
            .Include(s => s.CourseGroup)
            .Where(s => s.ProfileId == exchange.HomeProfileId)
            .OrderBy(s => s.Semester).ThenBy(s => s.SlotPosition)
            .ToListAsync(ct);

        var la = await db.LearningAgreements
            .AsNoTracking()
            .Include(la => la.Entries)
                .ThenInclude(e => e.PartnerCourse)
            .FirstOrDefaultAsync(la => la.ExchangeId == exchangeId, ct);

        var activeEntries = la?.Entries.Select(e => e.ToResponse()).ToList() ?? [];

        var snapshots = await db.ExchangeSnapshots
            .AsNoTracking()
            .Where(s => s.ExchangeId == exchangeId && s.Phase == SnapshotPhase.LearningAgreement)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(ct);

        var deletedEntries = new List<LearningAgreementEntryResponse>();
        if (snapshots.Count > 0)
        {
            var activeKeys = activeEntries
                .Where(e => e.PartnerCourseId.HasValue)
                .Select(e => (e.HomeSlotId, e.PartnerCourseId!.Value))
                .ToHashSet();

            var seen = new HashSet<(int, int)>();
            foreach (var snapshot in snapshots)
            {
                var data = JsonSerializer.Deserialize<LearningAgreementSnapshotData>(snapshot.Snapshot, JsonHelper.DefaultOptions);
                if (data is null) continue;
                foreach (var entry in data.Entries.Where(e => e.PartnerCourseId.HasValue))
                {
                    var key = (entry.HomeSlotId, entry.PartnerCourseId!.Value);
                    if (!activeKeys.Contains(key) && seen.Add(key))
                        deletedEntries.Add(entry with { IsDeleted = true });
                }
            }
        }

        return new LearningAgreementResponse(
            exchangeId,
            la?.Status.ToString() ?? DocumentStatus.Draft.ToString(),
            slots.Select(s => s.ToResponse()).ToList(),
            [.. activeEntries, .. deletedEntries]
        );
    }

    public async Task<ErrorOr<LearningAgreementResponse>> SaveLearningAgreementAsync(Guid exchangeGuid, int requesterId, SaveLearningAgreementRequest request, CancellationToken ct = default)
    {
        var idResult = await ResolveExchangeGuidAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var accessCheck = await CheckExchangeAccessAsync(exchangeId, requesterId, true, ct);
        if (accessCheck.IsError) return accessCheck.Errors;
        var (exchange, _) = accessCheck.Value;

        var la = await db.LearningAgreements.FirstOrDefaultAsync(l => l.ExchangeId == exchangeId, ct);
        if (la is not null && (la.Status is DocumentStatus.Approved or DocumentStatus.Submitted))
            return Error.Conflict("LA_LOCKED", "Learning agreement cannot be modified in current status.");

        var validationError = ValidateEntryRequest(request);
        if (validationError is not null) return validationError.Value;

        var profileSlotIds = await db.HomeSlots
            .AsNoTracking()
            .Where(s => s.ProfileId == exchange.HomeProfileId)
            .Select(s => s.Id)
            .ToHashSetAsync(ct);

        foreach (var dto in request.Entries)
        {
            if (!profileSlotIds.Contains(dto.HomeSlotId))
                return Error.Validation("SLOT_NOT_IN_PROFILE", $"Home slot {dto.HomeSlotId} does not belong to this profile.");
        }

        var ectsError = await ValidatePartnerCourseEctsAsync(request, ct);
        if (ectsError is not null) return ectsError.Value;

        var laEntity = await GetOrCreateLearningAgreementAsync(exchangeId, ct);
        await DeleteExistingEntriesAsync(laEntity.Id, ct);
        await CreateNewEntriesAsync(laEntity.Id, request, ct);

        await db.SaveChangesAsync(ct);
        return await GetLearningAgreementAsync(exchangeGuid, requesterId, ct);
    }

    private static ErrorOr<LearningAgreementResponse>? ValidateEntryRequest(SaveLearningAgreementRequest request)
    {
        foreach (var dto in request.Entries)
        {
            if (!Enum.TryParse<SlotMode>(dto.Mode, out var mode))
                return Error.Validation("INVALID_MODE", $"Invalid slot mode: {dto.Mode}.");
            if (mode != SlotMode.AtExchange && dto.PartnerCourseId is not null)
                return Error.Validation("INVALID_MAPPING", "Partner courses are only allowed on slots marked as AtExchange.");
        }
        return null;
    }

    private async Task<ErrorOr<LearningAgreementResponse>?> ValidatePartnerCourseEctsAsync(
        SaveLearningAgreementRequest request, CancellationToken ct)
    {
        var allPartnerCourseIds = request.Entries
            .Where(e => e.PartnerCourseId.HasValue)
            .Select(e => e.PartnerCourseId!.Value)
            .Distinct().ToList();

        if (allPartnerCourseIds.Count == 0) return null;

        var partnerCourses = await db.PartnerCourses
            .AsNoTracking()
            .Where(c => allPartnerCourseIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Ects, ct);

        var ectsUsage = request.Entries
            .Where(e => e.PartnerCourseId.HasValue && e.AwardedEcts.HasValue)
            .GroupBy(e => e.PartnerCourseId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.AwardedEcts!.Value));

        foreach (var (courseId, totalUsed) in ectsUsage)
        {
            if (!partnerCourses.TryGetValue(courseId, out var maxEcts))
                return Error.NotFound("PARTNER_COURSE_NOT_FOUND", $"Partner course {courseId} not found.");
            if (totalUsed > maxEcts)
                return Error.Validation("ECTS_EXCEEDED", $"Awarded ECTS for course {courseId} exceeds available {maxEcts}.");
        }
        return null;
    }

    public async Task<ErrorOr<List<ExchangeSummaryResponse>>> GetMyStudentsExchangesAsync(int requesterId, CancellationToken ct = default)
    {
        var requester = await db.Users.FindAsync([requesterId], ct);
        if (requester is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        var query = db.Exchanges
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.PartnerProgram).ThenInclude(p => p.Institution)
            .Include(e => e.HomeProfile).ThenInclude(hp => hp.Program).ThenInclude(p => p.Institution)
            .Include(e => e.LearningAgreement)
            .Include(e => e.Recognition);

        var filtered = requester.IsAdmin()
            ? query
            : query.Where(e => e.Student.CoordinatorId == requesterId);

        var exchanges = await filtered.OrderByDescending(e => e.CreatedAt).ToListAsync(ct);
        return exchanges.Select(e => e.ToSummaryResponse()).ToList();
    }

    public async Task<ErrorOr<ExchangeResponse>> UpdateCoordinatorMessageAsync(Guid exchangeGuid, int requesterId, string? message, CancellationToken ct = default)
    {
        var idResult = await ResolveExchangeGuidAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

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

    public async Task<ErrorOr<Deleted>> DeleteExchangeAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default)
    {
        var idResult = await ResolveExchangeGuidAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var exchange = await db.Exchanges
            .Include(e => e.Student)
            .FirstOrDefaultAsync(e => e.Id == exchangeId, ct);

        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        if (exchange.StudentId != requesterId &&
            exchange.Student.CoordinatorId != requesterId &&
            exchange.Student.Role != UserRole.Admin)
            return Error.Forbidden("ACCESS_DENIED", "Access denied.");

        var la = await db.LearningAgreements
            .Include(l => l.Entries)
            .FirstOrDefaultAsync(l => l.ExchangeId == exchangeId, ct);

        var recognition = await db.Recognitions
            .Include(r => r.Entries)
            .FirstOrDefaultAsync(r => r.ExchangeId == exchangeId, ct);

        if (la?.Status != DocumentStatus.Draft || recognition?.Status != DocumentStatus.Draft)
            return Error.Conflict("NOT_DRAFT", "Only draft exchanges can be deleted.");

        db.ExchangeSnapshots.RemoveRange(
            await db.ExchangeSnapshots.Where(s => s.ExchangeId == exchangeId).ToListAsync(ct));

        if (la is not null)
        {
            db.LearningAgreementEntries.RemoveRange(la.Entries);
            db.LearningAgreements.Remove(la);
        }

        if (recognition is not null)
        {
            db.RecognitionEntries.RemoveRange(recognition.Entries);
            db.Recognitions.Remove(recognition);
        }

        db.Exchanges.Remove(exchange);
        await db.SaveChangesAsync(ct);
        return Result.Deleted;
    }

    public async Task<ErrorOr<List<ExchangeSnapshotResponse>>> GetSnapshotsAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default)
    {
        var idResult = await ResolveExchangeGuidAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

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

    public async Task<ErrorOr<ExchangeSnapshotResponse>> GetSnapshotAsync(Guid exchangeGuid, int snapshotId, int requesterId, CancellationToken ct = default)
    {
        var idResult = await ResolveExchangeGuidAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

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

    #region Private Methods

    private IQueryable<Exchange> ExchangeWithIncludes() => db.Exchanges
        .AsNoTracking()
        .Include(e => e.Student)
        .Include(e => e.Coordinator)
        .Include(e => e.HomeProfile).ThenInclude(hp => hp.Program).ThenInclude(p => p.Institution)
        .Include(e => e.PartnerProgram).ThenInclude(p => p.Institution)
        .Include(e => e.LearningAgreement);

    private async Task<ErrorOr<int>> ResolveExchangeGuidAsync(Guid guid, CancellationToken ct)
    {
        var id = await db.Exchanges.Where(e => e.Guid == guid).Select(e => e.Id).FirstOrDefaultAsync(ct);
        return id == 0 ? Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.") : id;
    }

    private async Task<ErrorOr<(Exchange exchange, User requester)>> CheckExchangeAccessAsync(
        int exchangeId, int requesterId, bool requireStudentInclude = false, CancellationToken ct = default)
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

    private async Task<LearningAgreement> GetOrCreateLearningAgreementAsync(int exchangeId, CancellationToken ct)
    {
        var learningAgreement = await db.LearningAgreements.FirstOrDefaultAsync(la => la.ExchangeId == exchangeId, ct);

        if (learningAgreement is not null)
            return learningAgreement;

        learningAgreement = new LearningAgreement { ExchangeId = exchangeId, Status = DocumentStatus.Draft };

        db.LearningAgreements.Add(learningAgreement);
        await db.SaveChangesAsync(ct);

        learningAgreement.Entries = [];
        return learningAgreement;
    }

    private async Task DeleteExistingEntriesAsync(int laId, CancellationToken ct)
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

    private async Task CreateNewEntriesAsync(int laId, SaveLearningAgreementRequest request, CancellationToken ct)
    {
        foreach (var dto in request.Entries)
        {
            Enum.TryParse<SlotMode>(dto.Mode, out var mode);
            db.LearningAgreementEntries.Add(new LearningAgreementEntry
            {
                LearningAgreementId = laId,
                HomeSlotId = dto.HomeSlotId,
                Mode = mode,
                PartnerCourseId = dto.PartnerCourseId,
                AwardedEcts = dto.AwardedEcts
            });
        }
        await Task.CompletedTask;
    }

    #endregion
}
