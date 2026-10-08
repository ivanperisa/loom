using ErrorOr;
using Loom.Application.Common.Security;
using Loom.Application.Interfaces;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Planning;

/// <summary>JSON export of a learning agreement's mappings, and importing such a file into a draft.</summary>
public sealed class LaTransferService(IAppDbContext db, ExchangeAccess access, ICurrentActor actor, LaEntryWriter writer, LaSnapshots snapshots)
{
    public async Task<ErrorOr<MappingExportDto>> ExportAsync(Guid exchangeGuid, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        var exchange = context.Value;

        var entries = await db.LearningAgreementEntries
            .AsNoTracking()
            .Where(e => e.LearningAgreement.ExchangeId == exchange.ExchangeId)
            .OrderBy(e => e.Id)
            .Include(e => e.PartnerCourse)
            .Include(e => e.HomeSlot).ThenInclude(s => s.Course)
            .Include(e => e.HomeSlot).ThenInclude(s => s.CourseGroup)
            .ToListAsync(ct);

        var exportedBy = await db.Users.Where(u => u.Id == actor.UserId).Select(u => u.Name).FirstAsync(ct);
        var partner = await db.Institutions.AsNoTracking()
            .Where(i => i.Id == exchange.PartnerInstitutionId)
            .Select(i => new MappingExportInstitution(i.Id, i.Name, i.ErasmusCode))
            .FirstAsync(ct);
        var home = await db.HomeProfiles.AsNoTracking()
            .Where(p => p.Id == exchange.HomeProfileId)
            .Select(p => new MappingExportHomeContext(p.Id, p.Name, p.Program.Name, p.Program.Institution.Name))
            .FirstAsync(ct);

        return new MappingExportDto(
            Version: 1,
            ExportedAt: DateTime.UtcNow,
            ExportedByName: exportedBy,
            Institution: partner,
            Home: home,
            Mappings: entries.Select(e => new MappingExportEntry(
                e.HomeSlotId,
                e.HomeSlot.Label,
                e.HomeSlot.Semester,
                e.HomeSlot.Ects,
                e.Mode.ToString(),
                e.PartnerCourse is { } c ? new MappingExportCourse(c.Id, c.Code, c.Name, c.Ects) : null,
                e.AwardedEcts)).ToList());
    }

    /// <summary>
    /// Replaces the draft with the file's mappings. Skips slots outside the profile, slots that already have
    /// recognition data and courses that cannot be found; validates like a normal save; keeps a backup.
    /// </summary>
    public async Task<ErrorOr<MappingImportResult>> ImportAsync(Guid exchangeGuid, MappingExportDto file, CancellationToken ct)
    {
        if (file.Version != 1) return PlanningErrors.UnsupportedExportVersion;

        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        var exchange = context.Value;

        var learningAgreement = await db.LearningAgreements.AsNoTracking().FirstOrDefaultAsync(l => l.ExchangeId == exchange.ExchangeId, ct);
        if (learningAgreement is not null && learningAgreement.Status != DocumentStatus.Draft) return PlanningErrors.Locked;

        var profileSlotIds = await db.HomeSlots.Where(s => s.ProfileId == exchange.HomeProfileId).Select(s => s.Id).ToHashSetAsync(ct);
        var courses = await db.PartnerCourses.AsNoTracking().Where(c => c.InstitutionId == exchange.PartnerInstitutionId).ToListAsync(ct);
        var courseById = courses.ToDictionary(c => c.Id);
        var courseByCode = courses.ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);
        var slotsWithRecognition = await db.RecognitionEntries
            .Where(r => r.LearningAgreementEntry.LearningAgreement.ExchangeId == exchange.ExchangeId
                && (r.EnrollmentStatus != null || r.OriginalGrade != null || r.EctsGrade != null
                    || r.HrGrade != null || r.ExamDate != null || r.IsRecognized != null))
            .Select(r => r.LearningAgreementEntry.HomeSlotId)
            .ToHashSetAsync(ct);

        var apply = new List<LearningAgreementEntryUpsertDto>();
        var skipped = new List<MappingImportSkip>();
        foreach (var entry in file.Mappings)
        {
            if (!profileSlotIds.Contains(entry.HomeSlotId))
            {
                skipped.Add(new MappingImportSkip(entry.HomeSlotId, entry.HomeSlotLabel, "SlotNotInProfile"));
                continue;
            }
            if (slotsWithRecognition.Contains(entry.HomeSlotId))
            {
                skipped.Add(new MappingImportSkip(entry.HomeSlotId, entry.HomeSlotLabel, "RecognitionExists"));
                continue;
            }

            int? courseId = null;
            if (entry.Mode == nameof(SlotMode.AtExchange) && entry.PartnerCourse is not null)
            {
                courseId = courseById.TryGetValue(entry.PartnerCourse.Id, out var byId) ? byId.Id
                    : courseByCode.TryGetValue(entry.PartnerCourse.Code, out var byCode) ? byCode.Id
                    : null;
                if (courseId is null)
                {
                    skipped.Add(new MappingImportSkip(entry.HomeSlotId, entry.HomeSlotLabel, "CourseNotFound"));
                    continue;
                }
            }
            apply.Add(new LearningAgreementEntryUpsertDto(entry.HomeSlotId, entry.Mode, courseId, entry.AwardedEcts));
        }

        if (apply.Count > 0)
        {
            var request = new SaveLearningAgreementRequest(apply);
            var valid = await writer.ValidateAsync(request, exchange, ct);
            if (valid.IsError) return valid.Errors;

            await snapshots.AddBackupAsync(exchange.ExchangeId, actor.UserId, ct);

            var tracked = await writer.GetOrCreateAsync(exchange.ExchangeId, ct);
            var replaced = await writer.ReplaceEntriesAsync(tracked.Id, request, ct);
            if (replaced.IsError) return replaced.Errors;

            tracked.LastModifiedById = actor.UserId;
            await db.SaveChangesAsync(ct);
        }

        var appliedCourses = apply.Where(a => a.PartnerCourseId.HasValue).Select(a => a.PartnerCourseId!.Value).Distinct().Count();
        return new MappingImportResult(appliedCourses, skipped);
    }
}
