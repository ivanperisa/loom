using ExchangeMapper.Application.DTOs.Institution;
using ExchangeMapper.Domain.Entities;

namespace ExchangeMapper.Application.Mappers;

public static class InstitutionMapper
{
    public static InstitutionResponse ToResponse(this Institution institution) => new(
        institution.Id,
        institution.Name,
        institution.NameEn,
        institution.Country,
        institution.City,
        institution.ErasmusCode,
        institution.IsHome
    );

    public static ForeignProgramResponse ToResponse(this ForeignProgram program) => new(
        program.Id,
        program.Name,
        program.NameEn,
        program.InstitutionId,
        program.Institution.Name
    );

    public static StudyProgramResponse ToResponse(this StudyProgram program) => new(
        program.Id,
        program.Name,
        program.NameEn,
        program.Level.ToString(),
        program.DurationSemesters,
        program.StudyProfiles.Select(p => p.ToResponse()).ToList()
    );

    public static StudyProfileResponse ToResponse(this StudyProfile profile) => new(
        profile.Id,
        profile.Name,
        profile.NameEn
    );
}
