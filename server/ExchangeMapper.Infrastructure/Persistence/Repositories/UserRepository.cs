using ExchangeMapper.Application.Interfaces.Repositories;
using ExchangeMapper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMapper.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext context) : Repository<User>(context), IUserRepository
{
    public async Task<User?> GetByExternalIdAsync(string externalId, CancellationToken ct = default)
    {
        return await DbSet.AsNoTracking().FirstOrDefaultAsync(x => x.ExternalId == externalId, ct);
    }

    public async Task<User?> GetByExternalIdWithDetailsAsync(string externalId, CancellationToken ct = default)
    {
        return await DbSet
            .AsNoTracking()
            .Include(u => u.UserInstitutions)
                .ThenInclude(ui => ui.Institution)
            .Include(u => u.UserInstitutions)
                .ThenInclude(ui => ui.StudyProfile)
                    .ThenInclude(sp => sp!.StudyProgram)
            .Include(u => u.UserInstitutions)
                .ThenInclude(ui => ui.Exchanges)
            .FirstOrDefaultAsync(u => u.ExternalId == externalId, ct);
    }
}
