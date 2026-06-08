namespace Loom.Application.DTOs.Institution;

public record PartnerInstitutionAdminResponse(
    int Id,
    string Name,
    string? NameHr,
    string Country,
    string? City,
    string? ErasmusCode,
    int CourseCount
);
