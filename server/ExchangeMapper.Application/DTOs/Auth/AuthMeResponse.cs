namespace ExchangeMapper.Application.DTOs.Auth;

public record AuthMeResponse(
    Guid Id,
    string Email,
    string Name,
    string? Jmbag,
    string? Mentor,
    string Role,
    bool IsOnboarded,
    Guid? InstitutionId,
    string? InstitutionName,
    Guid? CoordinatorId,
    string? CoordinatorName
);
