namespace Loom.Application.DTOs.User;

public record CompleteOnboardingRequest(
    int InstitutionId,
    string? Jmbag = null,
    bool RequestCoordinatorRole = false
);
