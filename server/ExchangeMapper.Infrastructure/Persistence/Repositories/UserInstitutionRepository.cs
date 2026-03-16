using ExchangeMapper.Application.Interfaces.Repositories;
using ExchangeMapper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMapper.Infrastructure.Persistence.Repositories;

public class UserInstitutionRepository(AppDbContext context) : Repository<UserInstitution>(context), IUserInstitutionRepository
{
    public async Task<List<UserInstitution>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(ui => ui.Institution)
            .Include(ui => ui.StudyProfile)
                .ThenInclude(sp => sp!.StudyProgram)
            .Include(ui => ui.Exchanges)
            .Where(ui => ui.UserId == userId)
            .OrderBy(ui => ui.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<UserInstitution?> GetHomeByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await DbSet
            .Include(ui => ui.Institution)
            .FirstOrDefaultAsync(ui => ui.UserId == userId && ui.Institution.IsHome, ct);
    }
}
