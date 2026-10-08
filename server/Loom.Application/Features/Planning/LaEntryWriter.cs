using ErrorOr;
using Loom.Application.Common.Security;
using Loom.Application.Interfaces;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Planning;

/// <summary>
/// The one place that validates and writes learning agreement entries.
/// Used by save, import and restore, so they all apply the same rules.
/// </summary>
public sealed class LaEntryWriter(IAppDbContext db)
{
    /// <summary>Modes, slots belonging to the exchange's profile, and ECTS not exceeding each partner course.</summary>
    public async Task<ErrorOr<Success>> ValidateAsync(SaveLearningAgreementRequest request, ExchangeContext exchange, CancellationToken ct)
    {
        foreach (var entry in request.Entries)
        {
            if (!Enum.TryParse<SlotMode>(entry.Mode, out var mode)) return PlanningErrors.InvalidMode(entry.Mode);
            if (mode != SlotMode.AtExchange && entry.PartnerCourseId is not null) return PlanningErrors.CourseOnNonExchangeSlot;
        }

        var profileSlotIds = await db.HomeSlots
            .Where(s => s.ProfileId == exchange.HomeProfileId)
            .Select(s => s.Id)
            .ToHashSetAsync(ct);
        var foreignSlot = request.Entries.FirstOrDefault(e => !profileSlotIds.Contains(e.HomeSlotId));
        if (foreignSlot is not null) return PlanningErrors.SlotNotInProfile(foreignSlot.HomeSlotId);

        var courseIds = request.Entries.Where(e => e.PartnerCourseId.HasValue).Select(e => e.PartnerCourseId!.Value).Distinct().ToList();
        if (courseIds.Count == 0) return Result.Success;

        var available = await db.PartnerCourses
            .Where(c => courseIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Ects, ct);
        var used = request.Entries
            .Where(e => e.PartnerCourseId.HasValue && e.AwardedEcts.HasValue)
            .GroupBy(e => e.PartnerCourseId!.Value)
            .Select(g => (CourseId: g.Key, Ects: g.Sum(e => e.AwardedEcts!.Value)));
        foreach (var (courseId, ects) in used)
        {
            if (!available.TryGetValue(courseId, out var max)) return PlanningErrors.PartnerCourseNotFound(courseId);
            if (ects > max) return PlanningErrors.EctsExceeded(courseId, max);
        }
        return Result.Success;
    }

    public async Task<LearningAgreement> GetOrCreateAsync(int exchangeId, CancellationToken ct)
    {
        var learningAgreement = await db.LearningAgreements.FirstOrDefaultAsync(la => la.ExchangeId == exchangeId, ct);
        if (learningAgreement is not null) return learningAgreement;

        learningAgreement = new LearningAgreement { ExchangeId = exchangeId, Status = DocumentStatus.Draft };
        db.LearningAgreements.Add(learningAgreement);
        await db.SaveChangesAsync(ct);
        learningAgreement.Entries = [];
        return learningAgreement;
    }

    /// <summary>
    /// Makes the entries match the request (keyed by slot + course): updates, adds and removes.
    /// Entries that already have recognition data cannot be removed.
    /// </summary>
    public async Task<ErrorOr<Success>> ReplaceEntriesAsync(int learningAgreementId, SaveLearningAgreementRequest request, CancellationToken ct)
    {
        var existing = await db.LearningAgreementEntries.Where(e => e.LearningAgreementId == learningAgreementId).ToListAsync(ct);
        var requestedKeys = request.Entries.Select(e => (e.HomeSlotId, e.PartnerCourseId)).ToHashSet();
        var toDelete = existing.Where(e => !requestedKeys.Contains((e.HomeSlotId, e.PartnerCourseId))).ToList();

        if (toDelete.Count > 0)
        {
            var toDeleteIds = toDelete.Select(e => e.Id).ToList();
            var recognitionEntries = await db.RecognitionEntries.Where(r => toDeleteIds.Contains(r.LearningAgreementEntryId)).ToListAsync(ct);
            if (recognitionEntries.Any(r => r.EnrollmentStatus != null || r.OriginalGrade != null || r.EctsGrade != null
                    || r.HrGrade != null || r.ExamDate != null || r.IsRecognized != null))
                return PlanningErrors.RecognitionExists;

            db.RecognitionEntries.RemoveRange(recognitionEntries);
            db.LearningAgreementEntries.RemoveRange(toDelete);
        }

        var existingByKey = existing.ToDictionary(e => (e.HomeSlotId, e.PartnerCourseId));
        foreach (var dto in request.Entries)
        {
            var mode = Enum.Parse<SlotMode>(dto.Mode);   // validated by ValidateAsync
            if (existingByKey.TryGetValue((dto.HomeSlotId, dto.PartnerCourseId), out var entry))
            {
                entry.Mode = mode;
                entry.AwardedEcts = dto.AwardedEcts;
            }
            else
            {
                db.LearningAgreementEntries.Add(new LearningAgreementEntry
                {
                    LearningAgreementId = learningAgreementId,
                    HomeSlotId = dto.HomeSlotId,
                    Mode = mode,
                    PartnerCourseId = dto.PartnerCourseId,
                    AwardedEcts = dto.AwardedEcts,
                });
            }
        }
        return Result.Success;
    }
}
