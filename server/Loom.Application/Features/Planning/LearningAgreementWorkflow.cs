using ErrorOr;
using Loom.Application.Common;
using Loom.Application.Common.Security;
using Loom.Application.Features.Exchanges;
using Loom.Application.Interfaces;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Planning;

/// <summary>
/// Draft ⇄ Approved, decided by the exchange's assigned coordinator. Approving signs the agreement and
/// records an approval snapshot; reopening (back to Draft) clears the signature so an amendment can be made.
/// </summary>
public sealed class LearningAgreementWorkflow(IAppDbContext db, ExchangeAccess access, ICurrentActor actor, LaSnapshots snapshots)
{
    public async Task<ErrorOr<ExchangeResponse>> SetStatusAsync(Guid exchangeGuid, UpdateLearningAgreementStatusRequest request, CancellationToken ct)
    {
        var exchangeId = await access.ResolveIdAsync(exchangeGuid, ct);
        if (exchangeId.IsError) return exchangeId.Errors;

        if (!Enum.TryParse<DocumentStatus>(request.Status, out var status) || status is not (DocumentStatus.Draft or DocumentStatus.Approved))
            return PlanningErrors.InvalidStatus;

        var exchange = await db.Exchanges.FirstAsync(e => e.Id == exchangeId.Value, ct);
        if (exchange.CoordinatorId != actor.UserId) return PlanningErrors.NotAssignedCoordinator;

        var learningAgreement = await db.LearningAgreements.FirstOrDefaultAsync(l => l.ExchangeId == exchange.Id, ct);
        if (learningAgreement is null) return PlanningErrors.NotFound;
        if (learningAgreement.Status == status) return PlanningErrors.StatusUnchanged(status);

        learningAgreement.Status = status;
        learningAgreement.LastModifiedById = actor.UserId;
        if (status == DocumentStatus.Approved)
        {
            learningAgreement.SignedAt = DateTime.UtcNow;
            learningAgreement.SignedById = actor.UserId;
            exchange.CoordinatorMessage = null;
            snapshots.Add(exchange.Id, SnapshotType.Auto, await snapshots.CaptureAsync(exchange.Id, ct), actor.UserId);
        }
        else
        {
            learningAgreement.SignedAt = null;
            learningAgreement.SignedById = null;
            if (request.Message is not null) exchange.CoordinatorMessage = request.Message.NullIfBlank();
        }

        exchange.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await db.Exchanges.AsNoTracking().Where(e => e.Id == exchange.Id).Select(ExchangeProjections.Detail).FirstAsync(ct);
    }
}
