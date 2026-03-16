using ExchangeMapper.Application.Interfaces.Repositories;
using ExchangeMapper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMapper.Infrastructure.Persistence.Repositories;

public class InstitutionRepository(AppDbContext context) : Repository<Institution>(context), IInstitutionRepository
{
    public async Task<List<Institution>> GetHomeInstitutionsAsync(CancellationToken ct = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(x => x.IsHome)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }

    public async Task<List<Institution>> GetForeignInstitutionsAsync(CancellationToken ct = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(i => !i.IsHome)
            .OrderBy(i => i.Name)
            .ToListAsync(ct);
    }

    public override async Task<Institution?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await DbSet.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
    }
}
