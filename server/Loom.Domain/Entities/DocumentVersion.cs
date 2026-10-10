using Loom.Domain.Common;
using Loom.Domain.Enums;

namespace Loom.Domain.Entities;

/// <summary>
/// A frozen copy of a document: every approval (numbered 1, 2, … per document) and backups before import/restore.
/// The payload holds ids plus display copies, so it still reads correctly after courses are renamed.
/// </summary>
public class DocumentVersion : EntityBase
{
    public int ExchangeId { get; set; }
    public Exchange Exchange { get; set; } = null!;

    public DocumentKind Document { get; set; }
    public VersionKind Kind { get; set; }

    /// <summary>1 = the original approval, 2 = amendment A1, … Null for backups.</summary>
    public int? VersionNo { get; set; }

    /// <summary>Payload format. 0 = carried over from the old snapshots (recognition: labels only).</summary>
    public int SchemaVersion { get; set; }
    public string Payload { get; set; } = null!;
    public string ContentHash { get; set; } = null!;

    public int? CreatedById { get; set; }
    public User? CreatedBy { get; set; }
}
