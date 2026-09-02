using ErrorOr;
using Loom.Application.DTOs.Common;
using Loom.Application.DTOs.Institution;
using Loom.Application.DTOs.LearningAgreement;
using Loom.Application.Helpers;
using Loom.Application.Interfaces;
using Loom.Application.Interfaces.Services;
using Loom.Application.Mappers;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Services;

public class InstitutionService(IAppDbContext db, CachedQuery cache) : IInstitutionService
{
    private static readonly TimeSpan LookupTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ListTtl = TimeSpan.FromSeconds(30);

    #region Lookups

    public async Task<ErrorOr<List<InstitutionResponse>>> GetHomeInstitutionsAsync(CancellationToken ct = default)
    {
        return await cache.GetOrCreateAsync("home-institutions", "all", LookupTtl, async () =>
        {
            var institutions = await db.Institutions
                .AsNoTracking()
                .Where(x => x.Type == InstitutionType.Home)
                .OrderBy(x => x.Name)
                .ToListAsync(ct);
            return institutions.Select(i => i.ToResponse()).ToList();
        });
    }

    public async Task<ErrorOr<List<HomeProgramResponse>>> GetHomeProgramsAsync(CancellationToken ct = default)
    {
        return await cache.GetOrCreateAsync("home-programs", "all", LookupTtl, async () =>
        {
            var programs = await db.HomePrograms
                .AsNoTracking()
                .Include(p => p.Profiles)
                .OrderBy(p => p.Name)
                .ToListAsync(ct);
            return programs.Select(p => p.ToResponse()).ToList();
        });
    }

    public async Task<ErrorOr<PagedResponse<PartnerInstitutionAdminResponse>>> GetPartnerInstitutionsAsync(bool includeDeleted, PagedRequest paging, string? country = null, string? sortBy = null, CancellationToken ct = default)
    {
        var key = $"{includeDeleted}:{country}:{sortBy}:{paging.SortDir}:{paging.Search}:{paging.SafePage}:{paging.SafePageSize}";
        return await cache.GetOrCreateAsync("partner-institutions", key, ListTtl, async () =>
        {
            var query = db.Institutions
                .AsNoTracking()
                .Where(i => i.Type == InstitutionType.Partner && (includeDeleted || !i.IsDeleted));

            if (!string.IsNullOrWhiteSpace(country))
                query = query.Where(i => i.Country == country);

            if (!string.IsNullOrWhiteSpace(paging.Search))
            {
                var term = $"%{paging.Search.Trim().ToLower()}%";
                query = query.Where(i =>
                    EF.Functions.Like(i.Name.ToLower(), term) ||
                    (i.NameHr != null && EF.Functions.Like(i.NameHr.ToLower(), term)) ||
                    (i.City != null && EF.Functions.Like(i.City.ToLower(), term)) ||
                    (i.ErasmusCode != null && EF.Functions.Like(i.ErasmusCode.ToLower(), term)));
            }

            var totalCount = await query.CountAsync(ct);
            var hasDeleted = await db.Institutions.AnyAsync(i => i.Type == InstitutionType.Partner && i.IsDeleted, ct);

            var desc = paging.SortDir == "desc";
            Func<IQueryable<Institution>, IOrderedQueryable<Institution>> orderBy = sortBy switch
            {
                "erasmusCode" => q => desc ? q.OrderByDescending(i => i.ErasmusCode) : q.OrderBy(i => i.ErasmusCode),
                "name" => q => desc ? q.OrderByDescending(i => i.Name) : q.OrderBy(i => i.Name),
                "country" => q => desc ? q.OrderByDescending(i => i.Country) : q.OrderBy(i => i.Country),
                _ => q => q.OrderBy(i => i.Country).ThenBy(i => i.Name),
            };

            var items = await orderBy(query)
                .ThenBy(i => i.Id)
                .Skip(paging.Skip)
                .Take(paging.SafePageSize)
                .Select(i => new PartnerInstitutionAdminResponse(
                    i.Id, i.Name, i.NameHr, i.Country, i.City, i.ErasmusCode, i.PartnerCourses.Count, i.IsDeleted))
                .ToListAsync(ct);

            return new PagedResponse<PartnerInstitutionAdminResponse>(items, paging.SafePage, paging.SafePageSize, totalCount, hasDeleted);
        });
    }

    public async Task<ErrorOr<PagedResponse<PartnerCourseResponse>>> GetPartnerCoursesByInstitutionAsync(int institutionId, bool includeDeleted, PagedRequest paging, ExchangeSemester? semester = null, StudyProgramLevel? level = null, string? sortBy = null, CancellationToken ct = default)
    {
        var key = $"{institutionId}:{includeDeleted}:{semester}:{level}:{sortBy}:{paging.SortDir}:{paging.Search}:{paging.SafePage}:{paging.SafePageSize}";
        return await cache.GetOrCreateAsync("partner-courses", key, ListTtl, async () =>
        {
            var query = db.PartnerCourses
                .AsNoTracking()
                .Where(c => c.InstitutionId == institutionId && (includeDeleted || !c.IsDeleted));

            if (semester is not null)
                query = query.Where(c => c.Semester == semester.Value);

            if (level is not null)
                query = query.Where(c => c.Level == level.Value);

            if (!string.IsNullOrWhiteSpace(paging.Search))
            {
                var term = $"%{paging.Search.Trim().ToLower()}%";
                query = query.Where(c =>
                    EF.Functions.Like(c.Code.ToLower(), term) ||
                    EF.Functions.Like(c.Name.ToLower(), term) ||
                    (c.NameHr != null && EF.Functions.Like(c.NameHr.ToLower(), term)));
            }

            var hasDeleted = await db.PartnerCourses.AnyAsync(c => c.InstitutionId == institutionId && c.IsDeleted, ct);

            var desc = paging.SortDir == "desc";
            Func<IQueryable<PartnerCourse>, IOrderedQueryable<PartnerCourse>> orderBy = sortBy switch
            {
                "name" => q => desc ? q.OrderByDescending(c => c.Name) : q.OrderBy(c => c.Name),
                "nameHr" => q => desc ? q.OrderByDescending(c => c.NameHr) : q.OrderBy(c => c.NameHr),
                "semester" => q => desc ? q.OrderByDescending(c => c.Semester) : q.OrderBy(c => c.Semester),
                "level" => q => desc ? q.OrderByDescending(c => c.Level) : q.OrderBy(c => c.Level),
                "ects" => q => desc ? q.OrderByDescending(c => c.Ects) : q.OrderBy(c => c.Ects),
                _ => desc ? q => q.OrderByDescending(c => c.Code) : q => q.OrderBy(c => c.Code),
            };

            var result = await query.ToPagedResponseAsync(paging, orderBy, c => c.Id, c => c.ToResponse(), ct);
            return result with { HasDeleted = hasDeleted };
        });
    }

    #endregion

    #region Partner institutions management

    public async Task<ErrorOr<PartnerInstitutionAdminResponse>> CreatePartnerInstitutionAsync(CreatePartnerInstitutionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Error.Validation("INVALID_NAME", "Institution name is required.");
        if (string.IsNullOrWhiteSpace(request.Country))
            return Error.Validation("INVALID_COUNTRY", "Country is required.");

        var institution = new Institution
        {
            Name = request.Name.Trim(),
            NameHr = string.IsNullOrWhiteSpace(request.NameHr) ? request.Name.Trim() : request.NameHr.Trim(),
            Country = request.Country.Trim(),
            City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
            ErasmusCode = string.IsNullOrWhiteSpace(request.ErasmusCode) ? null : request.ErasmusCode.Trim(),
            Type = InstitutionType.Partner,
        };
        db.Institutions.Add(institution);
        await db.SaveChangesAsync(ct);
        cache.BumpVersion("partner-institutions");

        return await db.Institutions
            .AsNoTracking()
            .Where(i => i.Id == institution.Id)
            .Select(i => new PartnerInstitutionAdminResponse(
                i.Id, i.Name, i.NameHr, i.Country, i.City, i.ErasmusCode, i.PartnerCourses.Count, i.IsDeleted))
            .FirstAsync(ct);
    }

    public async Task<ErrorOr<PartnerInstitutionAdminResponse>> UpdatePartnerInstitutionAsync(int institutionId, UpdateInstitutionRequest request, CancellationToken ct = default)
    {
        var institution = await db.Institutions
            .FirstOrDefaultAsync(i => i.Id == institutionId && i.Type == InstitutionType.Partner, ct);
        if (institution is null) return Error.NotFound("INSTITUTION_NOT_FOUND", "Institution not found.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return Error.Validation("INVALID_NAME", "Institution name is required.");
        if (string.IsNullOrWhiteSpace(request.Country))
            return Error.Validation("INVALID_COUNTRY", "Country is required.");

        institution.Name = request.Name.Trim();
        institution.NameHr = string.IsNullOrWhiteSpace(request.NameHr) ? request.Name.Trim() : request.NameHr.Trim();
        institution.Country = request.Country.Trim();
        institution.City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim();
        institution.ErasmusCode = string.IsNullOrWhiteSpace(request.ErasmusCode) ? null : request.ErasmusCode.Trim();
        await db.SaveChangesAsync(ct);
        cache.BumpVersion("partner-institutions");

        return await db.Institutions
            .AsNoTracking()
            .Where(i => i.Id == institution.Id)
            .Select(i => new PartnerInstitutionAdminResponse(
                i.Id, i.Name, i.NameHr, i.Country, i.City, i.ErasmusCode, i.PartnerCourses.Count, i.IsDeleted))
            .FirstAsync(ct);
    }

    public async Task<ErrorOr<Deleted>> DeletePartnerInstitutionAsync(int institutionId, CancellationToken ct = default)
    {
        var institution = await db.Institutions
            .FirstOrDefaultAsync(i => i.Id == institutionId && i.Type == InstitutionType.Partner, ct);
        if (institution is null) return Error.NotFound("INSTITUTION_NOT_FOUND", "Institution not found.");

        var hasExchanges = await db.Exchanges.AnyAsync(e => e.PartnerInstitutionId == institutionId, ct);
        if (hasExchanges)
        {
            institution.IsDeleted = true;
            institution.DeletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            cache.BumpVersion("partner-institutions");
            return Result.Deleted;
        }

        db.Institutions.Remove(institution);
        await db.SaveChangesAsync(ct);
        cache.BumpVersion("partner-institutions");
        return Result.Deleted;
    }

    public async Task<ErrorOr<Updated>> RestorePartnerInstitutionAsync(int institutionId, CancellationToken ct = default)
    {
        var institution = await db.Institutions
            .FirstOrDefaultAsync(i => i.Id == institutionId && i.Type == InstitutionType.Partner, ct);
        if (institution is null) return Error.NotFound("INSTITUTION_NOT_FOUND", "Institution not found.");

        institution.IsDeleted = false;
        institution.DeletedAt = null;
        await db.SaveChangesAsync(ct);
        cache.BumpVersion("partner-institutions");
        return Result.Updated;
    }

    #endregion

    #region Partner courses management

    public async Task<ErrorOr<PartnerCourseResponse>> CreatePartnerCourseByInstitutionAsync(int institutionId, CreatePartnerCourseRequest request, CancellationToken ct = default)
    {
        var institution = await db.Institutions.FindAsync([institutionId], ct);
        if (institution is null) return Error.NotFound("INSTITUTION_NOT_FOUND", "Institution not found.");
        if (string.IsNullOrWhiteSpace(request.Code))
            return Error.Validation("INVALID_CODE", "Course code is required.");
        if (string.IsNullOrWhiteSpace(request.Name))
            return Error.Validation("INVALID_NAME", "Course name is required.");
        if (!Enum.TryParse<ExchangeSemester>(request.Semester, out var semester))
            return Error.Validation("INVALID_SEMESTER", "Invalid semester.");
        if (!Enum.TryParse<StudyProgramLevel>(request.Level, out var level))
            return Error.Validation("INVALID_LEVEL", "Invalid study program level.");

        var course = new PartnerCourse
        {
            InstitutionId = institutionId,
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            NameHr = string.IsNullOrWhiteSpace(request.NameHr) ? null : request.NameHr.Trim(),
            Url = string.IsNullOrWhiteSpace(request.Url) ? null : request.Url.Trim(),
            Ects = request.Ects,
            LecturesH = request.LecturesH,
            AuditoryH = request.AuditoryH,
            LabH = request.LabH,
            Semester = semester,
            Level = level,
        };
        db.PartnerCourses.Add(course);
        await db.SaveChangesAsync(ct);
        cache.BumpVersion("partner-courses");
        return course.ToResponse();
    }

    public async Task<ErrorOr<PartnerCourseResponse>> UpdatePartnerCourseAsync(int courseId, UpdatePartnerCourseRequest request, CancellationToken ct = default)
    {
        var course = await db.PartnerCourses.FindAsync([courseId], ct);
        if (course is null) return Error.NotFound("COURSE_NOT_FOUND", "Course not found.");
        if (string.IsNullOrWhiteSpace(request.Code))
            return Error.Validation("INVALID_CODE", "Course code is required.");
        if (string.IsNullOrWhiteSpace(request.Name))
            return Error.Validation("INVALID_NAME", "Course name is required.");
        if (!Enum.TryParse<ExchangeSemester>(request.Semester, out var semester))
            return Error.Validation("INVALID_SEMESTER", "Invalid semester.");
        if (!Enum.TryParse<StudyProgramLevel>(request.Level, out var level))
            return Error.Validation("INVALID_LEVEL", "Invalid study program level.");

        var code = request.Code.Trim();
        var duplicate = await db.PartnerCourses.AnyAsync(
            c => c.InstitutionId == course.InstitutionId && c.Id != courseId && c.Code == code, ct);
        if (duplicate) return Error.Conflict("DUPLICATE_CODE", "A course with this code already exists for the institution.");

        course.Code = code;
        course.Name = request.Name.Trim();
        course.NameHr = string.IsNullOrWhiteSpace(request.NameHr) ? null : request.NameHr.Trim();
        course.Url = string.IsNullOrWhiteSpace(request.Url) ? null : request.Url.Trim();
        course.Ects = request.Ects;
        course.LecturesH = request.LecturesH;
        course.AuditoryH = request.AuditoryH;
        course.LabH = request.LabH;
        course.Semester = semester;
        course.Level = level;
        await db.SaveChangesAsync(ct);
        cache.BumpVersion("partner-courses");
        return course.ToResponse();
    }

    public async Task<ErrorOr<Deleted>> DeletePartnerCourseAsync(int courseId, CancellationToken ct = default)
    {
        var course = await db.PartnerCourses.FindAsync([courseId], ct);
        if (course is null) return Error.NotFound("COURSE_NOT_FOUND", "Course not found.");

        var isUsed = await db.LearningAgreementEntries.AnyAsync(e => e.PartnerCourseId == courseId, ct);
        if (isUsed)
        {
            course.IsDeleted = true;
            course.DeletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            cache.BumpVersion("partner-courses");
            return Result.Deleted;
        }

        db.PartnerCourses.Remove(course);
        await db.SaveChangesAsync(ct);
        cache.BumpVersion("partner-courses");
        return Result.Deleted;
    }

    public async Task<ErrorOr<Updated>> RestorePartnerCourseAsync(int courseId, CancellationToken ct = default)
    {
        var course = await db.PartnerCourses.FindAsync([courseId], ct);
        if (course is null) return Error.NotFound("COURSE_NOT_FOUND", "Course not found.");

        course.IsDeleted = false;
        course.DeletedAt = null;
        await db.SaveChangesAsync(ct);
        cache.BumpVersion("partner-courses");
        return Result.Updated;
    }

    public async Task<ErrorOr<PartnerCourseResponse>> MergePartnerCoursesAsync(MergePartnerCoursesRequest request, CancellationToken ct = default)
    {
        if (request.DuplicateCourseIds.Count == 0 || request.DuplicateCourseIds.Contains(request.PrimaryCourseId))
            return Error.Validation("INVALID_MERGE_SET", "Invalid set of courses to merge.");

        var primary = await db.PartnerCourses.FindAsync([request.PrimaryCourseId], ct);
        if (primary is null) return Error.NotFound("COURSE_NOT_FOUND", "Primary course not found.");

        var duplicates = await db.PartnerCourses
            .Where(c => request.DuplicateCourseIds.Contains(c.Id))
            .ToListAsync(ct);
        if (duplicates.Count != request.DuplicateCourseIds.Count)
            return Error.NotFound("COURSE_NOT_FOUND", "One or more courses to merge were not found.");
        if (duplicates.Any(c => c.InstitutionId != primary.InstitutionId))
            return Error.Validation("INVALID_MERGE_SET", "Courses to merge must belong to the same institution.");

        var duplicateIds = duplicates.Select(c => c.Id).ToList();
        var entries = await db.LearningAgreementEntries
            .Where(e => e.PartnerCourseId != null && duplicateIds.Contains(e.PartnerCourseId.Value))
            .ToListAsync(ct);
        foreach (var entry in entries)
            entry.PartnerCourseId = primary.Id;

        db.PartnerCourses.RemoveRange(duplicates);
        await db.SaveChangesAsync(ct);
        cache.BumpVersion("partner-courses");
        return primary.ToResponse();
    }

    public async Task<ErrorOr<PartnerCourseUsageResponse>> GetPartnerCourseUsageAsync(int courseId, CancellationToken ct = default)
    {
        var course = await db.PartnerCourses.FindAsync([courseId], ct);
        if (course is null) return Error.NotFound("COURSE_NOT_FOUND", "Course not found.");

        var entries = await db.LearningAgreementEntries
            .AsNoTracking()
            .Where(e => e.PartnerCourseId == courseId && !e.IsDeleted)
            .Include(e => e.LearningAgreement).ThenInclude(la => la.Exchange).ThenInclude(ex => ex.HomeProfile).ThenInclude(hp => hp.Program)
            .Include(e => e.HomeSlot).ThenInclude(s => s.Course)
            .Include(e => e.HomeSlot).ThenInclude(s => s.CourseGroup)
            .ToListAsync(ct);

        var exchangeCount = entries.Select(e => e.LearningAgreement.ExchangeId).Distinct().Count();

        var groups = entries
            .GroupBy(e => new
            {
                ProgramName = e.LearningAgreement.Exchange.HomeProfile.Program.Name,
                ProfileName = e.LearningAgreement.Exchange.HomeProfile.Name,
                RecognizedAsIsvuCode = e.HomeSlot.Course?.IsvuCode ?? e.HomeSlot.CourseGroup?.IsvuCode,
                RecognizedAsName = e.HomeSlot.Course?.Name ?? e.HomeSlot.CourseGroup?.Name ?? string.Empty,
                IsCourseGroup = e.HomeSlot.Course is null,
            })
            .Select(g => new PartnerCourseUsageGroup(
                g.Key.ProgramName,
                g.Key.ProfileName,
                g.Key.RecognizedAsIsvuCode,
                g.Key.RecognizedAsName,
                g.Key.IsCourseGroup,
                g.Select(e => e.LearningAgreement.ExchangeId).Distinct().Count(),
                g.Sum(e => e.AwardedEcts ?? 0),
                g.Select(e => e.LearningAgreement.Exchange.AcademicYear).Distinct().OrderDescending().ToList()
            ))
            .OrderBy(g => g.ProgramName).ThenBy(g => g.ProfileName)
            .ToList();

        return new PartnerCourseUsageResponse(exchangeCount, groups);
    }

    #endregion
}
