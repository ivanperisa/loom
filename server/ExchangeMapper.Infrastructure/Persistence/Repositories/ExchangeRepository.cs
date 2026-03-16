using ExchangeMapper.Application.Interfaces.Repositories;
using ExchangeMapper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMapper.Infrastructure.Persistence.Repositories;

public class ExchangeRepository(AppDbContext context) : Repository<Exchange>(context), IExchangeRepository
{
    public override async Task<Exchange?> GetByIdAsync(Guid exchangeId, CancellationToken ct = default)
    {
        return await DbSet
            .Include(e => e.ForeignInstitution)
            .Include(e => e.ExchangeCourses)
                .ThenInclude(c => c.CourseMappings)
                    .ThenInclude(m => m.Course)
            .FirstOrDefaultAsync(e => e.Id == exchangeId, ct);
    }

    public async Task<Exchange?> GetByStudentIdAsync(Guid studentId, CancellationToken ct = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(e => e.ForeignInstitution)
            .Include(e => e.ExchangeCourses)
                .ThenInclude(c => c.CourseMappings)
                    .ThenInclude(m => m.Course)
            .FirstOrDefaultAsync(e => e.StudentId == studentId, ct);
    }

    public async Task<bool> ExistsForStudentAsync(Guid studentId, CancellationToken ct = default)
    {
        return await DbSet.AnyAsync(e => e.StudentId == studentId, ct);
    }

    public async Task<bool> ExistsForUserInstitutionAsync(Guid userInstitutionId, CancellationToken ct = default)
    {
        return await DbSet.AnyAsync(e => e.UserInstitutionId == userInstitutionId, ct);
    }

    public async Task<List<Exchange>> GetAllWithStudentAsync(CancellationToken ct = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.ForeignInstitution)
            .OrderBy(e => e.Student.Name)
            .ToListAsync(ct);
    }
}
