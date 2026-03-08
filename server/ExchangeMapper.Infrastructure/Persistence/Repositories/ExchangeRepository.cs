using ExchangeMapper.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMapper.Infrastructure.Persistence.Repositories;

public class ExchangeRepository(AppDbContext context) : IExchangeRepository
{
    public async Task<bool> ExistsForUserInstitutionAsync(Guid userInstitutionId, CancellationToken ct = default)
    {
        return await context.Exchanges.AnyAsync(e => e.UserInstitutionId == userInstitutionId, ct);
    }
}
