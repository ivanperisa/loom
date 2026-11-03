namespace Loom.Application.Features.Planning;

/// <summary>
/// What a learning agreement version stores: ids (what restore uses) plus display copies (what history shows, even after courses
/// are renamed). Property names match the old snapshot JSON, so carried-over versions read the same way.
/// </summary>
public record LearningAgreementVersionPayload(List<LearningAgreementVersionEntry> Entries);

public record LearningAgreementVersionEntry(
    int HomeSlotId,
    string HomeSlotLabel,
    int HomeSlotSemester,
    int HomeSlotEcts,
    string Mode,
    int? PartnerCourseId,
    string? PartnerCourseCode,
    string? PartnerCourseName,
    decimal? AwardedEcts,
    string? PartnerCourseNameHr = null,
    string? PartnerCourseUrl = null);
