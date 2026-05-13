using ErrorOr;
using Loom.Application.DTOs.Admin;
using Loom.Application.DTOs.Auth;
using Loom.Application.DTOs.User;

namespace Loom.Application.Interfaces.Services;

public interface IUserService
{
    Task<ErrorOr<AuthMeResponse>> GetCurrentUserAsync(int userId, CancellationToken ct = default);
    Task<ErrorOr<AuthMeResponse>> CompleteOnboardingAsync(int userId, CompleteOnboardingRequest request, CancellationToken ct = default);
    Task<ErrorOr<AuthMeResponse>> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task<ErrorOr<AuthMeResponse>> RequestCoordinatorRoleAsync(int userId, CancellationToken ct = default);

    // Admin — user management
    Task<ErrorOr<List<UserListResponse>>> GetAllUsersAsync(int adminId, CancellationToken ct = default);

    // Admin — coordinator role management
    Task<ErrorOr<List<CoordinatorRequestResponse>>> GetCoordinatorRequestsAsync(int adminId, CancellationToken ct = default);
    Task<ErrorOr<AuthMeResponse>> MakeCoordinatorAsync(int adminId, int targetUserId, CancellationToken ct = default);
    Task<ErrorOr<AuthMeResponse>> RejectCoordinatorRequestAsync(int adminId, int targetUserId, CancellationToken ct = default);
    Task<ErrorOr<AuthMeResponse>> RemoveCoordinatorAsync(int adminId, int targetUserId, CancellationToken ct = default);

    // Admin — coordinator whitelist
    Task<ErrorOr<List<CoordinatorWhitelistEntryResponse>>> GetCoordinatorWhitelistAsync(int adminId, CancellationToken ct = default);
    Task<ErrorOr<CoordinatorWhitelistEntryResponse>> AddToCoordinatorWhitelistAsync(int adminId, string email, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> RemoveFromCoordinatorWhitelistAsync(int adminId, string email, CancellationToken ct = default);
}
