namespace Loom.Application.DTOs.Admin;

public record CoordinatorRequestResponse(
    int Id,
    string Name,
    string Email,
    string? InstitutionName
);
