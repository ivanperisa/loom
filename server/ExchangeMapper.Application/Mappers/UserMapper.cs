using ExchangeMapper.Application.DTOs.Auth;
using ExchangeMapper.Domain.Entities;

namespace ExchangeMapper.Application.Mappers;

public static class UserMapper
{
    public static AuthMeResponse ToAuthMeResponse(this User user) => new(
        user.Id,
        user.Email,
        user.Name,
        user.Jmbag,
        user.Mentor,
        user.Role.ToString(),
        user.IsOnboarded,
        user.InstitutionId,
        user.Institution?.Name,
        user.CoordinatorId,
        user.Coordinator?.Name,
        user.CoordinatorRequestStatus
    );
}
