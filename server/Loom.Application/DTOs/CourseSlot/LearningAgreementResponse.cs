namespace Loom.Application.DTOs.CourseSlot;

public record LearningAgreementResponse(
    Guid ExchangeId,
    string Status,
    List<CourseSlotResponse> Slots,
    List<SlotStateResponse> SlotStates
);
