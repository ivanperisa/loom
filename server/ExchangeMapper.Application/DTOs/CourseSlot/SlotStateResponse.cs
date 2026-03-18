namespace ExchangeMapper.Application.DTOs.CourseSlot;

public record SlotStateResponse(
    Guid Id,
    Guid CourseSlotId,
    string Mode,
    List<SlotMappingResponse> Mappings
);
