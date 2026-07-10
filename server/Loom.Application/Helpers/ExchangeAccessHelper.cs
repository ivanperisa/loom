using ErrorOr;
using Loom.Application.Interfaces;
using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Helpers;

public static class ExchangeAccessHelper
{
    public static async Task<ErrorOr<(Exchange Exchange, User Requester)>> CheckExchangeAccessAsync(
        this IAppDbContext db, int exchangeId, int requesterId, bool includeStudent = false, CancellationToken ct = default)
    {
        var query = db.Exchanges.AsQueryable();
        if (includeStudent) query = query.Include(e => e.Student);

        var exchange = await query.FirstOrDefaultAsync(e => e.Id == exchangeId, ct);
        if (exchange is null) return Error.NotFound("EXCHANGE_NOT_FOUND", "Exchange not found.");

        var requester = await db.Users.FindAsync([requesterId], ct);
        if (requester is null) return Error.NotFound("USER_NOT_FOUND", "User not found.");

        if (exchange.StudentId != requesterId && !requester.IsCoordinatorFor(exchange.CoordinatorId))
            return Error.Forbidden("ACCESS_DENIED", "Access denied.");

        return (exchange, requester);
    }
}
