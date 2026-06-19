namespace Loom.Application.DTOs.Admin;

public record UserListResponse(
    int Id,
    string Name,
    string Email,
    string Role,
    string? InstitutionName,
    int? InstitutionId,
    string? CoordinatorRequestStatus,
    bool IsOnboarded,
    string? Jmbag,
    string? Mentor,
    int? CoordinatorId,
    string? CoordinatorName
);
