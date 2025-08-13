using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EasyAuth.Framework.Core.Security;

/// <summary>
/// Comprehensive input validation middleware for enhanced security
/// Protects against injection attacks, malformed requests, and suspicious patterns
/// </summary>
public class InputValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InputValidationMiddleware> _logger;
    private readonly InputValidationOptions _options;

    // Security patterns to detect potential attacks
    private static readonly Regex[] SuspiciousPatterns = new[]
    {
        new Regex(@"<script[^>]*>.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"javascript:", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"on\w+\s*=", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"(\bUNION\b|\bSELECT\b|\bINSERT\b|\bDELETE\b|\bDROP\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"(--|\#|\/\*|\*\/)", RegexOptions.Compiled),
        new Regex(@"(\||;|&|\$\(|\`)", RegexOptions.Compiled),
        new Regex(@"(\.\./|\.\.\\)", RegexOptions.Compiled) // Path traversal
    };

    public InputValidationMiddleware(
        RequestDelegate next,
        ILogger<InputValidationMiddleware> logger,
        InputValidationOptions? options = null)
    {
        _next = next;
        _logger = logger;
        _options = options ?? new InputValidationOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Validate request size
            if (await ValidateRequestSizeAsync(context))
            {
                await _next(context);
                return;
            }

            // Validate headers
            if (ValidateHeaders(context))
            {
                await _next(context);
                return;
            }

            // Validate query parameters
            if (ValidateQueryParameters(context))
            {
                await _next(context);
                return;
            }

            // Validate form data if present
            if (context.Request.HasFormContentType)
            {
                if (await ValidateFormDataAsync(context))
                {
                    await _next(context);
                    return;
                }
            }

            // Validate JSON body if present
            if (context.Request.ContentType?.Contains("application/json") == true)
            {
                if (await ValidateJsonBodyAsync(context))
                {
                    await _next(context);
                    return;
                }
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in input validation middleware");
            await HandleValidationError(context, "Internal validation error");
        }
    }

    private async Task<bool> ValidateRequestSizeAsync(HttpContext context)
    {
        if (context.Request.ContentLength > _options.MaxRequestSizeBytes)
        {
            _logger.LogWarning("Request size {Size} exceeds maximum {Max} from {IP}",
                context.Request.ContentLength, _options.MaxRequestSizeBytes, context.Connection.RemoteIpAddress);
            
            await HandleValidationError(context, "Request too large");
            return false;
        }
        return true;
    }

    private bool ValidateHeaders(HttpContext context)
    {
        foreach (var header in context.Request.Headers)
        {
            if (header.Value.Any(value => value != null && ContainsSuspiciousPattern(value)))
            {
                _logger.LogWarning("Suspicious pattern detected in header {HeaderName} from {IP}",
                    header.Key, context.Connection.RemoteIpAddress);
                
                HandleValidationError(context, "Invalid header content").Wait();
                return false;
            }

            // Check header length
            if (header.Value.Any(value => value?.Length > _options.MaxHeaderValueLength))
            {
                _logger.LogWarning("Header value too long in {HeaderName} from {IP}",
                    header.Key, context.Connection.RemoteIpAddress);
                
                HandleValidationError(context, "Header value too long").Wait();
                return false;
            }
        }
        return true;
    }

    private bool ValidateQueryParameters(HttpContext context)
    {
        foreach (var param in context.Request.Query)
        {
            if (param.Value.Any(value => value != null && ContainsSuspiciousPattern(value)))
            {
                _logger.LogWarning("Suspicious pattern detected in query parameter {ParamName} from {IP}",
                    param.Key, context.Connection.RemoteIpAddress);
                
                HandleValidationError(context, "Invalid query parameter").Wait();
                return false;
            }

            // Check parameter length
            if (param.Value.Any(value => value?.Length > _options.MaxParameterLength))
            {
                _logger.LogWarning("Query parameter too long {ParamName} from {IP}",
                    param.Key, context.Connection.RemoteIpAddress);
                
                HandleValidationError(context, "Query parameter too long").Wait();
                return false;
            }
        }
        return true;
    }

    private async Task<bool> ValidateFormDataAsync(HttpContext context)
    {
        try
        {
            var form = await context.Request.ReadFormAsync();
            
            foreach (var field in form)
            {
                if (field.Value.Any(value => value != null && ContainsSuspiciousPattern(value)))
                {
                    _logger.LogWarning("Suspicious pattern detected in form field {FieldName} from {IP}",
                        field.Key, context.Connection.RemoteIpAddress);
                    
                    await HandleValidationError(context, "Invalid form data");
                    return false;
                }

                // Check field length
                if (field.Value.Any(value => value?.Length > _options.MaxFieldLength))
                {
                    _logger.LogWarning("Form field too long {FieldName} from {IP}",
                        field.Key, context.Connection.RemoteIpAddress);
                    
                    await HandleValidationError(context, "Form field too long");
                    return false;
                }
            }
        }
        catch (InvalidDataException)
        {
            _logger.LogWarning("Invalid form data from {IP}", context.Connection.RemoteIpAddress);
            await HandleValidationError(context, "Invalid form format");
            return false;
        }

        return true;
    }

    private async Task<bool> ValidateJsonBodyAsync(HttpContext context)
    {
        try
        {
            context.Request.EnableBuffering();
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (string.IsNullOrEmpty(body))
                return true;

            // Check JSON size
            if (body.Length > _options.MaxJsonSizeBytes)
            {
                _logger.LogWarning("JSON body too large {Size} from {IP}",
                    body.Length, context.Connection.RemoteIpAddress);
                
                await HandleValidationError(context, "JSON body too large");
                return false;
            }

            // Validate JSON structure
            try
            {
                JsonDocument.Parse(body);
            }
            catch (JsonException)
            {
                _logger.LogWarning("Invalid JSON format from {IP}", context.Connection.RemoteIpAddress);
                await HandleValidationError(context, "Invalid JSON format");
                return false;
            }

            // Check for suspicious patterns in JSON
            if (ContainsSuspiciousPattern(body))
            {
                _logger.LogWarning("Suspicious pattern detected in JSON body from {IP}",
                    context.Connection.RemoteIpAddress);
                
                await HandleValidationError(context, "Invalid JSON content");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating JSON body from {IP}", context.Connection.RemoteIpAddress);
            await HandleValidationError(context, "JSON validation error");
            return false;
        }

        return true;
    }

    private static bool ContainsSuspiciousPattern(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        return SuspiciousPatterns.Any(pattern => pattern.IsMatch(input));
    }

    private async Task HandleValidationError(HttpContext context, string message)
    {
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "Validation Error",
            message = message,
            timestamp = DateTimeOffset.UtcNow,
            correlationId = context.Items["CorrelationId"]?.ToString()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

/// <summary>
/// Configuration options for input validation middleware
/// </summary>
public class InputValidationOptions
{
    /// <summary>
    /// Maximum request size in bytes (default: 10MB)
    /// </summary>
    public long MaxRequestSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Maximum header value length (default: 8KB)
    /// </summary>
    public int MaxHeaderValueLength { get; set; } = 8 * 1024;

    /// <summary>
    /// Maximum query parameter length (default: 2KB)
    /// </summary>
    public int MaxParameterLength { get; set; } = 2 * 1024;

    /// <summary>
    /// Maximum form field length (default: 1MB)
    /// </summary>
    public int MaxFieldLength { get; set; } = 1024 * 1024;

    /// <summary>
    /// Maximum JSON body size in bytes (default: 5MB)
    /// </summary>
    public long MaxJsonSizeBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>
    /// Whether to enable suspicious pattern detection (default: true)
    /// </summary>
    public bool EnablePatternDetection { get; set; } = true;

    /// <summary>
    /// Whether to log validation failures (default: true)
    /// </summary>
    public bool EnableLogging { get; set; } = true;
}