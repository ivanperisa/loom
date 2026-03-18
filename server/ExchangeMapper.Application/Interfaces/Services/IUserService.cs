using ErrorOr;
using ExchangeMapper.Application.DTOs.Auth;
using ExchangeMapper.Application.DTOs.User;

namespace ExchangeMapper.Application.Interfaces.Services;

public interface IUserService
{
    Task<ErrorOr<AuthMeResponse>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
    Task<ErrorOr<AuthMeResponse>> CompleteOnboardingAsync(Guid userId, CompleteOnboardingRequest request, CancellationToken ct = default);
    Task<ErrorOr<AuthMeResponse>> MakeCoordinatorAsync(Guid adminId, Guid targetUserId, CancellationToken ct = default);
}
