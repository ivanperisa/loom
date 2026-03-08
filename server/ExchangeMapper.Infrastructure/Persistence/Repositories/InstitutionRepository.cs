using ExchangeMapper.Application.Interfaces.Repositories;
using ExchangeMapper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMapper.Infrastructure.Persistence.Repositories;

public class InstitutionRepository(AppDbContext context) : IInstitutionRepository
{
    public async Task<List<Institution>> GetHomeInstitutionsAsync(CancellationToken ct = default)
    {
        return await context.Institutions
            .AsNoTracking()
            .Where(x => x.IsHome)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
    }

    public async Task<Institution?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Institutions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task AddAsync(Institution institution)
    {
        context.Institutions.Add(institution);
        return Task.CompletedTask;
    }
}
