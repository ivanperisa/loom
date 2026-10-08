using ErrorOr;
using Loom.Application.Common.Security;
using Loom.Application.Interfaces;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Planning;

/// <summary>
/// The one place that checks and writes learning agreement entries. Save, import and restore all go through it,
/// so they follow the same rules: editable only as a draft before final recognition, same validation, same history.
/// </summary>
public sealed class LaEntryWriter(IAppDbContext db)
{
    /// <summary>The LA can change only while it is a draft and final recognition has not started.</summary>
    public async Task<ErrorOr<Success>> CheckEditableAsync(int exchangeId, CancellationToken ct)
    {
        var state = await db.LearningAgreements
            .Where(l => l.ExchangeId == exchangeId)
            .Select(l => new { l.Status, l.ConcludedAt })
            .FirstOrDefaultAsync(ct);
        if (state is null) return Result.Success;
        if (state.ConcludedAt is not null) return PlanningErrors.Concluded;
        if (state.Status != DocumentStatus.Draft) return PlanningErrors.Locked;
        return Result.Success;
    }

    /// <summary>
    /// Valid modes; one mode per slot; no course twice in a slot; slots from the exchange's profile; courses from its
    /// partner institution, each with positive ECTS, and in total not more than the course has.
    /// </summary>
    public async Task<ErrorOr<Success>> ValidateAsync(SaveLearningAgreementRequest request, ExchangeContext exchange, CancellationToken ct)
    {
        foreach (var entry in request.Entries)
        {
            if (!Enum.TryParse<SlotMode>(entry.Mode, out var mode) || !Enum.IsDefined(mode)) return PlanningErrors.InvalidMode(entry.Mode);
            if (mode != SlotMode.AtExchange && entry.PartnerCourseId is not null) return PlanningErrors.CourseOnNonExchangeSlot;
            if (entry.PartnerCourseId is not null && entry.AwardedEcts is not > 0) return PlanningErrors.EctsRequired(entry.HomeSlotId);
        }
        foreach (var slot in request.Entries.GroupBy(e => e.HomeSlotId))
        {
            if (slot.Select(e => e.Mode).Distinct().Count() > 1) return PlanningErrors.MixedSlotModes(slot.Key);
            if (slot.GroupBy(e => e.PartnerCourseId).Any(g => g.Count() > 1)) return PlanningErrors.DuplicateEntry(slot.Key);
        }

        var profileSlotIds = await db.HomeSlots
            .Where(s => s.ProfileId == exchange.HomeProfileId)
            .Select(s => s.Id)
            .ToHashSetAsync(ct);
        var foreignSlot = request.Entries.FirstOrDefault(e => !profileSlotIds.Contains(e.HomeSlotId));
        if (foreignSlot is not null) return PlanningErrors.SlotNotInProfile(foreignSlot.HomeSlotId);

        var courseIds = request.Entries.Where(e => e.PartnerCourseId.HasValue).Select(e => e.PartnerCourseId!.Value).Distinct().ToList();
        if (courseIds.Count == 0) return Result.Success;

        var courses = await db.PartnerCourses
            .Where(c => courseIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => new { c.Ects, c.InstitutionId }, ct);
        foreach (var group in request.Entries.Where(e => e.PartnerCourseId.HasValue).GroupBy(e => e.PartnerCourseId!.Value))
        {
            if (!courses.TryGetValue(group.Key, out var course)) return PlanningErrors.PartnerCourseNotFound(group.Key);
            if (course.InstitutionId != exchange.PartnerInstitutionId) return PlanningErrors.CourseNotAtPartner(group.Key);
            if (group.Sum(e => e.AwardedEcts!.Value) > course.Ects) return PlanningErrors.EctsExceeded(group.Key, course.Ects);
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
        return learningAgreement;
    }

    /// <summary>
    /// Makes the live entries match the request (keyed by slot + course). Something that was never approved is simply
    /// deleted; an approved component is only marked for removal, so the amendment can show it struck through.
    /// Adding back a component marked for removal un-marks it.
    /// </summary>
    public async Task ApplyAsync(int learningAgreementId, SaveLearningAgreementRequest request, CancellationToken ct)
    {
        var live = await db.LearningAgreementEntries
            .Where(e => e.LearningAgreementId == learningAgreementId && e.RemovedInVersion == null)
            .ToListAsync(ct);
        var byKey = live.GroupBy(e => (e.HomeSlotId, e.PartnerCourseId)).ToDictionary(g => g.Key, g => g.First());
        var requested = request.Entries.Select(e => (e.HomeSlotId, e.PartnerCourseId)).ToHashSet();

        foreach (var entry in live.Where(e => !requested.Contains((e.HomeSlotId, e.PartnerCourseId)) || byKey[(e.HomeSlotId, e.PartnerCourseId)] != e))
        {
            if (entry.AddedInVersion is null) db.LearningAgreementEntries.Remove(entry);
            else entry.IsDeleted = true;
        }

        foreach (var dto in request.Entries)
        {
            var mode = Enum.Parse<SlotMode>(dto.Mode);   // checked by ValidateAsync
            if (byKey.TryGetValue((dto.HomeSlotId, dto.PartnerCourseId), out var entry))
            {
                entry.IsDeleted = false;
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
    }
}
