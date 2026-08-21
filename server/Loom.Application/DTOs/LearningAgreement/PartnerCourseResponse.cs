namespace Loom.Application.DTOs.LearningAgreement;

public record PartnerCourseResponse(
    int Id,
    string Code,
    string Name,
    string? NameHr,
    string? Url,
    decimal Ects,
    int? LecturesH,
    int? AuditoryH,
    int? LabH,
    string Semester,
    string Level,
    bool IsDeleted
);
