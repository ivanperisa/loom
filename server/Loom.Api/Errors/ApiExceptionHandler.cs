using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Loom.Api.Errors;

/// <summary>
/// Last line of defence for exceptions. Expected business failures are ErrorOr results, not exceptions;
/// what reaches this handler is mapped to a ProblemDetails with a stable <c>code</c> and never leaks internals
/// (exception details are only included in Development).
/// </summary>
public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetails,
    IHostEnvironment environment,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = 499;   // client closed the request; nobody is listening
            return true;
        }

        var (status, code, detail) = exception switch
        {
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "CONCURRENT_UPDATE",
                "Someone else changed this at the same time. Reload and try again."),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                (StatusCodes.Status409Conflict, "DUPLICATE", "A record with the same value already exists."),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } } =>
                (StatusCodes.Status409Conflict, "IN_USE", "This record is still referenced by other data."),
            BadHttpRequestException bad => (bad.StatusCode, "BAD_REQUEST", "The request could not be read."),
            _ => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred."),
        };

        if (status >= 500) logger.LogError(exception, "Unhandled exception");
        else logger.LogWarning(exception, "Request failed with {Code}", code);

        var problem = new ProblemDetails { Status = status, Title = ReasonPhrase(status), Detail = detail };
        problem.Extensions["code"] = code;
        if (environment.IsDevelopment()) problem.Extensions["exception"] = exception.ToString();

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem, Exception = exception });
    }

    private static string ReasonPhrase(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status409Conflict => "Conflict",
        _ => "Internal Server Error",
    };
}
