namespace Loom.Application.DTOs.MappingScheme;

public record SaveMappingSchemeRequest(List<SaveMappingSchemeEntryRequest> Entries);

public record SaveMappingSchemeEntryRequest(
    int Id,
    int HomeSlotId,
    int? PartnerCourseId,
    decimal AwardedEcts,
    string? EnrollmentStatus,
    string? OriginalGrade,
    string? EctsGrade,
    string? HrGrade,
    DateOnly? ExamDate
);
