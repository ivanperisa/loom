using Loom.Application.DTOs.Auth;
using Loom.Domain.Entities;

namespace Loom.Application.Mappers;

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
