namespace Loom.Application.DTOs.Institution;

public record CreatePartnerInstitutionRequest(
    string Name,
    string? NameHr,
    string Country,
    string? City = null,
    string? ErasmusCode = null
);
