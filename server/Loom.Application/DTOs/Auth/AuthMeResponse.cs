namespace Loom.Application.DTOs.Auth;

public record AuthMeResponse(
    int Id,
    string Email,
    string Name,
    string? Jmbag,
    string? Mentor,
    string Role,
    bool IsOnboarded,
    int? InstitutionId,
    string? InstitutionName,
    int? CoordinatorId,
    string? CoordinatorName,
    string? CoordinatorRequestStatus
);
