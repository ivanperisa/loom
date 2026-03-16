using ExchangeMapper.Application.DTOs.Institution;

namespace ExchangeMapper.Application.DTOs.Auth;

public class AuthMeResponse
{
    public bool IsAuthenticated { get; set; }
    public string? Sub { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Role { get; set; }
    public bool IsOnboarded { get; set; }
    public List<UserInstitutionResponse> Institutions { get; set; } = [];
}
