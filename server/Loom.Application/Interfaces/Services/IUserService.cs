using ErrorOr;
using Loom.Application.DTOs.Admin;
using Loom.Application.DTOs.Auth;
using Loom.Application.DTOs.User;

namespace Loom.Application.Interfaces.Services;

public interface IUserService
{
    Task<ErrorOr<AuthMeResponse>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
    Task<ErrorOr<AuthMeResponse>> CompleteOnboardingAsync(Guid userId, CompleteOnboardingRequest request, CancellationToken ct = default);
    Task<ErrorOr<AuthMeResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task<ErrorOr<AuthMeResponse>> RequestCoordinatorRoleAsync(Guid userId, CancellationToken ct = default);

    // Admin — user management
    Task<ErrorOr<List<UserListResponse>>> GetAllUsersAsync(Guid adminId, CancellationToken ct = default);

    // Admin — coordinator role management
    Task<ErrorOr<List<CoordinatorRequestResponse>>> GetCoordinatorRequestsAsync(Guid adminId, CancellationToken ct = default);
    Task<ErrorOr<AuthMeResponse>> MakeCoordinatorAsync(Guid adminId, Guid targetUserId, CancellationToken ct = default);
    Task<ErrorOr<AuthMeResponse>> RejectCoordinatorRequestAsync(Guid adminId, Guid targetUserId, CancellationToken ct = default);
    Task<ErrorOr<AuthMeResponse>> RemoveCoordinatorAsync(Guid adminId, Guid targetUserId, CancellationToken ct = default);

    // Admin — coordinator whitelist
    Task<ErrorOr<List<CoordinatorWhitelistEntryResponse>>> GetCoordinatorWhitelistAsync(Guid adminId, CancellationToken ct = default);
    Task<ErrorOr<CoordinatorWhitelistEntryResponse>> AddToCoordinatorWhitelistAsync(Guid adminId, string email, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> RemoveFromCoordinatorWhitelistAsync(Guid adminId, string email, CancellationToken ct = default);
}
