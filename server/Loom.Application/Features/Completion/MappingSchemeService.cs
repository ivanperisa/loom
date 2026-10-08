using ErrorOr;
using Loom.Application.Common;
using Loom.Application.Common.Security;
using Loom.Application.Interfaces;
using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Completion;

/// <summary>
/// The mapping scheme: where each passed course is finally recognised (moving between slots, splitting ECTS, marking a
/// course not passed). Same data as table 2. May drift from the frozen LA; that is the point of it.
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
    /// Replaces the scheme with the request: existing entries by id (moved, resized, status), new entries (id ≤ 0) as
    /// splits of a course already in the scheme, missing entries removed. A course never gets more ECTS than it has.
    /// </summary>
    public async Task<ErrorOr<MappingSchemeResponse>> SaveAsync(Guid exchangeGuid, SaveMappingSchemeRequest request, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        var exchange = context.Value;

        var recognition = await guard.EditableAsync(exchange.ExchangeId, ct);
        if (recognition.IsError) return recognition.Errors;

        var entries = await db.MappingSchemeEntries.Where(e => e.ExchangeId == exchange.ExchangeId).ToListAsync(ct);
        var byId = entries.ToDictionary(e => e.Id);
        var schemeCourses = entries.Where(e => e.PartnerCourseId != null).Select(e => e.PartnerCourseId!.Value).ToHashSet();
        var profileSlots = await db.HomeSlots.Where(s => s.ProfileId == exchange.HomeProfileId).Select(s => s.Id).ToHashSetAsync(ct);

        var keep = new HashSet<int>();
        var resulting = new List<MappingSchemeEntry>();
        foreach (var item in request.Entries)
        {
            if (item.AwardedEcts < 0) return CompletionErrors.NegativeEcts;
            if (!profileSlots.Contains(item.HomeSlotId)) return CompletionErrors.SlotNotInProfile(item.HomeSlotId);
            if (!RecognitionService.TryParseStatus(item.EnrollmentStatus, out var status)) return CompletionErrors.InvalidEnrollmentStatus(item.EnrollmentStatus);
            var grades = RecognitionService.ValidateGrades(item.OriginalGrade, item.EctsGrade, item.HrGrade);
            if (grades.IsError) return grades.Errors;

            MappingSchemeEntry entry;
            if (item.Id > 0)
            {
                if (!byId.TryGetValue(item.Id, out entry!)) return CompletionErrors.EntryNotFound(item.Id);
                keep.Add(entry.Id);
            }
            else
            {
                if (item.PartnerCourseId is not int courseId || !schemeCourses.Contains(courseId)) return CompletionErrors.CourseNotInScheme(item.PartnerCourseId ?? 0);
                entry = new MappingSchemeEntry { ExchangeId = exchange.ExchangeId, PartnerCourseId = courseId };
                db.MappingSchemeEntries.Add(entry);
            }
            entry.HomeSlotId = item.HomeSlotId;
            entry.AwardedEcts = item.AwardedEcts;
            entry.EnrollmentStatus = status;
            entry.OriginalGrade = item.OriginalGrade.NullIfBlank();
            entry.EctsGrade = item.EctsGrade.NullIfBlank();
            entry.HrGrade = item.HrGrade.NullIfBlank();
            entry.ExamDate = item.ExamDate;
            resulting.Add(entry);
        }

        var courseIds = resulting.Where(e => e.PartnerCourseId != null).Select(e => e.PartnerCourseId!.Value).Distinct().ToList();
        var available = await db.PartnerCourses.Where(c => courseIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Ects, ct);
        foreach (var course in resulting.Where(e => e.PartnerCourseId != null).GroupBy(e => e.PartnerCourseId!.Value))
        {
            var max = available.GetValueOrDefault(course.Key);
            if (course.Sum(e => e.AwardedEcts ?? 0) > max) return CompletionErrors.EctsExceeded(course.Key, max);
        }

        foreach (var entry in entries.Where(e => !keep.Contains(e.Id))) db.MappingSchemeEntries.Remove(entry);
        recognition.Value.LastModifiedById = actor.UserId;
        recognition.Value.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await BuildResponseAsync(exchange.ExchangeId, ct);
    }

    private async Task<MappingSchemeResponse> BuildResponseAsync(int exchangeId, CancellationToken ct)
    {
        var entries = await db.MappingSchemeEntries
            .AsNoTracking()
            .Include(e => e.PartnerCourse)
            .Include(e => e.HomeSlot).ThenInclude(s => s.SlotType)
            .Include(e => e.HomeSlot).ThenInclude(s => s.Course)
            .Include(e => e.HomeSlot).ThenInclude(s => s.CourseGroup)
            .Where(e => e.ExchangeId == exchangeId)
            .OrderBy(e => e.Id)
            .ToListAsync(ct);
        return entries.ToResponse(exchangeId);
    }
}
