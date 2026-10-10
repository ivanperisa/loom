using Loom.Domain.Enums;

namespace Loom.Application.Features.Users;

public record AuthMeResponse(
    int Id,
    string Email,
    string Name,
    string? Jmbag,
    string? Mentor,
    UserRole Role,
    bool IsOnboarded,
    int? InstitutionId,
    string? InstitutionName,
    int? CoordinatorId,
    string? CoordinatorName,
    CoordinatorRequestStatus? CoordinatorRequestStatus);

/// <summary>Who is signed in. <see cref="User"/> is null when nobody is.</summary>
public record SessionResponse(bool IsAuthenticated, AuthMeResponse? User);

public record CompleteOnboardingRequest(int InstitutionId, string? Jmbag = null, bool RequestCoordinatorRole = false);

public record UpdateProfileRequest(string Name, string? Jmbag, int InstitutionId, string? Mentor, int? CoordinatorId);

/// <summary>What the API needs about the signed-in user on every request.</summary>
public record SyncedUser(int Id, UserRole Role);
