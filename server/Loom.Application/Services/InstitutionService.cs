using ErrorOr;
using Loom.Application.DTOs.Institution;
using Loom.Application.DTOs.LearningAgreement;
using Loom.Application.Interfaces;
using Loom.Application.Interfaces.Services;
using Loom.Application.Mappers;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Services;

public class InstitutionService(IAppDbContext db) : IInstitutionService
{
    #region Lookups

    public async Task<ErrorOr<List<InstitutionResponse>>> GetHomeInstitutionsAsync(CancellationToken ct = default)
    {
        var institutions = await db.Institutions
            .AsNoTracking()
            .Where(x => x.Type == InstitutionType.Home)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
        return institutions.Select(i => i.ToResponse()).ToList();
    }

    public async Task<ErrorOr<List<HomeProgramResponse>>> GetHomeProgramsAsync(CancellationToken ct = default)
    {
        var programs = await db.HomePrograms
            .AsNoTracking()
            .Include(p => p.Profiles)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
        return programs.Select(p => p.ToResponse()).ToList();
    }

    public async Task<ErrorOr<List<PartnerInstitutionAdminResponse>>> GetPartnerInstitutionsAsync(bool includeDeleted = false, CancellationToken ct = default)
    {
        var institutions = await db.Institutions
            .AsNoTracking()
            .Where(i => i.Type == InstitutionType.Partner && (includeDeleted || !i.IsDeleted))
            .Include(i => i.PartnerCourses)
            .OrderBy(i => i.Country)
            .ThenBy(i => i.Name)
            .ToListAsync(ct);
        return institutions.Select(i => i.ToAdminResponse()).ToList();
    }

    public async Task<ErrorOr<List<PartnerCourseResponse>>> GetPartnerCoursesByInstitutionAsync(int institutionId, bool includeDeleted = false, CancellationToken ct = default)
    {
        var courses = await db.PartnerCourses
            .AsNoTracking()
            .Where(c => c.InstitutionId == institutionId && (includeDeleted || !c.IsDeleted))
            .OrderBy(c => c.Code)
            .ToListAsync(ct);
        return courses.Select(c => c.ToResponse()).ToList();
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

        var saved = await db.Institutions
            .AsNoTracking()
            .Where(i => i.Id == institution.Id)
            .Include(i => i.PartnerCourses)
            .FirstAsync(ct);
        return saved.ToAdminResponse();
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

        var saved = await db.Institutions
            .AsNoTracking()
            .Where(i => i.Id == institution.Id)
            .Include(i => i.PartnerCourses)
            .FirstAsync(ct);
        return saved.ToAdminResponse();
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
            return Result.Deleted;
        }

        db.Institutions.Remove(institution);
        await db.SaveChangesAsync(ct);
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
            Ects = request.Ects,
            LecturesH = request.LecturesH,
            AuditoryH = request.AuditoryH,
            LabH = request.LabH,
            Semester = semester,
            Level = level,
        };
        db.PartnerCourses.Add(course);
        await db.SaveChangesAsync(ct);
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
        course.Ects = request.Ects;
        course.LecturesH = request.LecturesH;
        course.AuditoryH = request.AuditoryH;
        course.LabH = request.LabH;
        course.Semester = semester;
        course.Level = level;
        await db.SaveChangesAsync(ct);
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
            return Result.Deleted;
        }

        db.PartnerCourses.Remove(course);
        await db.SaveChangesAsync(ct);
        return Result.Deleted;
    }

    public async Task<ErrorOr<Updated>> RestorePartnerCourseAsync(int courseId, CancellationToken ct = default)
    {
        var course = await db.PartnerCourses.FindAsync([courseId], ct);
        if (course is null) return Error.NotFound("COURSE_NOT_FOUND", "Course not found.");

        course.IsDeleted = false;
        course.DeletedAt = null;
        await db.SaveChangesAsync(ct);
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
        return primary.ToResponse();
    }

    #endregion
}
