using ErrorOr;
using Loom.Application.Common.Errors;
using Loom.Application.Common.Security;
using Loom.Application.Features.Exchanges;
using Loom.Application.Interfaces;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Users;

public record ClaimResponse(Guid ExchangeGuid);

/// <summary>
/// A signed-in student takes over the placeholder their coordinator prepared. The access link is the proof:
/// only the person the coordinator sent it to has it. Knowing a JMBAG is not enough.
/// </summary>
public sealed class PlaceholderClaimService(IAppDbContext db, ICurrentActor actor, AccessLinkService links)
{
    public async Task<ErrorOr<ClaimResponse>> ClaimAsync(string token, CancellationToken ct)
    {
        var session = await links.OpenAsync(token, ct);
        if (session.IsError) return session.Errors;

        var user = await db.Users.FindAsync([actor.UserId], ct);
        if (user is null) return CommonErrors.UserNotFound;
        if (user.Role != UserRole.Student || user.IsPlaceholder) return AccessLinkErrors.OnlyStudentsClaim;

        var placeholder = await db.Users.SingleAsync(u => u.Id == session.Value.StudentId, ct);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        // The person, not just this exchange: every exchange the coordinator prepared for them moves over.
        var exchanges = await db.Exchanges.Where(e => e.StudentId == placeholder.Id).ToListAsync(ct);
        foreach (var exchange in exchanges) exchange.StudentId = user.Id;
        await links.RevokeAllAsync(exchanges.Select(e => e.Id).ToList(), ct);

        // Snapshots reference their author without ON DELETE, so they have to move before the placeholder goes.
        await db.ExchangeSnapshots
            .Where(s => s.ChangedById == placeholder.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ChangedById, user.Id), ct);

        user.CoordinatorId ??= placeholder.CoordinatorId;
        user.Mentor ??= placeholder.Mentor;
        if (!user.IsOnboarded && placeholder.InstitutionId is not null)
        {
            user.InstitutionId = placeholder.InstitutionId;
            user.IsOnboarded = true;
        }
        var jmbag = user.Jmbag is null ? placeholder.Jmbag : null;

        db.Users.Remove(placeholder);
        await db.SaveChangesAsync(ct);

        if (jmbag is not null)
        {
            // Separate save: the JMBAG is unique and the placeholder must be gone first.
            user.Jmbag = jmbag;
            await db.SaveChangesAsync(ct);
        }

        await transaction.CommitAsync(ct);
        return new ClaimResponse(session.Value.ExchangeGuid);
    }
}
