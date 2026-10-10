using Loom.Domain.Enums;

namespace Loom.Application.Features.Completion;

/// <summary>
/// The recognition document. Table 1 (<see cref="Agreed"/>) is the latest approved LA version shown as a recognition
/// table: computed, never stored, so it cannot drift. Table 2 (results and grades) is the mapping scheme's data.
/// </summary>
public record RecognitionResponse(
    int ExchangeId,
    DocumentStatus Status,
    string? Message,
    /// <summary>"Start final recognition" was pressed: table 2 and the mapping scheme exist and can be edited.</summary>
    bool IsStarted,
    DateTime? StartedAt,
    string? StartedByName,
    /// <summary>Start is possible now: the LA is approved and final recognition has not started.</summary>
    bool CanStart,
    /// <summary>The LA version table 1 shows (null before the first approval).</summary>
    int? AgreedVersionNo,
    List<AgreedEntryResponse> Agreed,
    DateTime? LastModifiedAt,
    string? LastModifiedByName,
    DateTime? SignedAt,
    string? SignedByName,
    int ApprovedVersionCount);

/// <summary>A row of table 1: one agreed course placement from the approved LA.</summary>
public record AgreedEntryResponse(
    string Id,
    int HomeSlotId,
    int PartnerCourseId,
    string PartnerCourseCode,
    string PartnerCourseName,
    string? PartnerCourseNameHr,
    string? PartnerCourseUrl,
    string? PartnerCourseHours,
    decimal PartnerCourseEcts,
    int? HomeSlotCourseIsvuCode,
    string HomeSlotCourseName,
    int? HomeSlotCourseGroupIsvuCode,
    string HomeSlotCourseGroupName,
    string HomeSlotColor,
    int HomeSlotSemester,
    decimal AwardedEcts);
