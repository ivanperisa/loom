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
using System.Text.Json;

namespace Loom.Application.Services;

public class LearningAgreementService(IAppDbContext db) : ILearningAgreementService
{
    public async Task<ErrorOr<LearningAgreementResponse>> GetLearningAgreementAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default)
    {
        var idResult = await db.ResolveExchangeIdAsync(exchangeGuid, ct);
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
                var data = JsonSerializer.Deserialize<LearningAgreementSnapshot>(snapshot.Snapshot, JsonHelper.DefaultOptions);
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
        var idResult = await db.ResolveExchangeIdAsync(exchangeGuid, ct);
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

    public async Task<ErrorOr<ExchangeResponse>> UpdateLearningAgreementStatusAsync(Guid exchangeGuid, int requesterId, UpdateLearningAgreementStatusRequest request, CancellationToken ct = default)
    {
        var idResult = await db.ResolveExchangeIdAsync(exchangeGuid, ct);
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

            var snapshotData = new LearningAgreementSnapshot(
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

    #region Private Methods

    private IQueryable<Exchange> ExchangeWithIncludes() => db.Exchanges
        .AsNoTracking()
        .Include(e => e.Student)
        .Include(e => e.Coordinator)
        .Include(e => e.HomeProfile).ThenInclude(hp => hp.Program).ThenInclude(p => p.Institution)
        .Include(e => e.PartnerProgram).ThenInclude(p => p.Institution)
        .Include(e => e.LearningAgreement);

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
