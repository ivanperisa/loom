namespace ExchangeMapper.Application.DTOs.Auth;

public record AuthMeResponse(
    Guid Id,
    string Email,
    string Name,
    string? Jmbag,
    string Role,
    bool IsOnboarded,
    Guid? InstitutionId,
    string? InstitutionName
);
