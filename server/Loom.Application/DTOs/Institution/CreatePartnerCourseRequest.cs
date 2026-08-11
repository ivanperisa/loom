namespace Loom.Application.DTOs.Institution;

public record CreatePartnerCourseRequest(
    string Code,
    string Name,
    string? NameHr,
    decimal Ects,
    string Semester,
    string Level,
    int? LecturesH = null,
    int? AuditoryH = null,
    int? LabH = null,
    string? Url = null
);
