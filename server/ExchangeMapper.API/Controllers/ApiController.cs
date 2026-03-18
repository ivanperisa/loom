using ErrorOr;
using ExchangeMapper.API.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExchangeMapper.API.Controllers;

[ApiController]
public abstract class ApiController : ControllerBase
{
    protected Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    protected Guid? TryGetCurrentUserId()
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(rawUserId, out var parsedUserId) ? parsedUserId : null;
    }

    protected string? GetCurrentRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }

    protected IActionResult Match<T>(ErrorOr<T> result, Func<T, IActionResult> onSuccess)
    {
        return result.Match(onSuccess, errors => errors.ToProblemDetails(this));
    }
}
