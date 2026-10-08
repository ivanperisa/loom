using ErrorOr;
using Loom.Application.Common;
using Loom.Application.Common.Security;
using Loom.Application.Features.Documents;
using Loom.Application.Features.Exchanges;
using Loom.Application.Interfaces;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Planning;

/// <summary>
/// Draft ⇄ Approved, decided by the exchange's assigned coordinator. Students never change the status.
/// Approving signs the LA and, when the content changed, creates the next numbered version: version 1 is the original
/// agreement, version n is amendment A(n-1). Reopening (back to Draft) allows an amendment, until final recognition starts.
/// </summary>
public sealed class LearningAgreementWorkflow(IAppDbContext db, ExchangeAccess access, ICurrentActor actor, LaContent content, VersionStore versions)
{
    public async Task<ErrorOr<ExchangeResponse>> SetStatusAsync(Guid exchangeGuid, UpdateLearningAgreementStatusRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<DocumentStatus>(request.Status, out var status) || !Enum.IsDefined(status)) return PlanningErrors.InvalidStatus;

        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        if (!context.Value.IsAssignedCoordinator || actor.IsGuest) return PlanningErrors.NotAssignedCoordinator;
        var exchangeId = context.Value.ExchangeId;

        var learningAgreement = await db.LearningAgreements.FirstOrDefaultAsync(l => l.ExchangeId == exchangeId, ct);
        if (learningAgreement is null) return PlanningErrors.NotFound;
        if (learningAgreement.IsConcluded) return PlanningErrors.Concluded;
        if (learningAgreement.Status == status) return PlanningErrors.StatusUnchanged(status);

        var exchange = await db.Exchanges.FirstAsync(e => e.Id == exchangeId, ct);
        learningAgreement.Status = status;
        learningAgreement.LastModifiedById = actor.UserId;
        if (status == DocumentStatus.Approved)
        {
            learningAgreement.SignedAt = DateTime.UtcNow;
            learningAgreement.SignedById = actor.UserId;
            exchange.CoordinatorMessage = null;
            await RecordVersionAsync(exchangeId, learningAgreement.Id, ct);
        }
        else
        {
            learningAgreement.SignedAt = null;
            learningAgreement.SignedById = null;
            if (request.Message is not null) exchange.CoordinatorMessage = request.Message.NullIfBlank();
        }

        exchange.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await db.Exchanges.AsNoTracking().Where(e => e.Id == exchangeId).Select(ExchangeProjections.Detail).FirstAsync(ct);
    }

    /// <summary>
    /// Stamps the components with the new version (added now / removed now) and freezes a copy.
    /// Approving again without changes keeps the current version: amendment numbers count changes, not clicks.
    /// </summary>
    private async Task RecordVersionAsync(int exchangeId, int learningAgreementId, CancellationToken ct)
    {
        var payload = await content.CaptureAsync(exchangeId, ct);
        var hash = LaContent.Hash(payload);
        var latest = await versions.LatestApprovedAsync(exchangeId, DocumentKind.LearningAgreement, ct);
        var pending = await db.LearningAgreementEntries
            .Where(e => e.LearningAgreementId == learningAgreementId
                && ((!e.IsDeleted && e.AddedInVersion == null) || (e.IsDeleted && e.RemovedInVersion == null)))
            .ToListAsync(ct);
        if (latest?.ContentHash == hash && pending.Count == 0) return;

        var versionNo = (latest?.VersionNo ?? 0) + 1;
        foreach (var entry in pending)
        {
            if (entry.IsDeleted) entry.RemovedInVersion = versionNo;
            else entry.AddedInVersion = versionNo;
        }
        versions.AddApproved(exchangeId, DocumentKind.LearningAgreement, versionNo, VersionStore.Serialize(payload), hash, actor.UserId);
    }
}
