using ErrorOr;
using ExchangeMapper.Application.DTOs.Auth;
using ExchangeMapper.Application.DTOs.CourseSlot;
using ExchangeMapper.Application.DTOs.Institution;
using ExchangeMapper.Application.Interfaces;
using ExchangeMapper.Application.Interfaces.Services;
using ExchangeMapper.Application.Mappers;
using ExchangeMapper.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMapper.Application.Services;

public class InstitutionService(IAppDbContext db) : IInstitutionService
{
    public async Task<ErrorOr<List<InstitutionResponse>>> GetHomeInstitutionsAsync(CancellationToken ct = default)
    {
        var institutions = await db.Institutions
            .AsNoTracking()
            .Where(x => x.IsHome)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
        return institutions.Select(i => i.ToResponse()).ToList();
    }

    public async Task<ErrorOr<List<ForeignProgramResponse>>> GetForeignProgramsAsync(CancellationToken ct = default)
    {
        var programs = await db.ForeignPrograms
            .AsNoTracking()
            .Include(p => p.Institution)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
        return programs.Select(p => p.ToResponse()).ToList();
    }

    public async Task<ErrorOr<List<StudyProgramResponse>>> GetStudyProgramsAsync(CancellationToken ct = default)
    {
        var programs = await db.StudyPrograms
            .AsNoTracking()
            .Include(p => p.StudyProfiles)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
        return programs.Select(p => p.ToResponse()).ToList();
    }

    public async Task<ErrorOr<List<ForeignCourseResponse>>> GetForeignCoursesAsync(Guid foreignProgramId, CancellationToken ct = default)
    {
        var courses = await db.ForeignCourses
            .AsNoTracking()
            .Where(c => c.ForeignProgramId == foreignProgramId)
            .OrderBy(c => c.Code)
            .ToListAsync(ct);
        return courses.Select(c => c.ToResponse()).ToList();
    }

    public async Task<ErrorOr<List<AuthMeResponse>>> GetCoordinatorsAsync(CancellationToken ct = default)
    {
        var coordinators = await db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Coordinator)
            .OrderBy(u => u.Name)
            .ToListAsync(ct);
        return coordinators.Select(u => u.ToAuthMeResponse()).ToList();
    }
}
