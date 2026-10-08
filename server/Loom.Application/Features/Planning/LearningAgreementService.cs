using ErrorOr;
using Loom.Application.Common;
using Loom.Application.Common.Security;
using Loom.Application.Interfaces;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Planning;

/// <summary>Reading and editing the learning agreement (slot ↔ partner course mapping) while it is a draft.</summary>
public sealed class LearningAgreementService(IAppDbContext db, ExchangeAccess access, ICurrentActor actor, LaEntryWriter writer)
{
    public async Task<ErrorOr<LearningAgreementResponse>> GetAsync(Guid exchangeGuid, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        return await BuildResponseAsync(context.Value, ct);
    }

    public async Task<ErrorOr<LearningAgreementResponse>> SaveAsync(Guid exchangeGuid, SaveLearningAgreementRequest request, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        var exchange = context.Value;

        var status = await db.LearningAgreements.Where(l => l.ExchangeId == exchange.ExchangeId).Select(l => (DocumentStatus?)l.Status).FirstOrDefaultAsync(ct);
        if (status is not null and not DocumentStatus.Draft) return PlanningErrors.Locked;

        var valid = await writer.ValidateAsync(request, exchange, ct);
        if (valid.IsError) return valid.Errors;

        var learningAgreement = await writer.GetOrCreateAsync(exchange.ExchangeId, ct);
        var replaced = await writer.ReplaceEntriesAsync(learningAgreement.Id, request, ct);
        if (replaced.IsError) return replaced.Errors;

        learningAgreement.LastModifiedById = actor.UserId;
        await db.SaveChangesAsync(ct);
        return await BuildResponseAsync(exchange, ct);
    }

    public async Task<ErrorOr<LearningAgreementResponse>> UpdateMessageAsync(Guid exchangeGuid, string? message, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;

        var learningAgreement = await db.LearningAgreements.FirstOrDefaultAsync(l => l.ExchangeId == context.Value.ExchangeId, ct);
        if (learningAgreement is null) return PlanningErrors.NotFound;

        learningAgreement.Message = message.NullIfBlank();
        learningAgreement.LastModifiedById = actor.UserId;
        await db.SaveChangesAsync(ct);
        return await BuildResponseAsync(context.Value, ct);
    }

    private async Task<LearningAgreementResponse> BuildResponseAsync(ExchangeContext exchange, CancellationToken ct)
    {
        var slots = await db.HomeSlots
            .AsNoTracking()
            .Include(s => s.SlotType)
            .Include(s => s.Course)
            .Include(s => s.CourseGroup)
            .Where(s => s.ProfileId == exchange.HomeProfileId)
            .OrderBy(s => s.Semester).ThenBy(s => s.SlotPosition)
            .ToListAsync(ct);

        var learningAgreement = await db.LearningAgreements
            .AsNoTracking()
            .Include(l => l.Entries).ThenInclude(e => e.PartnerCourse)
            .Include(l => l.LastModifiedByUser)
            .Include(l => l.SignedByUser)
            .FirstOrDefaultAsync(l => l.ExchangeId == exchange.ExchangeId, ct);

        var approvals = (await db.ExchangeSnapshots
                .AsNoTracking()
                .Where(s => s.ExchangeId == exchange.ExchangeId && s.Phase == SnapshotPhase.LearningAgreement && s.Type == SnapshotType.Auto)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync(ct))
            .Select(LaSnapshots.Read)
            .OfType<LaSnapshotData>()
            .ToList();

        var active = learningAgreement?.Entries.Select(e => e.ToResponse()).ToList() ?? [];

        return new LearningAgreementResponse(
            exchange.ExchangeId,
            (learningAgreement?.Status ?? DocumentStatus.Draft).ToString(),
            learningAgreement?.Message,
            slots.Select(s => s.ToResponse()).ToList(),
            AmendmentHistory.Apply(active, approvals),
            learningAgreement?.UpdatedAt,
            learningAgreement?.LastModifiedByUser?.Name,
            learningAgreement?.SignedAt,
            learningAgreement?.SignedByUser?.Name,
            approvals.Count);
    }
}
