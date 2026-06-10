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
            .Include(la => la.LastModifiedByUser)
            .Include(la => la.SignedByUser)
            .FirstOrDefaultAsync(la => la.ExchangeId == exchangeId, ct);

        var activeEntries = la?.Entries.Select(e => e.ToResponse()).ToList() ?? [];

        var snapshots = await db.ExchangeSnapshots
            .AsNoTracking()
            .Where(s => s.ExchangeId == exchangeId && s.Phase == SnapshotPhase.LearningAgreement && s.Type == SnapshotType.Auto)
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
                var data = JsonSerializer.Deserialize<LaSnapshotData>(snapshot.Snapshot, JsonHelper.DefaultOptions);
                if (data is null) continue;
                foreach (var entry in data.Entries.Where(e => e.PartnerCourseId.HasValue))
                {
                    var key = (entry.HomeSlotId, entry.PartnerCourseId!.Value);
                    if (!activeKeys.Contains(key) && seen.Add(key))
                    {
                        deletedEntries.Add(new LearningAgreementEntryResponse(
                            0, entry.HomeSlotId, entry.Mode,
                            entry.PartnerCourseId, entry.PartnerCourseCode, entry.PartnerCourseName, null,
                            entry.AwardedEcts, IsDeleted: true));
                    }
                }
            }
        }

        return new LearningAgreementResponse(
            exchangeId,
            la?.Status.ToString() ?? DocumentStatus.Draft.ToString(),
            la?.Message,
            slots.Select(s => s.ToResponse()).ToList(),
            [.. activeEntries, .. deletedEntries],
            la?.UpdatedAt,
            la?.LastModifiedByUser?.Name,
            la?.SignedAt,
            la?.SignedByUser?.Name
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
        var upsertResult = await UpsertEntriesAsync(laEntity.Id, request, ct);
        if (upsertResult.IsError) return upsertResult.Errors;

        laEntity.LastModifiedById = requesterId;
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
        la.LastModifiedById = requesterId;

        if (newStatus == DocumentStatus.Approved)
        {
            la.SignedAt = DateTime.UtcNow;
            la.SignedById = requesterId;
        }
        else if (newStatus == DocumentStatus.Draft)
        {
            la.SignedAt = null;
            la.SignedById = null;
        }

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
                .Include(la => la.Entries)
                    .ThenInclude(e => e.HomeSlot).ThenInclude(s => s.Course)
                .Include(la => la.Entries)
                    .ThenInclude(e => e.HomeSlot).ThenInclude(s => s.CourseGroup)
                .FirstOrDefaultAsync(l => l.ExchangeId == exchangeId, ct);

            var snapshotData = new LaSnapshotData(
                laWithEntries?.Entries.Select(e => new LaSnapshotEntry(
                    e.HomeSlotId,
                    e.HomeSlot.Course?.Name ?? e.HomeSlot.CourseGroup?.Name ?? $"Slot {e.HomeSlotId}",
                    e.HomeSlot.Semester,
                    e.HomeSlot.Ects,
                    e.Mode.ToString(),
                    e.PartnerCourseId,
                    e.PartnerCourse?.Code,
                    e.PartnerCourse?.Name,
                    e.AwardedEcts
                )).ToList() ?? []);

            db.ExchangeSnapshots.Add(new ExchangeSnapshot
            {
                ExchangeId = exchangeId,
                ChangedById = requesterId,
                Phase = SnapshotPhase.LearningAgreement,
                Type = SnapshotType.Auto,
                Snapshot = JsonSerializer.Serialize(snapshotData, JsonHelper.DefaultOptions)
            });
        }

        await db.SaveChangesAsync(ct);

        var saved = await ExchangeWithIncludes().FirstOrDefaultAsync(e => e.Id == exchangeId, ct)
            ?? throw new InvalidOperationException();
        return saved.ToResponse();
    }

    public async Task<ErrorOr<LearningAgreementResponse>> UpdateLearningAgreementMessageAsync(Guid exchangeGuid, int requesterId, string? message, CancellationToken ct = default)
    {
        var idResult = await db.ResolveExchangeIdAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var accessCheck = await CheckExchangeAccessAsync(exchangeId, requesterId, false, ct);
        if (accessCheck.IsError) return accessCheck.Errors;

        var la = await db.LearningAgreements.FirstOrDefaultAsync(l => l.ExchangeId == exchangeId, ct);
        if (la is null) return Error.NotFound("LA_NOT_FOUND", "Learning agreement not found.");

        la.Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        la.UpdatedAt = DateTime.UtcNow;
        la.LastModifiedById = requesterId;
        await db.SaveChangesAsync(ct);

        return await GetLearningAgreementAsync(exchangeGuid, requesterId, ct);
    }

    public async Task<ErrorOr<MappingExportDto>> ExportMappingsAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default)
    {
        var idResult = await db.ResolveExchangeIdAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var accessCheck = await CheckExchangeAccessAsync(exchangeId, requesterId, false, ct);
        if (accessCheck.IsError) return accessCheck.Errors;
        var (exchange, requester) = accessCheck.Value;

        var laWithEntries = await db.LearningAgreements
            .AsNoTracking()
            .Include(la => la.Entries)
                .ThenInclude(e => e.PartnerCourse)
            .Include(la => la.Entries)
                .ThenInclude(e => e.HomeSlot).ThenInclude(s => s.Course)
            .Include(la => la.Entries)
                .ThenInclude(e => e.HomeSlot).ThenInclude(s => s.CourseGroup)
            .FirstOrDefaultAsync(la => la.ExchangeId == exchangeId, ct);

        var partnerInstitution = await db.Institutions
            .AsNoTracking()
            .FirstOrDefaultAsync(pi => pi.Id == exchange.PartnerInstitutionId, ct);

        var homeProfile = await db.HomeProfiles
            .AsNoTracking()
            .Include(p => p.Program).ThenInclude(pr => pr.Institution)
            .FirstOrDefaultAsync(p => p.Id == exchange.HomeProfileId, ct);

        var mappings = (laWithEntries?.Entries ?? []).Select(e => new MappingExportEntry(
            e.HomeSlotId,
            e.HomeSlot.Course?.Name ?? e.HomeSlot.CourseGroup?.Name ?? $"Slot {e.HomeSlotId}",
            e.HomeSlot.Semester,
            e.HomeSlot.Ects,
            e.Mode.ToString(),
            e.PartnerCourse is not null ? new MappingExportCourse(e.PartnerCourse.Id, e.PartnerCourse.Code, e.PartnerCourse.Name, e.PartnerCourse.Ects) : null,
            e.AwardedEcts
        )).ToList();

        return new MappingExportDto(
            Version: 1,
            ExportedAt: DateTime.UtcNow,
            ExportedByName: requester.Name,
            Institution: new MappingExportInstitution(partnerInstitution?.Id ?? 0, partnerInstitution?.Name ?? "", partnerInstitution?.ErasmusCode),
            Home: new MappingExportHomeContext(
                homeProfile?.Id ?? 0,
                homeProfile?.Name ?? "",
                homeProfile?.Program.Name ?? "",
                homeProfile?.Program.Institution.Name ?? ""
            ),
            Mappings: mappings
        );
    }

    public async Task<ErrorOr<MappingImportResult>> ImportMappingsAsync(Guid exchangeGuid, int requesterId, MappingExportDto dto, CancellationToken ct = default)
    {
        if (dto.Version != 1)
            return Error.Validation("INVALID_VERSION", "Unsupported export version.");

        var idResult = await db.ResolveExchangeIdAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var accessCheck = await CheckExchangeAccessAsync(exchangeId, requesterId, false, ct);
        if (accessCheck.IsError) return accessCheck.Errors;
        var (exchange, _) = accessCheck.Value;

        var profileSlotIds = (await db.HomeSlots
            .AsNoTracking()
            .Where(s => s.ProfileId == exchange.HomeProfileId)
            .Select(s => s.Id)
            .ToListAsync(ct)).ToHashSet();

        var partnerCourses = await db.PartnerCourses
            .AsNoTracking()
            .Where(pc => pc.InstitutionId == exchange.PartnerInstitutionId)
            .ToListAsync(ct);
        var partnerCourseById = partnerCourses.ToDictionary(pc => pc.Id);
        var partnerCourseByCode = partnerCourses.ToDictionary(pc => pc.Code, StringComparer.OrdinalIgnoreCase);

        var la = await db.LearningAgreements
            .Include(la => la.Entries)
            .FirstOrDefaultAsync(la => la.ExchangeId == exchangeId, ct);

        var entriesWithRecognition = la is not null
            ? (await db.RecognitionEntries
                .Where(re => la.Entries.Select(e => e.Id).Contains(re.LearningAgreementEntryId)
                    && (re.EnrollmentStatus != null || re.OriginalGrade != null || re.EctsGrade != null
                        || re.HrGrade != null || re.ExamDate != null || re.IsRecognized != null))
                .Select(re => re.LearningAgreementEntry.HomeSlotId)
                .ToListAsync(ct)).ToHashSet()
            : [];

        var applyList = new List<(MappingExportEntry entry, int? resolvedCourseId)>();
        var skipped = new List<MappingImportSkip>();

        foreach (var entry in dto.Mappings)
        {
            if (!profileSlotIds.Contains(entry.HomeSlotId))
            {
                skipped.Add(new MappingImportSkip(entry.HomeSlotId, entry.HomeSlotLabel, "SlotNotInProfile"));
                continue;
            }

            if (entriesWithRecognition.Contains(entry.HomeSlotId))
            {
                skipped.Add(new MappingImportSkip(entry.HomeSlotId, entry.HomeSlotLabel, "RecognitionExists"));
                continue;
            }

            int? resolvedCourseId = null;
            if (entry.Mode == "AtExchange" && entry.PartnerCourse is not null)
            {
                if (partnerCourseById.TryGetValue(entry.PartnerCourse.Id, out var byId))
                    resolvedCourseId = byId.Id;
                else if (partnerCourseByCode.TryGetValue(entry.PartnerCourse.Code, out var byCode))
                    resolvedCourseId = byCode.Id;
                else
                {
                    skipped.Add(new MappingImportSkip(entry.HomeSlotId, entry.HomeSlotLabel, "CourseNotFound"));
                    continue;
                }
            }

            applyList.Add((entry, resolvedCourseId));
        }

        if (applyList.Count > 0)
        {
            await SavePreImportSnapshotAsync(exchangeId, requesterId, ct);

            var saveRequest = new SaveLearningAgreementRequest(applyList.Select(a =>
                new LearningAgreementEntryUpsertDto(a.entry.HomeSlotId, a.entry.Mode, a.resolvedCourseId, a.entry.AwardedEcts)
            ).ToList());

            var laEntity = await GetOrCreateLearningAgreementAsync(exchangeId, ct);
            var upsertResult = await UpsertEntriesAsync(laEntity.Id, saveRequest, ct);
            if (upsertResult.IsError) return upsertResult.Errors;

            if (la?.Status is DocumentStatus.Approved or DocumentStatus.Submitted)
            {
                var laRecord = await db.LearningAgreements.FirstAsync(l => l.ExchangeId == exchangeId, ct);
                laRecord.Status = DocumentStatus.Draft;
                laRecord.UpdatedAt = DateTime.UtcNow;
                laRecord.SignedAt = null;
                laRecord.SignedById = null;
            }

            laEntity.LastModifiedById = requesterId;
            await db.SaveChangesAsync(ct);
        }

        var appliedCourseCount = applyList
            .Where(a => a.resolvedCourseId.HasValue)
            .Select(a => a.resolvedCourseId!.Value)
            .Distinct()
            .Count();

        return new MappingImportResult(appliedCourseCount, skipped);
    }

    public async Task<ErrorOr<List<LaSnapshotSummary>>> GetLearningAgreementHistoryAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default)
    {
        var idResult = await db.ResolveExchangeIdAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var accessCheck = await CheckExchangeAccessAsync(exchangeId, requesterId, false, ct);
        if (accessCheck.IsError) return accessCheck.Errors;

        var snapshots = await db.ExchangeSnapshots
            .AsNoTracking()
            .Include(s => s.ChangedBy)
            .Where(s => s.ExchangeId == exchangeId && s.Phase == SnapshotPhase.LearningAgreement && s.Type == SnapshotType.Auto)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(ct);

        var result = new List<LaSnapshotSummary>();
        LaSnapshotData? previous = null;

        foreach (var snapshot in snapshots)
        {
            var data = JsonSerializer.Deserialize<LaSnapshotData>(snapshot.Snapshot, JsonHelper.DefaultOptions);
            if (data is null) continue;

            var diff = previous is not null ? ComputeLaDiff(data, previous) : null;
            result.Add(new LaSnapshotSummary(snapshot.Id, snapshot.CreatedAt, snapshot.ChangedBy.Name, data.Entries.Count, diff));
            previous = data;
        }

        result.Reverse();
        return result;
    }

    public async Task<ErrorOr<List<SnapshotListItem>>> GetSnapshotsAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default)
    {
        var idResult = await db.ResolveExchangeIdAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var accessCheck = await CheckExchangeAccessAsync(exchangeId, requesterId, false, ct);
        if (accessCheck.IsError) return accessCheck.Errors;

        var snapshots = await db.ExchangeSnapshots
            .AsNoTracking()
            .Include(s => s.ChangedBy)
            .Where(s => s.ExchangeId == exchangeId && s.Phase == SnapshotPhase.LearningAgreement)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

        return snapshots.Select(s =>
        {
            var data = JsonSerializer.Deserialize<LaSnapshotData>(s.Snapshot, JsonHelper.DefaultOptions);
            return new SnapshotListItem(s.Id, s.Type, s.CreatedAt, s.ChangedBy.Name, data?.Entries.Count ?? 0);
        }).ToList();
    }

    public async Task<ErrorOr<Updated>> RestoreSnapshotAsync(Guid exchangeGuid, int snapshotId, int requesterId, CancellationToken ct = default)
    {
        var idResult = await db.ResolveExchangeIdAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var accessCheck = await CheckExchangeAccessAsync(exchangeId, requesterId, false, ct);
        if (accessCheck.IsError) return accessCheck.Errors;

        var snapshot = await db.ExchangeSnapshots
            .FirstOrDefaultAsync(s => s.Id == snapshotId && s.ExchangeId == exchangeId && s.Phase == SnapshotPhase.LearningAgreement, ct);
        if (snapshot is null) return Error.NotFound("SNAPSHOT_NOT_FOUND", "Snapshot not found.");

        var data = JsonSerializer.Deserialize<LaSnapshotData>(snapshot.Snapshot, JsonHelper.DefaultOptions);
        if (data is null) return Error.Validation("INVALID_SNAPSHOT", "Snapshot data is corrupted.");

        await SavePreImportSnapshotAsync(exchangeId, requesterId, ct);

        var exchange = await db.Exchanges.FirstOrDefaultAsync(e => e.Id == exchangeId, ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        var filteredCoursesForInstitution = await db.PartnerCourses
            .AsNoTracking()
            .Where(pc => pc.InstitutionId == exchange.PartnerInstitutionId)
            .ToDictionaryAsync(pc => pc.Code, StringComparer.OrdinalIgnoreCase, ct);

        var saveRequest = new SaveLearningAgreementRequest(data.Entries.Select(e =>
        {
            int? courseId = null;
            if (e.PartnerCourseId.HasValue && filteredCoursesForInstitution.TryGetValue(e.PartnerCourseCode ?? "", out var pc))
                courseId = pc.Id;
            else if (e.PartnerCourseId.HasValue)
                courseId = e.PartnerCourseId;

            return new LearningAgreementEntryUpsertDto(e.HomeSlotId, e.Mode, courseId, e.AwardedEcts);
        }).ToList());

        var laEntity = await GetOrCreateLearningAgreementAsync(exchangeId, ct);
        var upsertResult = await UpsertEntriesAsync(laEntity.Id, saveRequest, ct);
        if (upsertResult.IsError) return upsertResult.Errors;

        var la = await db.LearningAgreements.FirstAsync(l => l.ExchangeId == exchangeId, ct);
        la.Status = DocumentStatus.Draft;
        la.UpdatedAt = DateTime.UtcNow;
        la.LastModifiedById = requesterId;
        la.SignedAt = null;
        la.SignedById = null;

        await db.SaveChangesAsync(ct);
        return Result.Updated;
    }


    #region Private Methods

    private IQueryable<Exchange> ExchangeWithIncludes() => db.Exchanges
        .AsNoTracking()
        .Include(e => e.Student)
        .Include(e => e.Coordinator)
        .Include(e => e.HomeProfile).ThenInclude(hp => hp.Program).ThenInclude(p => p.Institution)
        .Include(e => e.PartnerInstitution)
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

    private async Task<ErrorOr<Success>> UpsertEntriesAsync(int laId, SaveLearningAgreementRequest request, CancellationToken ct)
    {
        var existingEntries = await db.LearningAgreementEntries
            .Where(e => e.LearningAgreementId == laId)
            .ToListAsync(ct);

        var existingIds = existingEntries.Select(e => e.Id).ToList();
        var idsWithRecognition = existingIds.Count > 0
            ? (await db.RecognitionEntries
                .Where(re => existingIds.Contains(re.LearningAgreementEntryId)
                    && (re.EnrollmentStatus != null || re.OriginalGrade != null || re.EctsGrade != null
                        || re.HrGrade != null || re.ExamDate != null || re.IsRecognized != null))
                .Select(re => re.LearningAgreementEntryId)
                .ToListAsync(ct)).ToHashSet()
            : [];

        var existingByKey = existingEntries.ToDictionary(e => (e.HomeSlotId, e.PartnerCourseId));
        var requestedKeys = request.Entries.Select(dto => (dto.HomeSlotId, dto.PartnerCourseId)).ToHashSet();

        var toDelete = existingEntries.Where(e => !requestedKeys.Contains((e.HomeSlotId, e.PartnerCourseId))).ToList();
        if (toDelete.Any(e => idsWithRecognition.Contains(e.Id)))
            return Error.Conflict("RECOGNITION_EXISTS", "Cannot remove a slot mapping that has recognition data.");

        var toDeleteIds = toDelete.Select(e => e.Id).ToList();
        if (toDeleteIds.Count > 0)
        {
            var emptyRecognitionEntries = await db.RecognitionEntries
                .Where(re => toDeleteIds.Contains(re.LearningAgreementEntryId))
                .ToListAsync(ct);
            db.RecognitionEntries.RemoveRange(emptyRecognitionEntries);
        }

        db.LearningAgreementEntries.RemoveRange(toDelete);

        foreach (var dto in request.Entries)
        {
            Enum.TryParse<SlotMode>(dto.Mode, out var mode);
            var key = (dto.HomeSlotId, dto.PartnerCourseId);
            if (existingByKey.TryGetValue(key, out var existing))
            {
                existing.Mode = mode;
                existing.AwardedEcts = dto.AwardedEcts;
            }
            else
            {
                db.LearningAgreementEntries.Add(new LearningAgreementEntry
                {
                    LearningAgreementId = laId,
                    HomeSlotId = dto.HomeSlotId,
                    Mode = mode,
                    PartnerCourseId = dto.PartnerCourseId,
                    AwardedEcts = dto.AwardedEcts
                });
            }
        }

        return Result.Success;
    }

    private async Task SavePreImportSnapshotAsync(int exchangeId, int requesterId, CancellationToken ct)
    {
        var currentLa = await db.LearningAgreements
            .AsNoTracking()
            .Include(la => la.Entries)
                .ThenInclude(e => e.PartnerCourse)
            .Include(la => la.Entries)
                .ThenInclude(e => e.HomeSlot).ThenInclude(s => s.Course)
            .Include(la => la.Entries)
                .ThenInclude(e => e.HomeSlot).ThenInclude(s => s.CourseGroup)
            .FirstOrDefaultAsync(la => la.ExchangeId == exchangeId, ct);

        if (currentLa is null || currentLa.Entries.Count == 0) return;

        var snapshotData = new LaSnapshotData(
            currentLa.Entries.Select(e => new LaSnapshotEntry(
                e.HomeSlotId,
                e.HomeSlot.Course?.Name ?? e.HomeSlot.CourseGroup?.Name ?? $"Slot {e.HomeSlotId}",
                e.HomeSlot.Semester,
                e.HomeSlot.Ects,
                e.Mode.ToString(),
                e.PartnerCourseId,
                e.PartnerCourse?.Code,
                e.PartnerCourse?.Name,
                e.AwardedEcts
            )).ToList());

        db.ExchangeSnapshots.Add(new ExchangeSnapshot
        {
            ExchangeId = exchangeId,
            ChangedById = requesterId,
            Phase = SnapshotPhase.LearningAgreement,
            Type = SnapshotType.PreImport,
            Snapshot = JsonSerializer.Serialize(snapshotData, JsonHelper.DefaultOptions)
        });
    }

    private static LaSnapshotDiff ComputeLaDiff(LaSnapshotData current, LaSnapshotData previous)
    {
        var prevByKey = previous.Entries.Where(e => e.PartnerCourseId.HasValue)
            .ToDictionary(e => (e.HomeSlotId, e.PartnerCourseId!.Value));
        var currByKey = current.Entries.Where(e => e.PartnerCourseId.HasValue)
            .ToDictionary(e => (e.HomeSlotId, e.PartnerCourseId!.Value));

        var added = currByKey.Where(kv => !prevByKey.ContainsKey(kv.Key)).Select(kv => kv.Value).ToList();
        var removed = prevByKey.Where(kv => !currByKey.ContainsKey(kv.Key)).Select(kv => kv.Value).ToList();
        var modified = currByKey
            .Where(kv => prevByKey.TryGetValue(kv.Key, out var prev) &&
                (prev.AwardedEcts != kv.Value.AwardedEcts || prev.Mode != kv.Value.Mode))
            .Select(kv => new LaSnapshotEntryChange(prevByKey[kv.Key], kv.Value))
            .ToList();

        return new LaSnapshotDiff(added, removed, modified);
    }

    #endregion
}
