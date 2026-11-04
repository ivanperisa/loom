namespace Loom.Application.Features.Planning;

public record HomeSlotResponse(
    int Id,
    int Semester,
    int SlotPosition,
    int Ects,
    int CourseTypeId,
    string CourseTypeName,
    string? CourseTypeNameEn,
    string Color,

    int? CourseIsvuCode,
    string? CourseName,
    string? CourseNameEn,

    int? CourseGroupIsvuCode,
    string? CourseGroupName,
    string? CourseGroupNameEn
);
