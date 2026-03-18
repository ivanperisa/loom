namespace ExchangeMapper.Application.DTOs.CourseSlot;

public record CourseSlotResponse(
    Guid Id,
    int Semester,
    int ColStart,
    int Ects,
    string Category,
    string? CourseCode,
    string CourseName,
    string? CourseNameEn,
    string Color
);
