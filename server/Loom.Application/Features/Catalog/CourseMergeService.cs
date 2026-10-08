using ErrorOr;
using Loom.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Catalog;

/// <summary>Merges duplicate partner courses into one, moving every reference to the primary course.</summary>
public sealed class CourseMergeService(IAppDbContext db)
{
    public async Task<ErrorOr<PartnerCourseResponse>> MergeAsync(MergePartnerCoursesRequest request, CancellationToken ct)
    {
        if (request.DuplicateCourseIds.Count == 0 || request.DuplicateCourseIds.Contains(request.PrimaryCourseId))
            return CatalogErrors.InvalidMergeSet;

        var primary = await db.PartnerCourses.FindAsync([request.PrimaryCourseId], ct);
        if (primary is null) return CatalogErrors.PrimaryCourseNotFound;

        var duplicates = await db.PartnerCourses.Where(c => request.DuplicateCourseIds.Contains(c.Id)).ToListAsync(ct);
        if (duplicates.Count != request.DuplicateCourseIds.Count) return CatalogErrors.MergeCourseNotFound;
        if (duplicates.Any(c => c.InstitutionId != primary.InstitutionId)) return CatalogErrors.MergeAcrossInstitutions;

        var duplicateIds = duplicates.Select(c => c.Id).ToList();
        var mergedIds = duplicateIds.Append(primary.Id).ToList();

        // Merging must not put two of the merged courses into the same slot of one document.
        var laConflict = await db.LearningAgreementEntries
            .Where(e => e.PartnerCourseId != null && mergedIds.Contains(e.PartnerCourseId.Value))
            .GroupBy(e => new { e.LearningAgreementId, e.HomeSlotId })
            .AnyAsync(g => g.Count() > 1, ct);
        var schemeConflict = await db.MappingSchemeEntries
            .Where(e => e.PartnerCourseId != null && mergedIds.Contains(e.PartnerCourseId.Value))
            .GroupBy(e => new { e.ExchangeId, e.HomeSlotId })
            .AnyAsync(g => g.Count() > 1, ct);
        if (laConflict || schemeConflict) return CatalogErrors.MergeConflict;

        var laEntries = await db.LearningAgreementEntries
            .Where(e => e.PartnerCourseId != null && duplicateIds.Contains(e.PartnerCourseId.Value)).ToListAsync(ct);
        var schemeEntries = await db.MappingSchemeEntries
            .Where(e => e.PartnerCourseId != null && duplicateIds.Contains(e.PartnerCourseId.Value)).ToListAsync(ct);
        laEntries.ForEach(e => e.PartnerCourseId = primary.Id);
        schemeEntries.ForEach(e => e.PartnerCourseId = primary.Id);

        db.PartnerCourses.RemoveRange(duplicates);
        await db.SaveChangesAsync(ct);
        return primary.ToResponse();
    }
}
