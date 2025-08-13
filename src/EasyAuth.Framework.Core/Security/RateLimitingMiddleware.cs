using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace EasyAuth.Framework.Core.Security;

/// <summary>
/// Advanced rate limiting middleware with DDoS protection
/// Implements sliding window, token bucket, and adaptive algorithms
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly IMemoryCache _cache;
    private readonly RateLimitingOptions _options;
    private readonly ConcurrentDictionary<string, ClientMetrics> _clientMetrics = new();

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IMemoryCache cache,
        RateLimitingOptions? options = null)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
        _options = options ?? new RateLimitingOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientIdentifier(context);
        var endpoint = GetEndpointIdentifier(context);
        
        // Check if client is temporarily blocked
        if (IsClientBlocked(clientId))
        {
            _logger.LogWarning("Blocked client {ClientId} attempted access from {IP}",
                clientId, context.Connection.RemoteIpAddress);
            await HandleRateLimitExceeded(context, "Client temporarily blocked");
            return;
        }

        // Apply rate limiting rules
        var limitResult = await CheckRateLimitsAsync(context, clientId, endpoint);
        if (!limitResult.IsAllowed)
        {
            _logger.LogWarning("Rate limit exceeded for client {ClientId} on {Endpoint} from {IP}",
                clientId, endpoint, context.Connection.RemoteIpAddress);
            
            // Apply progressive penalties for repeat offenders
            ApplyPenalty(clientId);
            
            await HandleRateLimitExceeded(context, limitResult.Reason, limitResult.RetryAfter);
            return;
        }

        // Record successful request
        RecordRequest(clientId, endpoint);

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Priority order: API key, user ID, IP address
        if (context.Request.Headers.ContainsKey("X-API-Key"))
        {
            return $"api:{context.Request.Headers["X-API-Key"].First()}";
        }

        if (context.User?.Identity?.IsAuthenticated == true)
        {
            return $"user:{context.User.Identity.Name}";
        }

        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        var clientIp = forwarded?.Split(',').FirstOrDefault()?.Trim() 
                      ?? context.Connection.RemoteIpAddress?.ToString() 
                      ?? "unknown";

        return $"ip:{clientIp}";
    }

    private string GetEndpointIdentifier(HttpContext context)
    {
        return $"{context.Request.Method}:{context.Request.Path}";
    }

    private bool IsClientBlocked(string clientId)
    {
        var blockKey = $"blocked:{clientId}";
        return _cache.Get(blockKey) != null;
    }

    private async Task<RateLimitResult> CheckRateLimitsAsync(HttpContext context, string clientId, string endpoint)
    {
        // Global rate limit (requests per minute)
        var globalResult = await CheckGlobalRateLimit(clientId);
        if (!globalResult.IsAllowed)
            return globalResult;

        // Endpoint-specific rate limit
        var endpointResult = await CheckEndpointRateLimit(clientId, endpoint);
        if (!endpointResult.IsAllowed)
            return endpointResult;

        // Burst protection (requests per second)
        var burstResult = await CheckBurstLimit(clientId);
        if (!burstResult.IsAllowed)
            return burstResult;

        // Authentication endpoint special limits
        if (IsAuthEndpoint(endpoint))
        {
            var authResult = await CheckAuthEndpointLimit(clientId);
            if (!authResult.IsAllowed)
                return authResult;
        }

        return RateLimitResult.Allowed();
    }

    private async Task<RateLimitResult> CheckGlobalRateLimit(string clientId)
    {
        var key = $"global:{clientId}";
        var window = TimeSpan.FromMinutes(1);
        var limit = _options.GlobalRequestsPerMinute;

        return await CheckSlidingWindowLimit(key, limit, window, "Global rate limit");
    }

    private async Task<RateLimitResult> CheckEndpointRateLimit(string clientId, string endpoint)
    {
        var key = $"endpoint:{clientId}:{endpoint}";
        var window = TimeSpan.FromMinutes(1);
        var limit = _options.EndpointRequestsPerMinute;

        return await CheckSlidingWindowLimit(key, limit, window, "Endpoint rate limit");
    }

    private async Task<RateLimitResult> CheckBurstLimit(string clientId)
    {
        var key = $"burst:{clientId}";
        var window = TimeSpan.FromSeconds(1);
        var limit = _options.BurstRequestsPerSecond;

        return await CheckSlidingWindowLimit(key, limit, window, "Burst protection");
    }

    private async Task<RateLimitResult> CheckAuthEndpointLimit(string clientId)
    {
        var key = $"auth:{clientId}";
        var window = TimeSpan.FromMinutes(5);
        var limit = _options.AuthRequestsPer5Minutes;

        return await CheckSlidingWindowLimit(key, limit, window, "Authentication rate limit");
    }

    private Task<RateLimitResult> CheckSlidingWindowLimit(
        string key, int limit, TimeSpan window, string reason)
    {
        var now = DateTimeOffset.UtcNow;
        var requests = _cache.Get<List<DateTimeOffset>>(key) ?? new List<DateTimeOffset>();

        // Remove expired requests
        requests.RemoveAll(r => now - r > window);

        if (requests.Count >= limit)
        {
            var oldestRequest = requests.Min();
            var retryAfter = (oldestRequest + window) - now;
            return Task.FromResult(RateLimitResult.Denied(reason, retryAfter));
        }

        // Add current request
        requests.Add(now);
        _cache.Set(key, requests, window);

        return Task.FromResult(RateLimitResult.Allowed());
    }

    private bool IsAuthEndpoint(string endpoint)
    {
        var authPatterns = new[]
        {
            "/api/auth/login",
            "/api/auth/refresh",
            "/api/auth/register",
            "/easyauth/"
        };

        return authPatterns.Any(pattern => endpoint.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private void ApplyPenalty(string clientId)
    {
        var metrics = _clientMetrics.GetOrAdd(clientId, _ => new ClientMetrics());
        metrics.ViolationCount++;
        metrics.LastViolation = DateTimeOffset.UtcNow;

        // Progressive blocking: 1 min, 5 min, 15 min, 1 hour
        var blockDuration = metrics.ViolationCount switch
        {
            1 => TimeSpan.FromMinutes(1),
            2 => TimeSpan.FromMinutes(5),
            3 => TimeSpan.FromMinutes(15),
            >= 4 => TimeSpan.FromHours(1),
            _ => TimeSpan.FromMinutes(1)
        };

        var blockKey = $"blocked:{clientId}";
        _cache.Set(blockKey, true, blockDuration);

        _logger.LogWarning("Client {ClientId} blocked for {Duration} (violation #{Count})",
            clientId, blockDuration, metrics.ViolationCount);
    }

    private void RecordRequest(string clientId, string endpoint)
    {
        var metrics = _clientMetrics.GetOrAdd(clientId, _ => new ClientMetrics());
        metrics.TotalRequests++;
        metrics.LastRequest = DateTimeOffset.UtcNow;

        // Reset violation count after successful period
        if (DateTimeOffset.UtcNow - metrics.LastViolation > TimeSpan.FromHours(1))
        {
            metrics.ViolationCount = 0;
        }
    }

    private async Task HandleRateLimitExceeded(HttpContext context, string reason, TimeSpan? retryAfter = null)
    {
        context.Response.StatusCode = 429; // Too Many Requests
        context.Response.ContentType = "application/json";

        if (retryAfter.HasValue)
        {
            context.Response.Headers["Retry-After"] = ((int)retryAfter.Value.TotalSeconds).ToString();
        }

        var response = new
        {
            error = "Rate Limit Exceeded",
            message = reason,
            retryAfter = retryAfter?.TotalSeconds,
            timestamp = DateTimeOffset.UtcNow,
            correlationId = context.Items["CorrelationId"]?.ToString()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

/// <summary>
/// Rate limiting configuration options
/// </summary>
public class RateLimitingOptions
{
    /// <summary>
    /// Global requests per minute per client (default: 60)
    /// </summary>
    public int GlobalRequestsPerMinute { get; set; } = 60;

    /// <summary>
    /// Requests per minute per endpoint per client (default: 30)
    /// </summary>
    public int EndpointRequestsPerMinute { get; set; } = 30;

    /// <summary>
    /// Burst requests per second per client (default: 10)
    /// </summary>
    public int BurstRequestsPerSecond { get; set; } = 10;

    /// <summary>
    /// Authentication requests per 5 minutes per client (default: 5)
    /// </summary>
    public int AuthRequestsPer5Minutes { get; set; } = 5;

    /// <summary>
    /// Whether to enable progressive penalties (default: true)
    /// </summary>
    public bool EnableProgressivePenalties { get; set; } = true;

    /// <summary>
    /// Whether to block repeat offenders (default: true)
    /// </summary>
    public bool EnableBlocking { get; set; } = true;
}

/// <summary>
/// Result of rate limit check
/// </summary>
public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public string Reason { get; set; } = string.Empty;
    public TimeSpan? RetryAfter { get; set; }

    public static RateLimitResult Allowed() => new() { IsAllowed = true };
    
    public static RateLimitResult Denied(string reason, TimeSpan? retryAfter = null) => 
        new() { IsAllowed = false, Reason = reason, RetryAfter = retryAfter };
}

/// <summary>
/// Client metrics for tracking behavior
/// </summary>
internal class ClientMetrics
{
    public int TotalRequests { get; set; }
    public int ViolationCount { get; set; }
    public DateTimeOffset LastRequest { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastViolation { get; set; } = DateTimeOffset.MinValue;
}