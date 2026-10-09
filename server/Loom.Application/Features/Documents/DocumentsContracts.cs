using Loom.Domain.Enums;

namespace Loom.Application.Features.Documents;

/// <summary>One entry of a version list: an approval (with what changed since the previous one) or a backup.</summary>
public record DocumentVersionResponse(
    int Id,
    VersionKind Kind,
    int? VersionNo,
    /// <summary>"A1", "A2", … for amendments; null for the original approval and for backups.</summary>
    string? AmendmentLabel,
    DateTime CreatedAt,
    string? CreatedByName,
    int EntryCount,
    /// <summary>Changes against the previous approval (the first one against nothing). Null for backups and old-format versions.</summary>
    List<DocumentChange>? Changes);

public record DocumentChange(
    string Type,
    int HomeSlotId,
    string HomeSlotLabel,
    int? PartnerCourseId,
    string? PartnerCourseCode,
    string? PartnerCourseName,
    List<FieldChange> Fields);

public record FieldChange(string Field, string? Before, string? After);
