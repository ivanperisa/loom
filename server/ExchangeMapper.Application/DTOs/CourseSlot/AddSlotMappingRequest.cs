namespace ExchangeMapper.Application.DTOs.CourseSlot;

public record AddSlotMappingRequest(Guid CourseSlotId, Guid ForeignCourseId, decimal AwardedEcts);
