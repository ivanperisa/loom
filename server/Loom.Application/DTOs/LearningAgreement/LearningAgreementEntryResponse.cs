namespace Loom.Application.DTOs.LearningAgreement;

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
    bool IsDeleted,
    int? AmendmentNumber
);
