using ErrorOr;
using ExchangeMapper.Application.DTOs.Responses;
using ExchangeMapper.Application.Interfaces.Repositories;
using ExchangeMapper.Application.Interfaces.Services;
using ExchangeMapper.Application.Mappers;

namespace ExchangeMapper.Application.Services;

public class InstitutionService(
    IInstitutionRepository institutionRepository,
    IStudyProgramRepository studyProgramRepository,
    IStudyProfileRepository studyProfileRepository) : IInstitutionService
{
    public async Task<ErrorOr<List<InstitutionResponseDto>>> GetHomeInstitutionsAsync(CancellationToken ct = default)
    {
        var institutions = await institutionRepository.GetHomeInstitutionsAsync(ct);
        return institutions.Select(x => x.ToInstitutionDto()).ToList();
    }

    public async Task<ErrorOr<List<StudyProgramResponseDto>>> GetProgramsByInstitutionAsync(Guid institutionId, CancellationToken ct = default)
    {
        var programs = await studyProgramRepository.GetByInstitutionIdAsync(institutionId, ct);
        return programs.Select(x => x.ToStudyProgramDto()).ToList();
    }

    public async Task<ErrorOr<List<StudyProfileResponseDto>>> GetProfilesByProgramAsync(Guid institutionId, Guid programId, CancellationToken ct = default)
    {
        var programs = await studyProgramRepository.GetByInstitutionIdAsync(institutionId, ct);
        if (programs.All(x => x.Id != programId))
        {
            return Error.NotFound("PROGRAM_NOT_FOUND", "Study program was not found for the given institution.");
        }

        var profiles = await studyProfileRepository.GetByProgramIdAsync(programId, ct);
        return profiles.Select(x => x.ToStudyProfileDto()).ToList();
    }
}
