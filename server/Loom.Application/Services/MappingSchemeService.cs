using ErrorOr;
using Loom.Application.DTOs.MappingScheme;
using Loom.Application.Helpers;
using Loom.Application.Interfaces;
using Loom.Application.Interfaces.Services;
using Loom.Application.Mappers;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Services;

public class MappingSchemeService(IAppDbContext db) : IMappingSchemeService
{
    private IQueryable<MappingSchemeEntry> WithIncludes() => db.MappingSchemeEntries
        .Include(e => e.PartnerCourse)
        .Include(e => e.HomeSlot).ThenInclude(s => s.SlotType)
        .Include(e => e.HomeSlot).ThenInclude(s => s.Course)
        .Include(e => e.HomeSlot).ThenInclude(s => s.CourseGroup)
        .Include(e => e.RecognizedAsCourse);

    private async Task<ErrorOr<int>> CheckAccessAsync(Guid exchangeGuid, int requesterId, CancellationToken ct)
    {
        var idResult = await db.ResolveExchangeIdAsync(exchangeGuid, ct);
        if (idResult.IsError) return idResult.Errors;
        var exchangeId = idResult.Value;

        var accessCheck = await db.CheckExchangeAccessAsync(exchangeId, requesterId, ct: ct);
        if (accessCheck.IsError) return accessCheck.Errors;

        return exchangeId;
    }

    public async Task<ErrorOr<MappingSchemeResponse>> GetMappingSchemeAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default)
    {
        var accessResult = await CheckAccessAsync(exchangeGuid, requesterId, ct);
        if (accessResult.IsError) return accessResult.Errors;
        var exchangeId = accessResult.Value;

        var entries = await WithIncludes()
            .Where(e => e.ExchangeId == exchangeId)
            .OrderBy(e => e.Id)
            .ToListAsync(ct);

        return entries.ToResponse(exchangeId);
    }

    public async Task<ErrorOr<MappingSchemeResponse>> SaveMappingSchemeAsync(Guid exchangeGuid, int requesterId, SaveMappingSchemeRequest request, CancellationToken ct = default)
    {
        var accessResult = await CheckAccessAsync(exchangeGuid, requesterId, ct);
        if (accessResult.IsError) return accessResult.Errors;
        var exchangeId = accessResult.Value;

        var entries = await db.MappingSchemeEntries
            .Where(e => e.ExchangeId == exchangeId)
            .ToListAsync(ct);

        var validPartnerCourseIds = entries
            .Where(e => e.PartnerCourseId != null)
            .Select(e => e.PartnerCourseId!.Value)
            .ToHashSet();

        var keepIds = new HashSet<int>();

        foreach (var req in request.Entries)
        {
            if (req.AwardedEcts < 0)
                return Error.Validation("INVALID_ECTS", "Awarded ECTS cannot be negative.");

            if (req.Id > 0)
            {
                var existing = entries.FirstOrDefault(e => e.Id == req.Id);
                if (existing is null) continue;

                existing.HomeSlotId = req.HomeSlotId;
                existing.AwardedEcts = req.AwardedEcts;
                existing.EnrollmentStatus = ParseStatus(req.EnrollmentStatus);
                existing.OriginalGrade = req.OriginalGrade;
                existing.EctsGrade = req.EctsGrade;
                existing.HrGrade = req.HrGrade;
                existing.ExamDate = req.ExamDate;
                keepIds.Add(existing.Id);
            }
            else
            {
                if (req.PartnerCourseId is not int pcId || !validPartnerCourseIds.Contains(pcId))
                    return Error.Validation("INVALID_PARTNER_COURSE", "Split entry must reference a partner course already in the scheme.");

                db.MappingSchemeEntries.Add(new MappingSchemeEntry
                {
                    ExchangeId = exchangeId,
                    HomeSlotId = req.HomeSlotId,
                    PartnerCourseId = pcId,
                    AwardedEcts = req.AwardedEcts,
                    EnrollmentStatus = ParseStatus(req.EnrollmentStatus),
                    OriginalGrade = req.OriginalGrade,
                    EctsGrade = req.EctsGrade,
                    HrGrade = req.HrGrade,
                    ExamDate = req.ExamDate,
                });
            }
        }

        foreach (var e in entries)
            if (!keepIds.Contains(e.Id))
                db.MappingSchemeEntries.Remove(e);

        await db.SaveChangesAsync(ct);
        return await GetMappingSchemeAsync(exchangeGuid, requesterId, ct);
    }

    public async Task<bool> EnsureMappingSchemeInitializedAsync(int exchangeId, CancellationToken ct = default)
    {
        var exists = await db.MappingSchemeEntries.AnyAsync(e => e.ExchangeId == exchangeId, ct);
        if (exists) return false;

        var laEntries = await db.LearningAgreementEntries
            .AsNoTracking()
            .Where(e => e.LearningAgreement.ExchangeId == exchangeId && e.PartnerCourseId != null && !e.IsDeleted)
            .ToListAsync(ct);
        if (laEntries.Count == 0) return false;

        foreach (var la in laEntries)
        {
            db.MappingSchemeEntries.Add(new MappingSchemeEntry
            {
                ExchangeId = exchangeId,
                HomeSlotId = la.HomeSlotId,
                PartnerCourseId = la.PartnerCourseId,
                AwardedEcts = la.AwardedEcts,
            });
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    public static EnrollmentStatus? ParseStatus(string? value) =>
        Enum.TryParse<EnrollmentStatus>(value, out var parsed) ? parsed : null;
}
