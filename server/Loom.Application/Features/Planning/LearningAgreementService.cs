using ErrorOr;
using Loom.Application.Common;
using Loom.Application.Common.Security;
using Loom.Application.Features.Documents;
using Loom.Application.Interfaces;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Planning;

/// <summary>Reading and editing the learning agreement (slot ↔ partner course mapping) while it is a draft.</summary>
public sealed class LearningAgreementService(IAppDbContext db, ExchangeAccess access, ICurrentActor actor, LaEntryWriter writer, VersionStore versions)
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

        var editable = await writer.CheckEditableAsync(exchange.ExchangeId, ct);
        if (editable.IsError) return editable.Errors;
        var valid = await writer.ValidateAsync(request, exchange, ct);
        if (valid.IsError) return valid.Errors;

        var learningAgreement = await writer.GetOrCreateAsync(exchange.ExchangeId, ct);
        await writer.ApplyAsync(learningAgreement.Id, request, ct);
        learningAgreement.LastModifiedById = actor.UserId;
        learningAgreement.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await BuildResponseAsync(exchange, ct);
    }

    /// <summary>Notes stay editable in every state (they are feedback, not content).</summary>
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
            .Include(l => l.ConcludedByUser)
            .FirstOrDefaultAsync(l => l.ExchangeId == exchange.ExchangeId, ct);

        var entries = learningAgreement?.Entries
            .OrderBy(e => e.HomeSlotId).ThenBy(e => e.Id)
            .Select(e => e.ToResponse())
            .ToList() ?? [];

        return new LearningAgreementResponse(
            exchange.ExchangeId,
            learningAgreement?.Status ?? DocumentStatus.Draft,
            learningAgreement?.Message,
            slots.Select(s => s.ToResponse()).ToList(),
            entries,
            learningAgreement?.UpdatedAt,
            learningAgreement?.LastModifiedByUser?.Name,
            learningAgreement?.SignedAt,
            learningAgreement?.SignedByUser?.Name,
            await versions.ApprovedCountAsync(exchange.ExchangeId, DocumentKind.LearningAgreement, ct),
            learningAgreement?.IsConcluded ?? false,
            learningAgreement?.ConcludedAt,
            learningAgreement?.ConcludedByUser?.Name);
    }
}
