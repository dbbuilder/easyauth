using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EasyAuth.Framework.Core.Models;

namespace EasyAuth.Framework.Core.Extensions;

/// <summary>
/// Extension methods for controllers to return consistent API responses
/// Ensures all EasyAuth endpoints follow the same response format
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// Returns a successful response with data
    /// </summary>
    public static IActionResult ApiOk<T>(this ControllerBase controller, T data, string? message = null)
    {
        var correlationId = GetCorrelationId(controller);
        return controller.Ok(ApiResponse<T>.Ok(data, message, correlationId));
    }

    /// <summary>
    /// Returns a successful response without data
    /// </summary>
    public static IActionResult ApiOk(this ControllerBase controller, string? message = null)
    {
        var correlationId = GetCorrelationId(controller);
        return controller.Ok(ApiResponse<object>.Ok(message, correlationId));
    }

    /// <summary>
    /// Returns a created response with data
    /// </summary>
    public static IActionResult ApiCreated<T>(this ControllerBase controller, T data, string? message = null)
    {
        var correlationId = GetCorrelationId(controller);
        return controller.StatusCode(StatusCodes.Status201Created,
            ApiResponse<T>.Ok(data, message ?? "Resource created successfully", correlationId));
    }

    /// <summary>
    /// Returns a bad request response
    /// </summary>
    public static IActionResult ApiBadRequest(this ControllerBase controller, string error, string? message = null, object? errorDetails = null)
    {
        var correlationId = GetCorrelationId(controller);
        return controller.BadRequest(ApiResponse<object>.CreateError(error, message, errorDetails, correlationId));
    }

    /// <summary>
    /// Returns a validation error response
    /// </summary>
    public static IActionResult ApiValidationError(this ControllerBase controller, string message, Dictionary<string, string[]>? validationErrors = null)
    {
        var correlationId = GetCorrelationId(controller);
        return controller.BadRequest(ApiResponse<object>.ValidationError(message, validationErrors, correlationId));
    }

    /// <summary>
    /// Returns an unauthorized response
    /// </summary>
    public static IActionResult ApiUnauthorized(this ControllerBase controller, string? message = null)
    {
        var correlationId = GetCorrelationId(controller);
        return controller.Unauthorized(ApiResponse<object>.Unauthorized(message, correlationId));
    }

    /// <summary>
    /// Returns a forbidden response
    /// </summary>
    public static IActionResult ApiForbidden(this ControllerBase controller, string? message = null)
    {
        var correlationId = GetCorrelationId(controller);
        return controller.StatusCode(StatusCodes.Status403Forbidden,
            ApiResponse<object>.Forbidden(message, correlationId));
    }

    /// <summary>
    /// Returns a not found response
    /// </summary>
    public static IActionResult ApiNotFound(this ControllerBase controller, string? message = null)
    {
        var correlationId = GetCorrelationId(controller);
        return controller.NotFound(ApiResponse<object>.NotFound(message, correlationId));
    }

    /// <summary>
    /// Returns an internal server error response
    /// </summary>
    public static IActionResult ApiInternalError(this ControllerBase controller, string? message = null, object? errorDetails = null)
    {
        var correlationId = GetCorrelationId(controller);
        return controller.StatusCode(StatusCodes.Status500InternalServerError,
            ApiResponse<object>.InternalError(message, errorDetails, correlationId));
    }

    /// <summary>
    /// Returns a conflict response
    /// </summary>
    public static IActionResult ApiConflict(this ControllerBase controller, string error, string? message = null, object? errorDetails = null)
    {
        var correlationId = GetCorrelationId(controller);
        return controller.Conflict(ApiResponse<object>.CreateError(error, message, errorDetails, correlationId));
    }

    /// <summary>
    /// Returns a custom status code response
    /// </summary>
    public static IActionResult ApiResponseWithStatus<T>(this ControllerBase controller, int statusCode, ApiResponse<T> response)
    {
        if (string.IsNullOrEmpty(response.CorrelationId))
        {
            response.CorrelationId = GetCorrelationId(controller);
        }
        return controller.StatusCode(statusCode, response);
    }


    /// <summary>
    /// Gets or generates a correlation ID for request tracing
    /// </summary>
    private static string GetCorrelationId(ControllerBase controller)
    {
        // Try to get correlation ID from headers
        if (controller.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId) && 
            !string.IsNullOrEmpty(correlationId))
        {
            return correlationId.ToString();
        }

        // Try to get from HttpContext items
        if (controller.HttpContext.Items.TryGetValue("CorrelationId", out var contextCorrelationId) && 
            contextCorrelationId is string correlationIdString)
        {
            return correlationIdString;
        }

        // Generate new correlation ID
        var newCorrelationId = Guid.NewGuid().ToString("N")[..12]; // Short correlation ID
        controller.HttpContext.Items["CorrelationId"] = newCorrelationId;
        return newCorrelationId;
    }
}

/// <summary>
/// Middleware to add correlation ID to all responses
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get or generate correlation ID
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
                           ?? Guid.NewGuid().ToString("N")[..12];

        // Store in HttpContext for controllers to use
        context.Items["CorrelationId"] = correlationId;

        // Add to response headers
        context.Response.Headers["X-Correlation-ID"] = correlationId;

        await _next(context);
    }
}