namespace Loom.Application.DTOs.LearningAgreement;

public record LaSnapshotEntry(
    int HomeSlotId,
    string HomeSlotLabel,
    int HomeSlotSemester,
    int HomeSlotEcts,
    string Mode,
    int? PartnerCourseId,
    string? PartnerCourseCode,
    string? PartnerCourseName,
    decimal? AwardedEcts
);
