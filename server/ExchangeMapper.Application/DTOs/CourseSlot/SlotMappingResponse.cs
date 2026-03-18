namespace ExchangeMapper.Application.DTOs.CourseSlot;

public record SlotMappingResponse(
    Guid Id,
    Guid ForeignCourseId,
    string ForeignCourseCode,
    string ForeignCourseNameEn,
    string? ForeignCourseNameHr,
    decimal AwardedEcts
);
