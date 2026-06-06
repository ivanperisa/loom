using ErrorOr;
using Loom.Application.DTOs.Auth;
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

    public async Task<ErrorOr<List<PartnerProgramResponse>>> GetPartnerProgramsAsync(CancellationToken ct = default)
    {
        var profiles = await db.PartnerPrograms
            .AsNoTracking()
            .Include(p => p.Institution)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
        return profiles.Select(p => p.ToResponse()).ToList();
    }

    public async Task<ErrorOr<List<PartnerCourseResponse>>> GetPartnerCoursesAsync(int partnerProgramId, CancellationToken ct = default)
    {
        var courses = await db.PartnerCourses
            .AsNoTracking()
            .Where(c => c.Program.Id == partnerProgramId)
            .OrderBy(c => c.Code)
            .ToListAsync(ct);
        return courses.Select(c => c.ToResponse()).ToList();
    }

    public async Task<ErrorOr<List<AuthMeResponse>>> GetCoordinatorsAsync(CancellationToken ct = default)
    {
        var coordinators = await db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Coordinator || u.Role == UserRole.Admin)
            .OrderBy(u => u.Name)
            .ToListAsync(ct);
        return coordinators.Select(u => u.ToAuthMeResponse()).ToList();
    }

    public async Task<ErrorOr<List<PartnerInstitutionAdminResponse>>> GetPartnerInstitutionsAdminAsync(CancellationToken ct = default)
    {
        var institutions = await db.Institutions
            .AsNoTracking()
            .Where(i => i.Type == InstitutionType.Partner)
            .Include(i => i.PartnerPrograms)
                .ThenInclude(p => p.Courses)
            .OrderBy(i => i.Country)
            .ThenBy(i => i.Name)
            .ToListAsync(ct);
        return institutions.Select(i => i.ToAdminResponse()).ToList();
    }

    public async Task<ErrorOr<PartnerInstitutionAdminResponse>> CreatePartnerInstitutionAsync(CreatePartnerInstitutionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Error.Validation("INVALID_NAME", "Institution name is required.");
        if (string.IsNullOrWhiteSpace(request.Country))
            return Error.Validation("INVALID_COUNTRY", "Country is required.");

        var institution = new Institution
        {
            Name = request.Name.Trim(),
            NameEn = string.IsNullOrWhiteSpace(request.NameEn) ? request.Name.Trim() : request.NameEn.Trim(),
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
            .Include(i => i.PartnerPrograms)
                .ThenInclude(p => p.Courses)
            .FirstAsync(ct);
        return saved.ToAdminResponse();
    }

    public async Task<ErrorOr<Deleted>> DeletePartnerInstitutionAsync(int institutionId, CancellationToken ct = default)
    {
        var institution = await db.Institutions
            .Include(i => i.PartnerPrograms)
            .FirstOrDefaultAsync(i => i.Id == institutionId && i.Type == InstitutionType.Partner, ct);
        if (institution is null) return Error.NotFound("INSTITUTION_NOT_FOUND", "Institution not found.");

        var hasExchanges = await db.Exchanges.AnyAsync(e => e.PartnerProgram.InstitutionId == institutionId, ct);
        if (hasExchanges) return Error.Conflict("HAS_EXCHANGES", "Cannot delete institution with active exchanges.");

        db.Institutions.Remove(institution);
        await db.SaveChangesAsync(ct);
        return Result.Deleted;
    }

    public async Task<ErrorOr<PartnerProgramAdminResponse>> CreatePartnerProgramAsync(int institutionId, CreatePartnerProgramRequest request, CancellationToken ct = default)
    {
        var institution = await db.Institutions.FindAsync([institutionId], ct);
        if (institution is null || institution.Type != InstitutionType.Partner)
            return Error.NotFound("INSTITUTION_NOT_FOUND", "Partner institution not found.");
        if (string.IsNullOrWhiteSpace(request.Name))
            return Error.Validation("INVALID_NAME", "Program name is required.");
        if (!Enum.TryParse<StudyProgramLevel>(request.Level, out var level))
            return Error.Validation("INVALID_LEVEL", "Invalid study program level.");

        var program = new PartnerProgram
        {
            InstitutionId = institutionId,
            Name = request.Name.Trim(),
            NameEn = string.IsNullOrWhiteSpace(request.NameEn) ? null : request.NameEn.Trim(),
            Level = level,
        };
        db.PartnerPrograms.Add(program);
        await db.SaveChangesAsync(ct);

        var saved = await db.PartnerPrograms
            .AsNoTracking()
            .Include(p => p.Courses)
            .FirstAsync(p => p.Id == program.Id, ct);
        return saved.ToAdminResponse();
    }

    public async Task<ErrorOr<Deleted>> DeletePartnerProgramAsync(int programId, CancellationToken ct = default)
    {
        var program = await db.PartnerPrograms.FindAsync([programId], ct);
        if (program is null) return Error.NotFound("PROGRAM_NOT_FOUND", "Program not found.");

        var hasExchanges = await db.Exchanges.AnyAsync(e => e.PartnerProgramId == programId, ct);
        if (hasExchanges) return Error.Conflict("HAS_EXCHANGES", "Cannot delete program with active exchanges.");

        db.PartnerPrograms.Remove(program);
        await db.SaveChangesAsync(ct);
        return Result.Deleted;
    }

    public async Task<ErrorOr<PartnerCourseResponse>> CreatePartnerCourseAsync(int programId, CreatePartnerCourseRequest request, CancellationToken ct = default)
    {
        var program = await db.PartnerPrograms.FindAsync([programId], ct);
        if (program is null) return Error.NotFound("PROGRAM_NOT_FOUND", "Program not found.");
        if (string.IsNullOrWhiteSpace(request.Code))
            return Error.Validation("INVALID_CODE", "Course code is required.");
        if (string.IsNullOrWhiteSpace(request.NameEn))
            return Error.Validation("INVALID_NAME", "Course name is required.");

        var course = new PartnerCourse
        {
            ProgramId = programId,
            Code = request.Code.Trim(),
            NameEn = request.NameEn.Trim(),
            NameHr = string.IsNullOrWhiteSpace(request.NameHr) ? null : request.NameHr.Trim(),
            Ects = request.Ects,
            LecturesH = request.LecturesH,
            AuditoryH = request.AuditoryH,
            LabH = request.LabH,
        };
        db.PartnerCourses.Add(course);
        await db.SaveChangesAsync(ct);
        return course.ToResponse();
    }

    public async Task<ErrorOr<Deleted>> DeletePartnerCourseAsync(int courseId, CancellationToken ct = default)
    {
        var course = await db.PartnerCourses.FindAsync([courseId], ct);
        if (course is null) return Error.NotFound("COURSE_NOT_FOUND", "Course not found.");

        db.PartnerCourses.Remove(course);
        await db.SaveChangesAsync(ct);
        return Result.Deleted;
    }
}
