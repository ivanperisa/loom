using ErrorOr;
using Loom.Api.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Loom.Api.Controllers;

[ApiController]
public abstract class ApiController : ControllerBase
{
    protected int GetCurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    protected int? TryGetCurrentUserId()
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(rawUserId, out var parsedUserId) ? parsedUserId : null;
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
