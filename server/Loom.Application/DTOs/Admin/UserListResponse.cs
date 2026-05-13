namespace Loom.Application.DTOs.Admin;

public record UserListResponse(
    int Id,
    string Name,
    string Email,
    string Role,
    string? InstitutionName,
    string? CoordinatorRequestStatus
);
