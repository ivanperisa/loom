using ErrorOr;
using Loom.Application.Common.Errors;
using Loom.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Common.Security;

/// <summary>The exchange a request is about, plus what the current actor is to it.</summary>
public sealed record ExchangeContext(
    int ExchangeId,
    Guid Guid,
    int StudentId,
    int? CoordinatorId,
    int HomeProfileId,
    int PartnerInstitutionId,
    bool IsStudent,
    bool IsAssignedCoordinator);

/// <summary>Resolves an exchange by GUID and checks that the actor is its student or assigned coordinator, in one query.</summary>
public sealed class ExchangeAccess(IAppDbContext db, ICurrentActor actor)
{
    public async Task<ErrorOr<ExchangeContext>> LoadAsync(Guid exchangeGuid, CancellationToken ct)
    {
        var exchange = await db.Exchanges
            .Where(e => e.Guid == exchangeGuid)
            .Select(e => new { e.Id, e.StudentId, e.CoordinatorId, e.HomeProfileId, e.PartnerInstitutionId })
            .FirstOrDefaultAsync(ct);
        if (exchange is null) return CommonErrors.ExchangeNotFound;
        // A guest's link opens one exchange, even if the placeholder student has others.
        if (actor.GuestExchangeId is { } guestExchangeId && guestExchangeId != exchange.Id) return CommonErrors.AccessDenied;

        var isStudent = exchange.StudentId == actor.UserId;
        var isCoordinator = exchange.CoordinatorId == actor.UserId;
        if (!isStudent && !isCoordinator) return CommonErrors.AccessDenied;

        return new ExchangeContext(exchange.Id, exchangeGuid, exchange.StudentId, exchange.CoordinatorId,
            exchange.HomeProfileId, exchange.PartnerInstitutionId, isStudent, isCoordinator);
    }

    /// <summary>Exchange id for a GUID without an access check (for steps that check access differently).</summary>
    public async Task<ErrorOr<int>> ResolveIdAsync(Guid exchangeGuid, CancellationToken ct)
    {
        var id = await db.Exchanges.Where(e => e.Guid == exchangeGuid).Select(e => e.Id).FirstOrDefaultAsync(ct);
        return id == 0 ? CommonErrors.ExchangeNotFound : id;
    }
}
