using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Loom.Application.Common;
using Loom.Application.Interfaces;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Documents;

/// <summary>Writes and reads <see cref="DocumentVersion"/> rows. Callers save the context.</summary>
public sealed class VersionStore(IAppDbContext db)
{
    public const int CurrentSchema = 1;

    public static string Serialize<T>(T payload) => JsonSerializer.Serialize(payload, JsonHelper.DefaultOptions);

    public static T? Deserialize<T>(DocumentVersion version) where T : class
    {
        try { return JsonSerializer.Deserialize<T>(version.Payload, JsonHelper.DefaultOptions); }
        catch (JsonException) { return null; }
    }

    /// <summary>Hash of the content that matters, so "nothing changed" can be detected without comparing JSON text.</summary>
    public static string Hash(IEnumerable<string> canonicalRows) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", canonicalRows.Order(StringComparer.Ordinal))))).ToLowerInvariant();

    public Task<DocumentVersion?> LatestApprovedAsync(int exchangeId, DocumentKind document, CancellationToken ct) =>
        db.DocumentVersions
            .Where(v => v.ExchangeId == exchangeId && v.Document == document && v.Kind == VersionKind.Approved)
            .OrderByDescending(v => v.VersionNo)
            .FirstOrDefaultAsync(ct);

    public async Task<int> ApprovedCountAsync(int exchangeId, DocumentKind document, CancellationToken ct) =>
        await db.DocumentVersions
            .Where(v => v.ExchangeId == exchangeId && v.Document == document && v.Kind == VersionKind.Approved)
            .MaxAsync(v => v.VersionNo, ct) ?? 0;

    public DocumentVersion AddApproved(int exchangeId, DocumentKind document, int versionNo, string payload, string hash, int actorId)
    {
        var version = new DocumentVersion
        {
            ExchangeId = exchangeId, Document = document, Kind = VersionKind.Approved, VersionNo = versionNo,
            SchemaVersion = CurrentSchema, Payload = payload, ContentHash = hash, CreatedById = actorId,
        };
        db.DocumentVersions.Add(version);
        return version;
    }

    /// <summary>Restore point before the draft is replaced. Skipped when the draft is empty or already saved as the latest backup or approval.</summary>
    public async Task AddBackupAsync(int exchangeId, DocumentKind document, string payload, string hash, bool isEmpty, int actorId, CancellationToken ct)
    {
        if (isEmpty) return;
        var alreadyKept = await db.DocumentVersions
            .Where(v => v.ExchangeId == exchangeId && v.Document == document)
            .OrderByDescending(v => v.CreatedAt)
            .Take(1)
            .AnyAsync(v => v.ContentHash == hash, ct);
        if (alreadyKept) return;

        db.DocumentVersions.Add(new DocumentVersion
        {
            ExchangeId = exchangeId, Document = document, Kind = VersionKind.Backup, VersionNo = null,
            SchemaVersion = CurrentSchema, Payload = payload, ContentHash = hash, CreatedById = actorId,
        });
    }

    /// <summary>"A1" for version 2 and so on; the original approval has no label.</summary>
    public static string? AmendmentLabel(int? versionNo) => versionNo is > 1 ? $"A{versionNo - 1}" : null;
}
