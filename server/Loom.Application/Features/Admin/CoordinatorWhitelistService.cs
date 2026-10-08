using ErrorOr;
using Loom.Application.Interfaces;
using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Admin;

/// <summary>Emails that become coordinators automatically on their first login.</summary>
public sealed class CoordinatorWhitelistService(IAppDbContext db)
{
    public Task<List<CoordinatorWhitelistEntryResponse>> ListAsync(CancellationToken ct) =>
        db.CoordinatorWhitelist
            .AsNoTracking()
            .OrderBy(e => e.Email)
            .Select(e => new CoordinatorWhitelistEntryResponse(e.Id, e.Email, e.CreatedAt))
            .ToListAsync(ct);

    public async Task<ErrorOr<CoordinatorWhitelistEntryResponse>> AddAsync(string email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email)) return AdminErrors.EmailRequired;

        var normalized = Normalize(email);
        if (await db.CoordinatorWhitelist.AnyAsync(e => e.Email == normalized, ct)) return AdminErrors.AlreadyWhitelisted;

        var entry = new CoordinatorWhitelist { Email = normalized };
        db.CoordinatorWhitelist.Add(entry);
        await db.SaveChangesAsync(ct);
        return new CoordinatorWhitelistEntryResponse(entry.Id, entry.Email, entry.CreatedAt);
    }

    public async Task<ErrorOr<Deleted>> RemoveAsync(string email, CancellationToken ct)
    {
        var normalized = Normalize(email);
        var entry = await db.CoordinatorWhitelist.FirstOrDefaultAsync(e => e.Email == normalized, ct);
        if (entry is null) return AdminErrors.NotWhitelisted;

        db.CoordinatorWhitelist.Remove(entry);
        await db.SaveChangesAsync(ct);
        return Result.Deleted;
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
