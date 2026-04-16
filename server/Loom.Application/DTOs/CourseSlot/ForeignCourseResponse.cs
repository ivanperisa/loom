namespace Loom.Application.DTOs.CourseSlot;

public record ForeignCourseResponse(
    Guid Id,
    string Code,
    string NameEn,
    string? NameHr,
    decimal Ects,
    int? LecturesH,
    int? AuditoryH,
    int? LabH
);
