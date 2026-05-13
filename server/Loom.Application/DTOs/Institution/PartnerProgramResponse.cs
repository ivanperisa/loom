namespace Loom.Application.DTOs.Institution;

public record PartnerProgramResponse(
    int Id,
    string Name,
    string? NameEn,
    string Level,
    string InstitutionName
);
