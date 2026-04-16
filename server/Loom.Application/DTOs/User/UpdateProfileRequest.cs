namespace Loom.Application.DTOs.User;

public record UpdateProfileRequest(
    string Name,
    string? Jmbag,
    Guid InstitutionId,
    string? Mentor,
    Guid? CoordinatorId
);
