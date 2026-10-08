using System.Linq.Expressions;
using Loom.Domain.Entities;

namespace Loom.Application.Features.Users;

public static class UserProjections
{
    public static readonly Expression<Func<User, AuthMeResponse>> AuthMe = u => new AuthMeResponse(
        u.Id, u.Email, u.Name, u.Jmbag, u.Mentor, u.Role.ToString(), u.IsOnboarded,
        u.InstitutionId, u.Institution != null ? u.Institution.Name : null,
        u.CoordinatorId, u.Coordinator != null ? u.Coordinator.Name : null,
        u.CoordinatorRequestStatus);
}
