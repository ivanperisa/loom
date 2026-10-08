namespace Loom.Application.Features.Planning;

/// <summary>
/// What importing a file would do to the draft. Import replaces the draft: rows missing from the file are removed.
/// Nothing is stored; apply sends the same file again and gets the same result.
/// </summary>
public record ImportPreviewResponse(
    bool CanApply,
    /// <summary>Why it cannot be applied (an error code such as LA_LOCKED or ECTS_EXCEEDED), with a message.</summary>
    string? BlockingCode,
    string? BlockingMessage,
    List<ImportContextWarning> ContextWarnings,
    List<ImportRow> Added,
    List<ImportRow> Removed,
    List<ImportRow> Changed,
    int Unchanged,
    List<ImportSkip> Skipped);

public record ImportContextWarning(string Field, string FromFile, string InExchange);

public record ImportRow(
    int HomeSlotId,
    string HomeSlotLabel,
    string Mode,
    int? PartnerCourseId,
    string? PartnerCourseCode,
    string? PartnerCourseName,
    decimal? AwardedEcts,
    decimal? PreviousEcts = null,
    string? PreviousMode = null);

/// <summary>A file row that is left out, with why: SlotNotInProfile, CourseNotFound, AmbiguousCourse, InvalidMode, Duplicate.</summary>
public record ImportSkip(int HomeSlotId, string HomeSlotLabel, string? PartnerCourseCode, string Reason);

public record ImportResult(int Added, int Removed, int Changed, List<ImportSkip> Skipped);
