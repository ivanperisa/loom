namespace Loom.Application.Features.Planning;

public record SaveLearningAgreementRequest(List<LearningAgreementEntryUpsertDto> Entries);

public record LearningAgreementEntryUpsertDto(
    int HomeSlotId,
    string Mode,
    int? PartnerCourseId,
    decimal? AwardedEcts
);
