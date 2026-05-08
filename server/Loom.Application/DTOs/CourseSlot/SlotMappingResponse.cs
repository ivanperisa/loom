namespace Loom.Application.DTOs.CourseSlot;

public record LearningAgreementEntryResponse(
    Guid Id,
    Guid CourseSlotId,
    string Mode,
    Guid? ForeignCourseId,
    string? ForeignCourseCode,
    string? ForeignCourseNameEn,
    string? ForeignCourseNameHr,
    decimal? AwardedEcts,
    bool IsDeleted = false
);
