namespace ExchangeMapper.Application.DTOs.User;

public record CompleteOnboardingRequest(
    Guid InstitutionId,
    string? Jmbag = null
);
