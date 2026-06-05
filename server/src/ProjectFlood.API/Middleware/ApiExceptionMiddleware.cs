using System.Net;
using MySqlConnector;
using ProjectFlood.Application.Common;
using ProjectFlood.Contracts.Common;

namespace ProjectFlood.API.Middleware;

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (GameApiException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsJsonAsync(new ErrorResponse { Code = ex.Code, Message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsJsonAsync(new ErrorResponse { Code = "UNAUTHORIZED", Message = ex.Message });
        }
        catch (MySqlException ex) when (ex.Number is 1205 or 1213 or 3572)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            await context.Response.WriteAsJsonAsync(new ErrorResponse
            {
                Code = "CONCURRENT_MODIFICATION",
                Message = "The request conflicted with another update.",
            });
        }
        catch (Exception ex)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString();
            _logger.LogError(ex, "Unhandled exception [{CorrelationId}] {Method} {Path}",
                correlationId, context.Request.Method, context.Request.Path);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsJsonAsync(new ErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "Internal server error.",
            });
        }
    }
}
