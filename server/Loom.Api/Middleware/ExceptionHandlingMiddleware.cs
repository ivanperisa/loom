using Microsoft.AspNetCore.Mvc;

namespace Loom.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, code) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "NOT_FOUND"),
            ArgumentException => (StatusCodes.Status400BadRequest, "BAD_REQUEST"),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "INVALID_OPERATION"),
            _ => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR")
        };

        var title = statusCode switch
        {
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status400BadRequest => "Bad Request",
            _ => "Internal Server Error"
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : exception.Message
        };
        problemDetails.Extensions["code"] = code;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
