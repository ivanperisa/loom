namespace ExchangeMapper.Application.DTOs.Institution;

public record ForeignProgramResponse(
    Guid Id,
    string Name,
    string? NameEn,
    Guid InstitutionId,
    string InstitutionName
);
