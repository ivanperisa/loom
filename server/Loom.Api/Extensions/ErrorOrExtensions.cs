using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Extensions;

public static class ErrorOrExtensions
{
    public static ActionResult ToProblemDetails(this IEnumerable<Error> errors, ControllerBase controller)
    {
        var errorList = errors.ToList();
        var primaryError = errorList.First();

        var statusCode = primaryError.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Failure => StatusCodes.Status500InternalServerError,
            ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };

        var title = primaryError.Type switch
        {
            ErrorType.Validation => "Validation Error",
            ErrorType.Unauthorized => "Unauthorized",
            ErrorType.Forbidden => "Forbidden",
            ErrorType.NotFound => "Not Found",
            ErrorType.Conflict => "Conflict",
            ErrorType.Failure => "Operation Failed",
            ErrorType.Unexpected => "Unexpected Error",
            _ => "An error occurred"
        };

        var extensions = new Dictionary<string, object?>
        {
            ["code"] = primaryError.Code
        };

        // Values the client needs to word the message itself (course id, available ECTS, ...).
        if (primaryError.Metadata is { Count: > 0 } metadata)
            extensions["params"] = metadata;

        if (errorList.Count > 1)
        {
            extensions["errors"] = errorList.Select(e => new
            {
                code = e.Code,
                description = e.Description
            });
        }

        return controller.Problem(
            detail: primaryError.Description,
            statusCode: statusCode,
            title: title,
            extensions: extensions);
    }
}
