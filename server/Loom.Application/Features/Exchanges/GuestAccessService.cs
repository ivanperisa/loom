using ErrorOr;
using Loom.Application.Common.Errors;
using Loom.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Exchanges;

/// <summary>
/// Access links: anyone with the exchange GUID acts as its placeholder student.
/// Only works while the student has no account. (Phase 4 replaces this with revocable tokens and a guest session.)
/// </summary>
public sealed class GuestAccessService(IAppDbContext db)
{
    public async Task<ErrorOr<int>> ResolvePlaceholderStudentAsync(Guid exchangeGuid, CancellationToken ct)
    {
        var exchange = await db.Exchanges
            .Where(e => e.Guid == exchangeGuid)
            .Select(e => new { e.StudentId, IsPlaceholder = e.Student.Email == "" })
            .FirstOrDefaultAsync(ct);

        if (exchange is null) return CommonErrors.ExchangeNotFound;
        if (!exchange.IsPlaceholder) return CommonErrors.AccessDenied;
        return exchange.StudentId;
    }
}
