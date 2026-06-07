using ErrorOr;
using Loom.Application.DTOs.Auth;
using Loom.Application.DTOs.User;

namespace Loom.Application.Interfaces.Services;

public interface IUserService
{
    Task<ErrorOr<AuthMeResponse>> GetCurrentUserAsync(int userId, CancellationToken ct = default);
    Task<ErrorOr<AuthMeResponse>> CompleteOnboardingAsync(int userId, CompleteOnboardingRequest request, CancellationToken ct = default);
    Task<ErrorOr<AuthMeResponse>> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task<ErrorOr<AuthMeResponse>> RequestCoordinatorRoleAsync(int userId, CancellationToken ct = default);
}
