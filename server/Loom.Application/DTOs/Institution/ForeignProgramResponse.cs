namespace Loom.Application.DTOs.Institution;

public record ForeignProgramResponse(
    Guid Id,
    string Name,
    string? NameEn,
    Guid InstitutionId,
    string InstitutionName
);
