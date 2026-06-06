namespace Loom.Application.DTOs.Institution;

public record CreatePartnerCourseRequest(
    string Code,
    string NameEn,
    string? NameHr,
    decimal Ects,
    int? LecturesH = null,
    int? AuditoryH = null,
    int? LabH = null
);
