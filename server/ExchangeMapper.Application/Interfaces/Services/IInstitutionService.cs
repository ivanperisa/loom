using ErrorOr;
using ExchangeMapper.Application.DTOs.Responses;

namespace ExchangeMapper.Application.Interfaces.Services;

public interface IInstitutionService
{
    Task<ErrorOr<List<InstitutionResponseDto>>> GetHomeInstitutionsAsync(CancellationToken ct = default);
    Task<ErrorOr<List<StudyProgramResponseDto>>> GetProgramsByInstitutionAsync(Guid institutionId, CancellationToken ct = default);
    Task<ErrorOr<List<StudyProfileResponseDto>>> GetProfilesByProgramAsync(Guid institutionId, Guid programId, CancellationToken ct = default);
}
