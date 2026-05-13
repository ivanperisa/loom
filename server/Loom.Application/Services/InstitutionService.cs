using ErrorOr;
using Loom.Application.DTOs.Auth;
using Loom.Application.DTOs.Institution;
using Loom.Application.DTOs.LearningAgreement;
using Loom.Application.Interfaces;
using Loom.Application.Interfaces.Services;
using Loom.Application.Mappers;
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
}
