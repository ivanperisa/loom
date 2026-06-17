using Loom.Application.DTOs.Institution;
using Loom.Domain.Entities;

namespace Loom.Application.Mappers;

public static class InstitutionMapper
{
    public static InstitutionResponse ToResponse(this Institution institution) => new(
        institution.Id,
        institution.Name,
        institution.NameHr,
        institution.Country,
        institution.City,
        institution.ErasmusCode
    );

    public static PartnerInstitutionAdminResponse ToAdminResponse(this Institution institution) => new(
        institution.Id,
        institution.Name,
        institution.NameHr,
        institution.Country,
        institution.City,
        institution.ErasmusCode,
        institution.PartnerCourses.Count,
        institution.IsDeleted
    );

    public static HomeProgramResponse ToResponse(this HomeProgram program) => new(
        program.Id,
        program.Name,
        program.NameEn,
        program.Level.ToString(),
        program.DurationSemesters,
        program.Profiles.Select(p => p.ToResponse()).ToList()
    );

    public static HomeProfileResponse ToResponse(this HomeProfile profile) => new(
        profile.Id,
        profile.Name,
        profile.NameEn
    );
}
