namespace Loom.Application.DTOs.LearningAgreement;

public record PartnerCourseResponse(
    int Id,
    string Code,
    string NameEn,
    string? NameHr,
    decimal Ects,
    int? LecturesH,
    int? AuditoryH,
    int? LabH
);
