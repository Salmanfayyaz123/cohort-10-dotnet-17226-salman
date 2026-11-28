using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Middleware;

// Catches every unhandled exception, logs it with Serilog and returns a clean JSON error
public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Pick the status code based on the exception type
        var (statusCode, title) = exception switch
        {
            BadRequestException => (StatusCodes.Status400BadRequest, "Bad request"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Server error")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            // Unexpected bug: log the full exception with stack trace
            logger.LogError(exception, "Unhandled exception on {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            // Expected problem (bad input, duplicate, ...): a warning is enough
            logger.LogWarning("{Title} on {Method} {Path}: {Message}",
                title, httpContext.Request.Method, httpContext.Request.Path, exception.Message);
        }

        // Never show internal error details to users in production
        var detail = statusCode == StatusCodes.Status500InternalServerError && !environment.IsDevelopment()
            ? "An unexpected error occurred. Please try again later."
            : exception.Message;

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail
            }
        });
    }
}