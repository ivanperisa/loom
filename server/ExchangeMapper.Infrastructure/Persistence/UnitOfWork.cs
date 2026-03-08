using ExchangeMapper.Application.Interfaces;
using ExchangeMapper.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMapper.Infrastructure.Persistence;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    { 
        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.Now;
            }
        }
        return context.SaveChangesAsync(ct);
    }
}
