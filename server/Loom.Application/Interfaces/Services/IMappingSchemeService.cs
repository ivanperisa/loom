using ErrorOr;
using Loom.Application.DTOs.MappingScheme;

namespace Loom.Application.Interfaces.Services;

public interface IMappingSchemeService
{
    Task<ErrorOr<MappingSchemeResponse>> GetMappingSchemeAsync(Guid exchangeGuid, int requesterId, CancellationToken ct = default);
    Task<ErrorOr<MappingSchemeResponse>> SaveMappingSchemeAsync(Guid exchangeGuid, int requesterId, SaveMappingSchemeRequest request, CancellationToken ct = default);
    Task<bool> EnsureMappingSchemeInitializedAsync(int exchangeId, CancellationToken ct = default);
}
