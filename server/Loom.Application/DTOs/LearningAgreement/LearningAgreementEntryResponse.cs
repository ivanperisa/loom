namespace Loom.Application.DTOs.LearningAgreement;

public record LearningAgreementEntryResponse(
    int Id,
    int HomeSlotId,
    string Mode,
    int? PartnerCourseId,
    string? PartnerCourseCode,
    string? PartnerCourseNameEn,
    string? PartnerCourseNameHr,
    decimal? AwardedEcts,
    bool IsDeleted
);
