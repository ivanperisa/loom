using ExchangeMapper.Application.DTOs.Institution;
using ExchangeMapper.Domain.Enums;

namespace ExchangeMapper.Application.DTOs.Auth;

public class OnboardingRequest
{
    public UserRole Role { get; set; }
    public List<InstitutionEntryRequest> Institutions { get; set; } = [];
}
