namespace Loom.Application.DTOs.CourseSlot;

public record CourseSlotResponse(
    Guid Id,
    int Semester,
    int SlotPosition,
    int Ects,
    string CategoryCode,
    string CategoryName,
    string CategoryNameEn,
    string Color,
    string? CourseCode,
    string CourseName,
    string? CourseNameEn
);
