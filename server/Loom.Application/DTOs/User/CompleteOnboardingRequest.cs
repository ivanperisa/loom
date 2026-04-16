namespace Loom.Application.DTOs.User;

public record CompleteOnboardingRequest(
    Guid InstitutionId,
    string? Jmbag = null,
    bool RequestCoordinatorRole = false
);
