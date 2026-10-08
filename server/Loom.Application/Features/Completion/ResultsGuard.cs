using ErrorOr;
using Loom.Application.Interfaces;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Completion;

/// <summary>The results (table 2 + mapping scheme) can change after final recognition started and until the coordinator approves them.</summary>
public sealed class ResultsGuard(IAppDbContext db)
{
    public async Task<ErrorOr<Recognition>> EditableAsync(int exchangeId, CancellationToken ct)
    {
        var started = await db.LearningAgreements.AnyAsync(l => l.ExchangeId == exchangeId && l.ConcludedAt != null, ct);
        if (!started) return CompletionErrors.NotStarted;

        var recognition = await db.Recognitions.FirstOrDefaultAsync(r => r.ExchangeId == exchangeId, ct);
        if (recognition is null) return CompletionErrors.NotStarted;
        if (recognition.Status != DocumentStatus.Draft) return CompletionErrors.Locked;
        return recognition;
    }
}
