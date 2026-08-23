using ErrorOr;
using Loom.Application.DTOs.Admin;
using Loom.Application.DTOs.Auth;
using Loom.Application.DTOs.Common;
using Loom.Domain.Enums;

namespace Loom.Application.Interfaces.Services;

public interface IAdminService
{
    // Users
    Task<ErrorOr<PagedResponse<UserListResponse>>> GetAllUsersAsync(int adminId, PagedRequest paging, UserRole? role = null, int? institutionId = null, bool? registered = null, string? sortBy = null, CancellationToken ct = default);
    Task<ErrorOr<UserListResponse>> UpdateUserAsync(int adminId, int targetUserId, AdminUpdateUserRequest request, CancellationToken ct = default);

    // Coordinator & role management
    Task<ErrorOr<List<CoordinatorRequestResponse>>> GetCoordinatorRequestsAsync(int adminId, CancellationToken ct = default);
    Task<ErrorOr<AuthMeResponse>> RejectCoordinatorRequestAsync(int adminId, int targetUserId, CancellationToken ct = default);
    Task<ErrorOr<UserListResponse>> SetUserRoleAsync(int adminId, int targetUserId, UserRole role, CancellationToken ct = default);

    // Coordinator whitelist
    Task<ErrorOr<List<CoordinatorWhitelistEntryResponse>>> GetCoordinatorWhitelistAsync(int adminId, CancellationToken ct = default);
    Task<ErrorOr<CoordinatorWhitelistEntryResponse>> AddToCoordinatorWhitelistAsync(int adminId, string email, CancellationToken ct = default);
    Task<ErrorOr<Deleted>> RemoveFromCoordinatorWhitelistAsync(int adminId, string email, CancellationToken ct = default);
}
