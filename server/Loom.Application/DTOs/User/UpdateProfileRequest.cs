namespace Loom.Application.DTOs.User;

public record UpdateProfileRequest(
    string Name,
    string? Jmbag,
    int InstitutionId,
    string? Mentor,
    int? CoordinatorId
);
