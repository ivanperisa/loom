using Loom.Application.Common.Querying;
using Loom.Domain.Enums;

namespace Loom.Application.Features.Admin;

public record UserListQuery : ListQuery
{
    public UserRole? Role { get; init; }
    public int? InstitutionId { get; init; }
    /// <summary>true: real accounts only; false: placeholders only.</summary>
    public bool? Registered { get; init; }
}

public record UserListResponse(
    int Id,
    string Name,
    string Email,
    UserRole Role,
    string? InstitutionName,
    string? InstitutionCity,
    int? InstitutionId,
    CoordinatorRequestStatus? CoordinatorRequestStatus,
    bool IsOnboarded,
    string? Jmbag,
    string? Mentor,
    int? CoordinatorId,
    string? CoordinatorName);

public record AdminUpdateUserRequest(string Name, string? Jmbag, string? Mentor, int? CoordinatorId, int? InstitutionId);
public record AdminSetRoleRequest(UserRole Role);

public record CoordinatorRequestResponse(int Id, string Name, string Email, string? InstitutionName);

public record AddToWhitelistRequest(string Email);
public record CoordinatorWhitelistEntryResponse(int Id, string Email, DateTime CreatedAt);
