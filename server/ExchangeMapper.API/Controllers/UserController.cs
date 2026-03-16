using ExchangeMapper.Application.DTOs.Auth;
using ExchangeMapper.Application.DTOs.Institution;
using ExchangeMapper.Application.Interfaces.Services;
using ExchangeMapper.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeMapper.API.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class UserController(IUserService userService) : ApiController
{
    [HttpPost("onboarding")]
    public async Task<IActionResult> Onboarding([FromBody] OnboardingRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await userService.CompleteOnboardingAsync(userId.Value, request, ct);
        return Match(result, _ => Ok());
    }

    [HttpPost("institution")]
    public async Task<IActionResult> AddInstitution([FromBody] InstitutionEntryRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var roleClaim = GetCurrentRole();
        if (roleClaim is null || !Enum.TryParse<UserRole>(roleClaim, out var role))
        {
            return Unauthorized();
        }

        var result = await userService.AddInstitutionAsync(userId.Value, request, role, ct);
        return Match(result, _ => Ok());
    }

    [HttpPut("institution/{userInstitutionId:guid}")]
    public async Task<IActionResult> UpdateInstitution(Guid userInstitutionId, [FromBody] InstitutionEntryRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var roleClaim = GetCurrentRole();
        if (roleClaim is null || !Enum.TryParse<UserRole>(roleClaim, out var role))
        {
            return Unauthorized();
        }

        var result = await userService.UpdateInstitutionAsync(userId.Value, userInstitutionId, request, role, ct);
        return Match(result, _ => Ok());
    }

    [HttpDelete("institution/{userInstitutionId:guid}")]
    public async Task<IActionResult> RemoveInstitution(Guid userInstitutionId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await userService.RemoveInstitutionAsync(userId.Value, userInstitutionId, ct);
        return Match(result, _ => NoContent());
    }
}
