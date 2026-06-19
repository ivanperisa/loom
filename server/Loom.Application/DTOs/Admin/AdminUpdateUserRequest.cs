namespace Loom.Application.DTOs.Admin;

public record AdminUpdateUserRequest(
    string Name,
    string? Jmbag,
    string? Mentor,
    int? CoordinatorId,
    int? InstitutionId
);
