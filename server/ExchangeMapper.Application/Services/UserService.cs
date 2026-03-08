using ErrorOr;
using ExchangeMapper.Application.DTOs.Requests;
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
    IInstitutionRepository institutionRepository,
    IStudyProgramRepository studyProgramRepository,
    IStudyProfileRepository studyProfileRepository) : IUserService
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

        await userRepository.AddAsync(user);
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

    public async Task<ErrorOr<Success>> CompleteOnboardingAsync(Guid userId, OnboardingRequestDto request, CancellationToken ct = default)
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
            var assignment = await ResolveInstitutionAssignmentAsync(entry, request.Role, ct);
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
            await userInstitutionRepository.AddAsync(ui);
        }

        user.Role = request.Role;
        user.IsOnboarded = true;
        await userRepository.UpdateAsync(user);

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success;
    }

    public async Task<ErrorOr<Success>> AddInstitutionAsync(Guid userId, InstitutionEntryDto request, UserRole role, CancellationToken ct = default)
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

        var assignment = await ResolveInstitutionAssignmentAsync(request, role, ct);
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

        await userInstitutionRepository.AddAsync(userInstitution);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success;
    }

    public async Task<ErrorOr<Success>> UpdateInstitutionAsync(
        Guid userId,
        Guid userInstitutionId,
        InstitutionEntryDto request,
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

        var assignment = await ResolveInstitutionAssignmentAsync(request, role, ct);
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

        await userInstitutionRepository.UpdateAsync(existing);
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

        await userInstitutionRepository.DeleteAsync(userInstitution);
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
        await userRepository.UpdateAsync(user);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success;
    }

    private async Task<ErrorOr<(Guid InstitutionId, Guid? StudyProfileId)>> ResolveInstitutionAssignmentAsync(
        InstitutionEntryDto entry,
        UserRole role,
        CancellationToken ct)
    {
        if (entry.NewInstitution is not null)
        {
            if (string.IsNullOrWhiteSpace(entry.NewInstitution.Name)
                || string.IsNullOrWhiteSpace(entry.NewInstitution.Country))
            {
                return Error.Validation(
                    "INVALID_NEW_INSTITUTION",
                    "New institution requires at least name and country.");
            }

            var institution = new Institution
            {
                Name = entry.NewInstitution.Name,
                NameEn = entry.NewInstitution.NameEn?.Trim() ?? entry.NewInstitution.Name,
                Country = entry.NewInstitution.Country,
                City = entry.NewInstitution.City,
                ErasmusCode = entry.NewInstitution.ErasmusCode,
                IsHome = true
            };
            await institutionRepository.AddAsync(institution);

            if (role == UserRole.Coordinator)
            {
                return (institution.Id, null);
            }

            if (string.IsNullOrWhiteSpace(entry.NewInstitution.IscedCode)
                || string.IsNullOrWhiteSpace(entry.NewInstitution.ProgramName)
                || string.IsNullOrWhiteSpace(entry.NewInstitution.ProfileName))
            {
                return Error.Validation(
                    "INVALID_NEW_INSTITUTION",
                    "Student onboarding requires ISCED code, program name, and profile name.");
            }

            var studyProgram = new StudyProgram
            {
                InstitutionId = institution.Id,
                Name = entry.NewInstitution.ProgramName,
                NameEn = entry.NewInstitution.ProgramNameEn?.Trim() ?? entry.NewInstitution.ProgramName,
                IscedCode = entry.NewInstitution.IscedCode
            };
            await studyProgramRepository.AddAsync(studyProgram);

            var studyProfile = new StudyProfile
            {
                StudyProgramId = studyProgram.Id,
                Name = entry.NewInstitution.ProfileName,
                NameEn = entry.NewInstitution.ProfileNameEn?.Trim() ?? entry.NewInstitution.ProfileName
            };
            await studyProfileRepository.AddAsync(studyProfile);

            return (institution.Id, studyProfile.Id);
        }

        if (entry.ExistingInstitutionId.HasValue && entry.NewStudyProfile is not null && role == UserRole.Student)
        {
            if (entry.NewStudyProfile.StudyProgramId == Guid.Empty || string.IsNullOrWhiteSpace(entry.NewStudyProfile.ProfileName))
            {
                return Error.Validation("INVALID_NEW_STUDY_PROFILE", "Study program id and profile name are required.");
            }

            var institution = await institutionRepository.GetByIdAsync(entry.ExistingInstitutionId.Value, ct);
            if (institution is null)
            {
                return Error.NotFound("INSTITUTION_NOT_FOUND", "Institution not found.");
            }

            var studyProgram = await studyProgramRepository.GetByIdAsync(entry.NewStudyProfile.StudyProgramId, ct);
            if (studyProgram is null || studyProgram.InstitutionId != institution.Id)
            {
                return Error.Validation("INVALID_STUDY_PROGRAM", "Study program does not belong to the selected institution.");
            }

            var studyProfile = new StudyProfile
            {
                StudyProgramId = entry.NewStudyProfile.StudyProgramId,
                Name = entry.NewStudyProfile.ProfileName,
                NameEn = entry.NewStudyProfile.ProfileNameEn?.Trim() ?? entry.NewStudyProfile.ProfileName
            };
            await studyProfileRepository.AddAsync(studyProfile);

            return (institution.Id, studyProfile.Id);
        }

        if (entry.ExistingStudyProfileId.HasValue)
        {
            var profile = await studyProfileRepository.GetByIdAsync(entry.ExistingStudyProfileId.Value, ct);
            if (profile is null)
            {
                return Error.NotFound("STUDY_PROFILE_NOT_FOUND", "Study profile not found.");
            }

            var program = await studyProgramRepository.GetByIdAsync(profile.StudyProgramId, ct);
            if (program is null)
            {
                return Error.NotFound("STUDY_PROGRAM_NOT_FOUND", "Study program not found.");
            }

            return (program.InstitutionId, profile.Id);
        }

        if (role == UserRole.Coordinator && entry.ExistingInstitutionId.HasValue)
        {
            var institution = await institutionRepository.GetByIdAsync(entry.ExistingInstitutionId.Value, ct);
            if (institution is null)
            {
                return Error.NotFound("INSTITUTION_NOT_FOUND", "Institution not found.");
            }

            return (institution.Id, null);
        }

        return Error.Validation(
            "INVALID_INSTITUTION_ENTRY",
            "Institution entry is not valid.");
    }
}
