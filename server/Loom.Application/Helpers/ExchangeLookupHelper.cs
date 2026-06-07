using ErrorOr;
using Loom.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Helpers;

public static class ExchangeLookupHelper
{
    public static async Task<ErrorOr<int>> ResolveExchangeIdAsync(this IAppDbContext db, Guid guid, CancellationToken ct = default)
    {
        var id = await db.Exchanges.Where(e => e.Guid == guid).Select(e => e.Id).FirstOrDefaultAsync(ct);
        return id == 0 ? Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.") : id;
    }
}
