namespace Loom.Application.Features.Planning;

public record LearningAgreementEntryResponse(
    int Id,
    int HomeSlotId,
    string Mode,
    int? PartnerCourseId,
    string? PartnerCourseCode,
    string? PartnerCourseName,
    string? PartnerCourseNameHr,
    string? PartnerCourseUrl,
    decimal? AwardedEcts,
    /// <summary>Taken out: pending removal in this draft, or removed in an approved amendment.</summary>
    bool IsDeleted,
    /// <summary>
    /// Live rows: the amendment that added it (0 = original agreement). Deleted rows: the amendment that removed it.
    /// Null while the change awaits approval (the client shows it as part of the next amendment).
    /// </summary>
    int? AmendmentNumber);
