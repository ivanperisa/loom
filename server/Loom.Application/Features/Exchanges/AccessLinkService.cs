using ErrorOr;
using Loom.Application.Common.Errors;
using Loom.Application.Common.Security;
using Loom.Application.Interfaces;
using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Exchanges;

/// <summary>A guest session opened from an access link. Re-checked on every request.</summary>
public sealed record GuestSession(int LinkId, int ExchangeId, Guid ExchangeGuid, int StudentId);

/// <summary>
/// Access links for placeholder students (no account yet). The coordinator creates and rotates the link;
/// whoever opens it gets a guest session for that one exchange, until the link is revoked or claimed.
/// </summary>
public sealed class AccessLinkService(IAppDbContext db, ICurrentActor actor, ExchangeAccess access)
{
    /// <summary>The exchange's live link, created on first use.</summary>
    public async Task<ErrorOr<AccessLinkResponse>> GetOrCreateAsync(Guid exchangeGuid, CancellationToken ct)
    {
        var exchangeId = await CoordinatedPlaceholderExchangeAsync(exchangeGuid, ct);
        if (exchangeId.IsError) return exchangeId.Errors;

        var link = await db.ExchangeAccessLinks.FirstOrDefaultAsync(l => l.ExchangeId == exchangeId.Value && l.RevokedAt == null, ct)
            ?? await CreateAsync(exchangeId.Value, ct);
        return new AccessLinkResponse(link.Token, link.CreatedAt);
    }

    /// <summary>Revokes the live link (anyone holding it loses access at once) and issues a new one.</summary>
    public async Task<ErrorOr<AccessLinkResponse>> RegenerateAsync(Guid exchangeGuid, CancellationToken ct)
    {
        var exchangeId = await CoordinatedPlaceholderExchangeAsync(exchangeGuid, ct);
        if (exchangeId.IsError) return exchangeId.Errors;

        await RevokeAllAsync([exchangeId.Value], ct);
        var link = await CreateAsync(exchangeId.Value, ct);
        return new AccessLinkResponse(link.Token, link.CreatedAt);
    }

    public Task<ErrorOr<GuestSession>> OpenAsync(string token, CancellationToken ct) =>
        FindSessionAsync(db.ExchangeAccessLinks.Where(l => l.Token == token), ct);

    public Task<ErrorOr<GuestSession>> ResumeAsync(int linkId, CancellationToken ct) =>
        FindSessionAsync(db.ExchangeAccessLinks.Where(l => l.Id == linkId), ct);

    /// <summary>What a signed-in user sees when they open a link: go to the exchange, or claim it.</summary>
    public async Task<ErrorOr<AccessLinkPreviewResponse>> PreviewAsync(string token, CancellationToken ct)
    {
        var session = await OpenAsync(token, ct);
        if (session.IsError) return session.Errors;

        var exchange = await db.Exchanges.AsNoTracking()
            .Where(e => e.Id == session.Value.ExchangeId)
            .Select(e => new { e.CoordinatorId, StudentName = e.Student.Name, PartnerInstitution = e.PartnerInstitution.Name, e.AcademicYear })
            .SingleAsync(ct);
        var me = await db.Users.AsNoTracking().Where(u => u.Id == actor.UserId).Select(u => new { u.Role }).SingleAsync(ct);

        var isCoordinator = exchange.CoordinatorId == actor.UserId;
        return new AccessLinkPreviewResponse(
            session.Value.ExchangeGuid,
            exchange.StudentName,
            exchange.PartnerInstitution,
            exchange.AcademicYear,
            HasAccess: isCoordinator,
            CanClaim: !isCoordinator && me.Role == Domain.Enums.UserRole.Student);
    }

    public async Task RevokeAllAsync(IReadOnlyCollection<int> exchangeIds, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await db.ExchangeAccessLinks
            .Where(l => exchangeIds.Contains(l.ExchangeId) && l.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.RevokedAt, now), ct);
    }

    private async Task<ErrorOr<GuestSession>> FindSessionAsync(IQueryable<ExchangeAccessLink> links, CancellationToken ct)
    {
        var session = await links
            .Where(l => l.RevokedAt == null && l.Exchange.Student.Email == "")   // placeholder: no account yet
            .Select(l => new GuestSession(l.Id, l.ExchangeId, l.Exchange.Guid, l.Exchange.StudentId))
            .FirstOrDefaultAsync(ct);
        return session is null ? AccessLinkErrors.Invalid : session;
    }

    private async Task<ExchangeAccessLink> CreateAsync(int exchangeId, CancellationToken ct)
    {
        var link = new ExchangeAccessLink { ExchangeId = exchangeId, Token = ExchangeAccessLink.NewToken(), CreatedById = actor.UserId };
        db.ExchangeAccessLinks.Add(link);
        await db.SaveChangesAsync(ct);
        return link;
    }

    private async Task<ErrorOr<int>> CoordinatedPlaceholderExchangeAsync(Guid exchangeGuid, CancellationToken ct)
    {
        var context = await access.LoadAsync(exchangeGuid, ct);
        if (context.IsError) return context.Errors;
        if (!context.Value.IsAssignedCoordinator || actor.IsGuest) return AccessLinkErrors.OnlyCoordinator;

        var isPlaceholder = await db.Users.AnyAsync(u => u.Id == context.Value.StudentId && u.Email == "", ct);
        if (!isPlaceholder) return AccessLinkErrors.StudentRegistered;
        return context.Value.ExchangeId;
    }
}
