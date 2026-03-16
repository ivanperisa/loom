using ErrorOr;
using ExchangeMapper.Application.DTOs.Auth;
using ExchangeMapper.Application.DTOs.Institution;
using ExchangeMapper.Application.Interfaces;
using ExchangeMapper.Application.Interfaces.Repositories;
using ExchangeMapper.Application.Interfaces.Services;
using ExchangeMapper.Domain.Entities;
using ExchangeMapper.Domain.Enums;

namespace ExchangeMapper.Application.Services;

public class UserService(
    IUnitOfWork unitOfWork,
    IUserRepository userRepository,
    IUserInstitutionRepository userInstitutionRepository,
    IExchangeRepository exchangeRepository,
    IInstitutionResolverService institutionResolver) : IUserService, IUserSyncService
{
    public async Task<ErrorOr<User>> SyncUserAsync(string externalId, string email, string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return Error.Validation("INVALID_EXTERNAL_ID", "External id is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return Error.Validation("INVALID_EMAIL", "Email is required.");
        }

        var existingUser = await userRepository.GetByExternalIdAsync(externalId, ct);
        if (existingUser is not null)
        {
            return existingUser;
        }

        var user = new User
        {
            ExternalId = externalId,
            Email = email,
            Name = name,
            Role = UserRole.Student,
            IsOnboarded = false
        };

        await userRepository.AddAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return user;
    }

    public async Task<ErrorOr<User>> GetByExternalIdAsync(string externalId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return Error.Validation("INVALID_EXTERNAL_ID", "External id is required.");
        }

        var user = await userRepository.GetByExternalIdAsync(externalId, ct);
        return user is null
            ? Error.NotFound("USER_NOT_FOUND", "User not found.")
            : user;
    }

    public async Task<ErrorOr<User>> GetByExternalIdWithDetailsAsync(string externalId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return Error.Validation("INVALID_EXTERNAL_ID", "External id is required.");
        }

        var user = await userRepository.GetByExternalIdWithDetailsAsync(externalId, ct);
        return user is null
            ? Error.NotFound("USER_NOT_FOUND", "User not found.")
            : user;
    }

    public async Task<ErrorOr<Success>> CompleteOnboardingAsync(Guid userId, OnboardingRequest request, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(typeof(UserRole), request.Role))
        {
            return Error.Validation("INVALID_ROLE", "Provided role is not valid.");
        }

        if (request.Institutions is null || request.Institutions.Count == 0)
        {
            return Error.Validation("MISSING_INSTITUTIONS", "At least one institution is required.");
        }

        if (request.Role == UserRole.Student)
        {
            var missingProfile = request.Institutions.Any(i =>
                i.ExistingStudyProfileId is null &&
                i.NewStudyProfile is null &&
                i.NewInstitution is null);

            if (missingProfile)
            {
                return Error.Validation("MISSING_STUDY_PROFILE", "Each student institution entry must include a study profile.");
            }
        }

        if (request.Role == UserRole.Coordinator)
        {
            var missingInstitution = request.Institutions.Any(i =>
                i.ExistingInstitutionId is null &&
                i.NewInstitution is null);

            if (missingInstitution)
            {
                return Error.Validation("MISSING_INSTITUTION", "Each coordinator entry must include an institution.");
            }
        }

        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
        {
            return Error.NotFound("USER_NOT_FOUND", "User not found.");
        }

        if (user.IsOnboarded)
        {
            return Error.Conflict("ALREADY_ONBOARDED", "User has already completed onboarding.");
        }

        var userInstitutions = new List<UserInstitution>();
        foreach (var entry in request.Institutions)
        {
            var assignment = await institutionResolver.ResolveAssignmentAsync(entry, request.Role, ct);
            if (assignment.IsError)
            {
                return assignment.Errors;
            }

            var duplicate = userInstitutions.Any(ui =>
                ui.InstitutionId == assignment.Value.InstitutionId && ui.StudyProfileId == assignment.Value.StudyProfileId);
            if (duplicate)
            {
                return Error.Conflict("DUPLICATE_INSTITUTION", "Duplicate institution entry in request.");
            }

            userInstitutions.Add(new UserInstitution
            {
                UserId = userId,
                InstitutionId = assignment.Value.InstitutionId,
                StudyProfileId = assignment.Value.StudyProfileId
            });
        }

        foreach (var ui in userInstitutions)
        {
            await userInstitutionRepository.AddAsync(ui, ct);
        }

        user.Role = request.Role;
        user.IsOnboarded = true;
        await userRepository.UpdateAsync(user, ct);

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success;
    }

    public async Task<ErrorOr<Success>> AddInstitutionAsync(Guid userId, InstitutionEntryRequest request, UserRole role, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(typeof(UserRole), role))
        {
            return Error.Validation("INVALID_ROLE", "Provided role is not valid.");
        }

        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
        {
            return Error.NotFound("USER_NOT_FOUND", "User not found.");
        }

        if (user.Role != role)
        {
            return Error.Validation("INVALID_ROLE", "Request role does not match current user role.");
        }

        var assignment = await institutionResolver.ResolveAssignmentAsync(request, role, ct);
        if (assignment.IsError)
        {
            return assignment.Errors;
        }

        var existingUserInstitutions = await userInstitutionRepository.GetByUserIdAsync(userId, ct);
        var duplicateExists = existingUserInstitutions.Any(ui =>
            ui.InstitutionId == assignment.Value.InstitutionId && ui.StudyProfileId == assignment.Value.StudyProfileId);
        if (duplicateExists)
        {
            return Error.Conflict("USER_INSTITUTION_EXISTS", "User institution pair already exists.");
        }

        var userInstitution = new UserInstitution
        {
            UserId = userId,
            InstitutionId = assignment.Value.InstitutionId,
            StudyProfileId = assignment.Value.StudyProfileId
        };

        await userInstitutionRepository.AddAsync(userInstitution, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success;
    }

    public async Task<ErrorOr<Success>> UpdateInstitutionAsync(
        Guid userId,
        Guid userInstitutionId,
        InstitutionEntryRequest request,
        UserRole role,
        CancellationToken ct = default)
    {
        if (userInstitutionId == Guid.Empty)
        {
            return Error.Validation("INVALID_USER_INSTITUTION_ID", "User institution id is required.");
        }

        if (!Enum.IsDefined(typeof(UserRole), role))
        {
            return Error.Validation("INVALID_ROLE", "Provided role is not valid.");
        }

        var existing = await userInstitutionRepository.GetByIdAsync(userInstitutionId, ct);
        if (existing is null)
        {
            return Error.NotFound("USER_INSTITUTION_NOT_FOUND", "Institution association not found.");
        }

        if (existing.UserId != userId)
        {
            return Error.Forbidden("FORBIDDEN", "You do not have permission to edit this institution.");
        }

        var hasActiveExchanges = await exchangeRepository.ExistsForUserInstitutionAsync(userInstitutionId, ct);
        if (hasActiveExchanges)
        {
            return Error.Conflict("HAS_ACTIVE_EXCHANGES", "Cannot edit institution with active exchanges.");
        }

        var assignment = await institutionResolver.ResolveAssignmentAsync(request, role, ct);
        if (assignment.IsError)
        {
            return assignment.Errors;
        }

        var existingUserInstitutions = await userInstitutionRepository.GetByUserIdAsync(userId, ct);
        var duplicateExists = existingUserInstitutions.Any(ui =>
            ui.Id != userInstitutionId
            && ui.InstitutionId == assignment.Value.InstitutionId
            && ui.StudyProfileId == assignment.Value.StudyProfileId);
        if (duplicateExists)
        {
            return Error.Conflict("USER_INSTITUTION_EXISTS", "User institution pair already exists.");
        }

        existing.InstitutionId = assignment.Value.InstitutionId;
        existing.StudyProfileId = assignment.Value.StudyProfileId;

        await userInstitutionRepository.UpdateAsync(existing, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success;
    }

    public async Task<ErrorOr<Success>> RemoveInstitutionAsync(Guid userId, Guid userInstitutionId, CancellationToken ct = default)
    {
        if (userInstitutionId == Guid.Empty)
        {
            return Error.Validation("INVALID_USER_INSTITUTION_ID", "User institution id is required.");
        }

        var userInstitution = await userInstitutionRepository.GetByIdAsync(userInstitutionId, ct);
        if (userInstitution is null)
        {
            return Error.NotFound("USER_INSTITUTION_NOT_FOUND", "Institution association not found.");
        }

        if (userInstitution.UserId != userId)
        {
            return Error.Forbidden("FORBIDDEN", "You do not have permission to remove this institution.");
        }

        var hasActiveExchanges = await exchangeRepository.ExistsForUserInstitutionAsync(userInstitutionId, ct);
        if (hasActiveExchanges)
        {
            return Error.Conflict("HAS_ACTIVE_EXCHANGES", "Cannot remove institution with active exchanges.");
        }

        await userInstitutionRepository.DeleteAsync(userInstitution, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success;
    }

    public async Task<ErrorOr<Success>> MakeCoordinatorAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
        {
            return Error.NotFound("USER_NOT_FOUND", "User not found.");
        }

        user.Role = UserRole.Coordinator;
        user.IsOnboarded = true;
        await userRepository.UpdateAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success;
    }
}
