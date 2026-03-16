using ErrorOr;
using ExchangeMapper.Application.DTOs.Auth;
using ExchangeMapper.Application.DTOs.Institution;
using ExchangeMapper.Domain.Enums;

namespace ExchangeMapper.Application.Interfaces.Services;

public interface IUserService
{
    Task<ErrorOr<Success>> CompleteOnboardingAsync(Guid userId, OnboardingRequest request, CancellationToken ct = default);
    Task<ErrorOr<Success>> AddInstitutionAsync(Guid userId, InstitutionEntryRequest request, UserRole role, CancellationToken ct = default);
    Task<ErrorOr<Success>> UpdateInstitutionAsync(Guid userId, Guid userInstitutionId, InstitutionEntryRequest request, UserRole role, CancellationToken ct = default);
    Task<ErrorOr<Success>> RemoveInstitutionAsync(Guid userId, Guid userInstitutionId, CancellationToken ct = default);
    Task<ErrorOr<Success>> MakeCoordinatorAsync(Guid userId, CancellationToken ct = default);
}
