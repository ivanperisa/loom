namespace Loom.Application.DTOs.Exchange;

public record SaveLearningAgreementRequest(List<SlotStateUpsertDto> SlotStates);

public record SlotStateUpsertDto(Guid CourseSlotId, string Mode, List<SlotMappingUpsertDto> Mappings);

public record SlotMappingUpsertDto(Guid ForeignCourseId, decimal AwardedEcts);
