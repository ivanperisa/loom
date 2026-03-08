using ErrorOr;
using ExchangeMapper.Application.DTOs.Requests;
using ExchangeMapper.Domain.Entities;
using ExchangeMapper.Domain.Enums;

namespace ExchangeMapper.Application.Interfaces.Services;

public interface IUserService
{
    Task<ErrorOr<User>> SyncUserAsync(string externalId, string email, string name, CancellationToken ct = default);
    Task<ErrorOr<User>> GetByExternalIdAsync(string externalId, CancellationToken ct = default);
    Task<ErrorOr<User>> GetByExternalIdWithDetailsAsync(string externalId, CancellationToken ct = default);
    Task<ErrorOr<Success>> CompleteOnboardingAsync(Guid userId, OnboardingRequestDto request, CancellationToken ct = default);
    Task<ErrorOr<Success>> AddInstitutionAsync(Guid userId, InstitutionEntryDto request, UserRole role, CancellationToken ct = default);
    Task<ErrorOr<Success>> UpdateInstitutionAsync(Guid userId, Guid userInstitutionId, InstitutionEntryDto request, UserRole role, CancellationToken ct = default);
    Task<ErrorOr<Success>> RemoveInstitutionAsync(Guid userId, Guid userInstitutionId, CancellationToken ct = default);
    Task<ErrorOr<Success>> MakeCoordinatorAsync(Guid userId, CancellationToken ct = default);
}
