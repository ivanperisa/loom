using ErrorOr;
using Loom.Api.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Controllers;

[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
public abstract class ApiController : ControllerBase
{
    /// <summary>Success → <paramref name="onSuccess"/>; errors → ProblemDetails with the error code.</summary>
    protected ActionResult Match<T>(ErrorOr<T> result, Func<T, ActionResult> onSuccess) =>
        result.Match(onSuccess, errors => errors.ToProblemDetails(this));
}
