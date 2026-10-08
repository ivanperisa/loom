using ErrorOr;
using Loom.Api.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[ApiController]
public abstract class ApiController : ControllerBase
{
    /// <summary>Success → <paramref name="onSuccess"/>; errors → ProblemDetails with the error code.</summary>
    protected IActionResult Match<T>(ErrorOr<T> result, Func<T, IActionResult> onSuccess) =>
        result.Match(onSuccess, errors => errors.ToProblemDetails(this));
}
