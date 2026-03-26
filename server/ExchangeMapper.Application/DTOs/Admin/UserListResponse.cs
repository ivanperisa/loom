namespace ExchangeMapper.Application.DTOs.Admin;

public record UserListResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    string? InstitutionName,
    string? CoordinatorRequestStatus
);
