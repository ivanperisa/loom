namespace Loom.Application.DTOs.Institution;

public record InstitutionResponse(
    int Id,
    string Name,
    string? NameHr,
    string? Country,
    string? City,
    string? ErasmusCode
);
