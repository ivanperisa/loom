using ErrorOr;
using Loom.Application.DTOs.Admin;
using Loom.Application.DTOs.Auth;

namespace Loom.Application.Interfaces.Services;

public interface IAdminService
{
    // Users
    Task<ErrorOr<List<UserListResponse>>> GetAllUsersAsync(int adminId, CancellationToken ct = default);

    // Coordinator role management
    Task<ErrorOr<List<CoordinatorRequestResponse>>> GetCoordinatorRequestsAsync(int adminId, CancellationToken ct = default);
    Task<ErrorOr<AuthMeResponse>> MakeCoordinatorAsync(int adminId, int targetUserId, CancellationToken ct = default);
    Task<ErrorOr<AuthMeResponse>> RejectCoordinatorRequestAsync(int adminId, int targetUserId, CancellationToken ct = default);
    Task<ErrorOr<AuthMeResponse>> RemoveCoordinatorAsync(int adminId, int targetUserId, CancellationToken ct = default);

    // Coordinator whitelist
    Task<ErrorOr<List<CoordinatorWhitelistEntryResponse>>> GetCoordinatorWhitelistAsync(int adminId, CancellationToken ct = default);
    Task<ErrorOr<CoordinatorWhitelistEntryResponse>> AddToCoordinatorWhitelistAsync(int adminId, string email, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> RemoveFromCoordinatorWhitelistAsync(int adminId, string email, CancellationToken ct = default);
}
