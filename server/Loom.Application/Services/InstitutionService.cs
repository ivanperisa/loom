using ErrorOr;
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
}
