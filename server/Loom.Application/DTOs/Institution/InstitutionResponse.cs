namespace Loom.Application.DTOs.Institution;

public record InstitutionResponse(
    Guid Id,
    string Name,
    string? NameEn,
    string? Country,
    string? City,
    string? ErasmusCode,
    bool IsHome
);
