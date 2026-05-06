namespace Loom.Application.DTOs.Exchange;

public record SaveLearningAgreementRequest(List<LearningAgreementEntryUpsertDto> Entries);

public record LearningAgreementEntryUpsertDto(
    Guid CourseSlotId,
    string Mode,
    Guid? ForeignCourseId,
    decimal? AwardedEcts
);
