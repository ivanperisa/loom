namespace Loom.Application.DTOs.CourseSlot;

public record CourseSlotResponse(
    Guid Id,
    int Semester,
    int ColStart,
    int Ects,
    string CategoryCode,
    string CategoryName,
    string CategoryNameEn,
    string Color,
    string? CourseCode,
    string CourseName,
    string? CourseNameEn
);
