using ErrorOr;
using ExchangeMapper.Application.DTOs.Institution;
using ExchangeMapper.Domain.Enums;

namespace ExchangeMapper.Application.Interfaces.Services;

public interface IInstitutionResolverService
{
    Task<ErrorOr<(Guid InstitutionId, Guid? StudyProfileId)>> ResolveAssignmentAsync(
        InstitutionEntryRequest entry,
        UserRole role,
        CancellationToken ct = default);
}
