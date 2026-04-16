namespace Loom.Application.DTOs.Admin;

public record CoordinatorRequestResponse(
    Guid Id,
    string Name,
    string Email,
    string? InstitutionName
);
