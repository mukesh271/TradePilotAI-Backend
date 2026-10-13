using System.Net;
using TradePilot.Api.DTOs;

namespace TradePilot.Api.Middleware;

/// <summary>
/// Global exception handling middleware for consistent error responses.
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred.");

        var response = context.Response;
        response.ContentType = "application/json";

        var apiResponse = exception switch
        {
            ArgumentException argEx => new
            {
                success = false,
                message = "Invalid argument provided.",
                errors = new[] { argEx.Message }
            },

            InvalidOperationException invOpEx => new
            {
                success = false,
                message = "Invalid operation.",
                errors = new[] { invOpEx.Message }
            },

            _ => new
            {
                success = false,
                message = "An internal server error occurred. Please try again later.",
                errors = new string[] { }
            }
        };

        response.StatusCode = (int)HttpStatusCode.InternalServerError;

        return response.WriteAsJsonAsync(apiResponse);
    }
}
