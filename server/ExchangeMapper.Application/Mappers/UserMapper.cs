using ExchangeMapper.Application.DTOs.Auth;
using ExchangeMapper.Domain.Entities;

namespace ExchangeMapper.Application.Mappers;

public static class UserMapper
{
    public static AuthMeResponse ToAuthMeResponse(this User user) => new()
    {
        IsAuthenticated = true,
        Sub = user.ExternalId,
        Email = user.Email,
        Name = user.Name,
        Role = user.Role.ToString(),
        IsOnboarded = user.IsOnboarded,
        Institutions = user.UserInstitutions
            .OrderBy(ui => ui.CreatedAt)
            .Select(ui => ui.ToResponse())
            .ToList()
    };

    public static AuthMeResponse ToUnauthenticatedResponse() => new()
    {
        IsAuthenticated = false
    };
}
