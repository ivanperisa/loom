using ErrorOr;
using ExchangeMapper.Application.DTOs.Course;
using ExchangeMapper.Application.DTOs.Institution;
using ExchangeMapper.Application.Interfaces.Repositories;
using ExchangeMapper.Application.Interfaces.Services;
using ExchangeMapper.Application.Mappers;
using ExchangeMapper.Domain.Entities;
using ExchangeMapper.Domain.Enums;

namespace ExchangeMapper.Application.Services;

public class InstitutionService(
    IInstitutionRepository institutionRepository,
    IStudyProgramRepository studyProgramRepository,
    IStudyProfileRepository studyProfileRepository,
    IUserInstitutionRepository userInstitutionRepository,
    ICourseRepository courseRepository) : IInstitutionService, IInstitutionResolverService
{
    public async Task<ErrorOr<List<InstitutionResponse>>> GetHomeInstitutionsAsync(CancellationToken ct = default)
    {
        var institutions = await institutionRepository.GetHomeInstitutionsAsync(ct);
        return institutions.Select(x => x.ToResponse()).ToList();
    }

    public async Task<ErrorOr<List<InstitutionResponse>>> GetForeignInstitutionsAsync(CancellationToken ct = default)
    {
        var institutions = await institutionRepository.GetForeignInstitutionsAsync(ct);
        return institutions.Select(x => x.ToResponse()).ToList();
    }

    public async Task<ErrorOr<List<StudyProgramResponse>>> GetProgramsByInstitutionAsync(Guid institutionId, CancellationToken ct = default)
    {
        var programs = await studyProgramRepository.GetByInstitutionIdAsync(institutionId, ct);
        return programs.Select(x => x.ToResponse()).ToList();
    }

    public async Task<ErrorOr<List<StudyProfileResponse>>> GetProfilesByProgramAsync(Guid institutionId, Guid programId, CancellationToken ct = default)
    {
        var programs = await studyProgramRepository.GetByInstitutionIdAsync(institutionId, ct);
        if (programs.All(x => x.Id != programId))
        {
            return Error.NotFound("PROGRAM_NOT_FOUND", "Study program was not found for the given institution.");
        }

        var profiles = await studyProfileRepository.GetByProgramIdAsync(programId, ct);
        return profiles.Select(x => x.ToResponse()).ToList();
    }

    public async Task<ErrorOr<List<CourseResponse>>> GetAvailableCoursesAsync(Guid studentId, string? query, CancellationToken ct = default)
    {
        var homeUi = await userInstitutionRepository.GetHomeByUserIdAsync(studentId, ct);
        if (homeUi is null || homeUi.StudyProfileId is null)
        {
            return Error.NotFound("STUDY_PROFILE_NOT_FOUND", "Home study profile not found for this student.");
        }

        var courses = await courseRepository.GetByStudyProfileAsync(homeUi.StudyProfileId.Value, query, ct);
        return courses.Select(c => c.ToResponse()).ToList();
    }

    public async Task<ErrorOr<(Guid InstitutionId, Guid? StudyProfileId)>> ResolveAssignmentAsync(
        InstitutionEntryRequest entry,
        UserRole role,
        CancellationToken ct = default)
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
            await institutionRepository.AddAsync(institution, ct);

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
                IscedCode = entry.NewInstitution.IscedCode,
                Level = entry.NewInstitution.Level,
                DurationSemesters = entry.NewInstitution.DurationSemesters
            };
            await studyProgramRepository.AddAsync(studyProgram, ct);

            var studyProfile = new StudyProfile
            {
                StudyProgramId = studyProgram.Id,
                Name = entry.NewInstitution.ProfileName,
                NameEn = entry.NewInstitution.ProfileNameEn?.Trim() ?? entry.NewInstitution.ProfileName
            };
            await studyProfileRepository.AddAsync(studyProfile, ct);

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
            await studyProfileRepository.AddAsync(studyProfile, ct);

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
