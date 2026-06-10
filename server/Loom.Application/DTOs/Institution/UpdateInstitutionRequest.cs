namespace Loom.Application.DTOs.Institution;

public record UpdateInstitutionRequest(
    string Name,
    string? NameHr,
    string Country,
    string? City = null,
    string? ErasmusCode = null
);
