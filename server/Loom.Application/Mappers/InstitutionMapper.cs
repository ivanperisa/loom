using Loom.Application.DTOs.Institution;
using Loom.Domain.Entities;

namespace Loom.Application.Mappers;

public static class InstitutionMapper
{
    public static InstitutionResponse ToResponse(this Institution institution) => new(
        institution.Id,
        institution.Name,
        institution.NameEn,
        institution.Country,
        institution.City,
        institution.ErasmusCode
    );

    public static PartnerProgramResponse ToResponse(this PartnerProgram program) => new(
        program.Id,
        program.Name,
        program.NameEn,
        program.Level.ToString(),
        program.Institution.Name,
        program.Institution.Country,
        program.Institution.City
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
