using ErrorOr;
using Loom.Application.Common;
using Loom.Application.Common.Security;
using Loom.Application.Interfaces;
using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Completion;

/// <summary>
/// The mapping scheme: where each course's result is finally recognised (moving between slots, splitting ECTS, marking a
/// course not passed). The placements belong to table 2's results. May drift from the frozen LA; that is the point of it.
/// </summary>
public sealed class MappingSchemeService(IAppDbContext db, ExchangeAccess access, ResultsGuard guard, ICurrentActor actor)
{
    public async Task<ErrorOr<MappingSchemeResponse>> GetAsync(Guid exchangeGuid, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        return await BuildResponseAsync(context.Value.ExchangeId, ct);
    }

    /// <summary>
    /// Replaces the scheme with the request: existing placements by id (moved, resized), new ones (id ≤ 0) as splits of a
    /// course already in the scheme, missing ones removed. Every course keeps at least one placement, sits in a slot at
    /// most once and never gets more ECTS than it has.
    /// </summary>
    public async Task<ErrorOr<MappingSchemeResponse>> SaveAsync(Guid exchangeGuid, SaveMappingSchemeRequest request, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        var exchange = context.Value;

        var recognition = await guard.EditableAsync(exchange.ExchangeId, ct);
        if (recognition.IsError) return recognition.Errors;

        var results = await db.RecognitionEntries
            .Include(r => r.PartnerCourse)
            .Include(r => r.Placements)
            .Where(r => r.RecognitionId == recognition.Value.Id)
            .ToDictionaryAsync(r => r.PartnerCourseId, ct);
        var placements = results.Values.SelectMany(r => r.Placements).ToDictionary(p => p.Id);
        var profileSlots = await db.HomeSlots.Where(s => s.ProfileId == exchange.HomeProfileId).Select(s => s.Id).ToHashSetAsync(ct);

        foreach (var item in request.Statuses)
        {
            if (!RecognitionService.TryParseStatus(item.EnrollmentStatus, out var status)) return CompletionErrors.InvalidEnrollmentStatus(item.EnrollmentStatus);
            if (!results.TryGetValue(item.PartnerCourseId, out var result)) return CompletionErrors.CourseNotInScheme(item.PartnerCourseId);
            result.EnrollmentStatus = status;
        }

        var keep = new HashSet<int>();
        var resulting = new List<(RecognitionEntry Result, MappingSchemeEntry Placement)>();
        foreach (var item in request.Entries)
        {
            if (item.AwardedEcts < 0) return CompletionErrors.NegativeEcts;
            if (!profileSlots.Contains(item.HomeSlotId)) return CompletionErrors.SlotNotInProfile(item.HomeSlotId);

            MappingSchemeEntry placement;
            if (item.Id > 0)
            {
                if (!placements.TryGetValue(item.Id, out placement!)) return CompletionErrors.EntryNotFound(item.Id);
                keep.Add(placement.Id);
            }
            else
            {
                if (!results.TryGetValue(item.PartnerCourseId, out var owner)) return CompletionErrors.CourseNotInScheme(item.PartnerCourseId);
                placement = new MappingSchemeEntry { RecognitionEntry = owner };
                owner.Placements.Add(placement);
            }
            placement.HomeSlotId = item.HomeSlotId;
            placement.AwardedEcts = item.AwardedEcts;
            resulting.Add((placement.RecognitionEntry, placement));
        }

        foreach (var result in results.Values)
        {
            var course = resulting.Where(r => r.Result == result).Select(r => r.Placement).ToList();
            if (course.Count == 0) return CompletionErrors.CourseNotPlaced(result.PartnerCourseId);
            if (course.GroupBy(p => p.HomeSlotId).FirstOrDefault(g => g.Count() > 1) is { } twice)
                return CompletionErrors.CourseTwiceInSlot(result.PartnerCourseId, twice.Key);
            if (course.Sum(p => p.AwardedEcts) > result.PartnerCourse.Ects) return CompletionErrors.EctsExceeded(result.PartnerCourseId, result.PartnerCourse.Ects);
        }

        foreach (var placement in placements.Values.Where(p => !keep.Contains(p.Id))) db.MappingSchemeEntries.Remove(placement);
        recognition.Value.LastModifiedById = actor.UserId;
        recognition.Value.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await BuildResponseAsync(exchange.ExchangeId, ct);
    }

    private async Task<MappingSchemeResponse> BuildResponseAsync(int exchangeId, CancellationToken ct)
    {
        var entries = await db.MappingSchemeEntries
            .AsNoTracking()
            .Include(e => e.RecognitionEntry).ThenInclude(r => r.PartnerCourse)
            .Include(e => e.HomeSlot).ThenInclude(s => s.SlotType)
            .Include(e => e.HomeSlot).ThenInclude(s => s.Course)
            .Include(e => e.HomeSlot).ThenInclude(s => s.CourseGroup)
            .Where(e => e.RecognitionEntry.Recognition.ExchangeId == exchangeId)
            .OrderBy(e => e.Id)
            .ToListAsync(ct);
        return entries.ToResponse(exchangeId);
    }
}
