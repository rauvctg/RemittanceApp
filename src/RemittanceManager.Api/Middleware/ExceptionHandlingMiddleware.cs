using System.Net;
using System.Text.Json;
using RemittanceManager.Application.Common.Exceptions;
using RemittanceManager.Application.Common.Constants;

namespace RemittanceManager.Api.Middleware;

/// <summary>
/// Maps <see cref="AppException"/> instances to RFC 7807 problem+json responses.
/// Unhandled exceptions become a generic 500 without leaking internal details.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (status, title) = ex switch
        {
            ValidationException => (HttpStatusCode.BadRequest, ValidationMessages.ValidationFailed),
            UnauthorizedException => (HttpStatusCode.Unauthorized, ErrorMessages.Unauthorized),
            NotFoundException => (HttpStatusCode.NotFound, ErrorMessages.NotFound),
            ConflictException => (HttpStatusCode.Conflict, ErrorMessages.Conflict),
            _ => (HttpStatusCode.InternalServerError, ErrorMessages.InternalServerError)
        };

        if (status == HttpStatusCode.InternalServerError)
            _logger.LogError(ex, ErrorMessages.UnhandledExceptionLog, context.Request.Path);
        else
            _logger.LogInformation("Handled {ExceptionType}: {Message}", ex.GetType().Name, ex.Message);

        var payload = new Dictionary<string, object?>
        {
            ["title"] = title,
            ["status"] = (int)status,
            ["detail"] = status == HttpStatusCode.InternalServerError ? ErrorMessages.UnexpectedError : ex.Message
        };

        if (ex is ValidationException ve && ve.Errors.Count > 0)
            payload["errors"] = ve.Errors;

        context.Response.Clear();
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
