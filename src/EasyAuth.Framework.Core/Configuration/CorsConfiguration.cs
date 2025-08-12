using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;

namespace EasyAuth.Framework.Core.Configuration;

/// <summary>
/// Auto-configures CORS for EasyAuth endpoints with smart origin detection
/// Eliminates manual CORS setup complexity for frontend integration
/// </summary>
public class EAuthCorsConfiguration
{
    private readonly ILogger<EAuthCorsConfiguration> _logger;
    private readonly EAuthCorsOptions _options;

    public EAuthCorsConfiguration(ILogger<EAuthCorsConfiguration> logger, IOptions<EAuthCorsOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Configures CORS services with EasyAuth-specific policies
    /// Includes auto-detection of development servers and zero-config experience
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            // Enhanced development policy with auto-detection
            options.AddPolicy("EasyAuthDevelopment", policy =>
            {
                var autoDetectedOrigins = EasyAuthDefaults.GetAllDevelopmentOrigins();
                var configuredOrigins = _options.AllowedOrigins.ToList();
                var allOrigins = autoDetectedOrigins.Concat(configuredOrigins).Distinct().ToList();

                _logger.LogInformation("Auto-detected {Count} development origins: {Origins}", 
                    autoDetectedOrigins.Count, string.Join(", ", autoDetectedOrigins.Take(5)));

                policy
                    .SetIsOriginAllowed(origin => 
                        IsDevOrigin(origin) || 
                        IsLocalhostOrigin(origin) || 
                        allOrigins.Contains(origin) ||
                        EAuthCorsDetectionMiddleware.IsAutoDetectedFramework(origin))
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
                    
                _logger.LogInformation("EasyAuthDevelopment policy configured with {Total} total origins", allOrigins.Count);
            });

            // Secure policy for production with specific origins
            options.AddPolicy("EasyAuthProduction", policy =>
            {
                policy
                    .WithOrigins(_options.AllowedOrigins.ToArray())
                    .WithMethods(_options.AllowedMethods.ToArray())
                    .WithHeaders(_options.AllowedHeaders.ToArray())
                    .AllowCredentials();
            });

            // Auto-detection policy that learns from requests
            options.AddPolicy("EasyAuthAuto", ConfigureAutoDetectionPolicy);
        });

        _logger.LogInformation("EasyAuth CORS policies configured successfully");
    }

    /// <summary>
    /// Applies CORS middleware with automatic policy selection
    /// </summary>
    public void ConfigureMiddleware(IApplicationBuilder app)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        
        string policyName = environment.Equals("Development", StringComparison.OrdinalIgnoreCase) 
            ? "EasyAuthDevelopment" 
            : _options.EnableAutoDetection 
                ? "EasyAuthAuto" 
                : "EasyAuthProduction";

        app.UseCors(policyName);
        
        _logger.LogInformation("EasyAuth CORS middleware applied with policy: {PolicyName}", policyName);
    }

    private void ConfigureAutoDetectionPolicy(CorsPolicyBuilder policy)
    {
        if (_options.EnableAutoDetection)
        {
            // Smart CORS policy that adapts to incoming requests
            policy.SetIsOriginAllowed(origin =>
            {
                // Always allow localhost for development
                if (IsLocalhostOrigin(origin))
                {
                    _logger.LogDebug("Auto-allowing localhost origin: {Origin}", origin);
                    return true;
                }

                // Check against known safe origins
                if (_options.AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Auto-allowing configured origin: {Origin}", origin);
                    return true;
                }

                // Auto-learn new origins in development
                if (IsDevOrigin(origin) && _options.AutoLearnOrigins)
                {
                    _logger.LogInformation("Auto-learning new development origin: {Origin}", origin);
                    _options.AllowedOrigins.Add(origin);
                    return true;
                }

                _logger.LogWarning("Rejecting unknown origin: {Origin}", origin);
                return false;
            });
        }
        else
        {
            policy.WithOrigins(_options.AllowedOrigins.ToArray());
        }

        policy
            .WithMethods(_options.AllowedMethods.ToArray())
            .WithHeaders(_options.AllowedHeaders.ToArray())
            .AllowCredentials();
    }

    private static bool IsLocalhostOrigin(string origin)
    {
        if (string.IsNullOrEmpty(origin)) return false;

        try
        {
            var uri = new Uri(origin);
            return uri.IsLoopback || 
                   uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                   uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                   uri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase) ||
                   uri.Host.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase) ||
                   // Support common dev server hostnames
                   uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
                   // Support any localhost port (for flexibility)
                   (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) && uri.Port >= 1024);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsDevOrigin(string origin)
    {
        if (string.IsNullOrEmpty(origin)) return false;

        try
        {
            var uri = new Uri(origin);
            
            // Enhanced development patterns for broader compatibility
            var devPatterns = new[]
            {
                "localhost",
                "127.0.0.1",
                "::1",
                "0.0.0.0",
                // Private network ranges (RFC 1918)
                "192.168.",
                "10.",
                "172.16.", "172.17.", "172.18.", "172.19.", "172.20.", 
                "172.21.", "172.22.", "172.23.", "172.24.", "172.25.", 
                "172.26.", "172.27.", "172.28.", "172.29.", "172.30.", "172.31.",
                // Development TLDs and patterns
                ".local",
                ".localhost",
                ".dev",
                ".test",
                // Docker and container patterns
                "host.docker.internal",
                // Common development subdomains
                "dev.",
                "test.",
                "staging.",
                "preview.",
                // Vercel/Netlify preview patterns
                "-preview-",
                ".vercel.app",
                ".netlify.app",
                ".surge.sh",
                // CodeSandbox, StackBlitz patterns
                "csb.app",
                "stackblitz.io",
                // Mobile development
                "expo.dev"
            };

            var host = uri.Host.ToLowerInvariant();
            var fullUri = origin.ToLowerInvariant();
            
            // Check against patterns
            return devPatterns.Any(pattern => 
                host.Contains(pattern) || fullUri.Contains(pattern)) ||
                // Allow any localhost port range
                (host == "localhost" && uri.Port >= 1024 && uri.Port <= 65535) ||
                // Allow common dev ports on any localhost-like host
                IsCommonDevPort(uri.Port);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsCommonDevPort(int port)
    {
        // Common development server ports
        var commonPorts = new[]
        {
            3000, 3001, 3002, 3003, // React, Next.js
            4200, 4201, 4202,       // Angular
            5000, 5001, 5002,       // ASP.NET, Svelte
            5173, 5174,             // Vite
            8000, 8001, 8080, 8081, 8888, // Various dev servers
            9000, 9001, 9002,       // Webpack, various
            8080, 8888              // Vue CLI, other tools
        };
        
        return commonPorts.Contains(port) || 
               (port >= 3000 && port <= 3010) || // React range
               (port >= 8000 && port <= 8010) || // General dev range
               (port >= 9000 && port <= 9010);   // Build tool range
    }
}

/// <summary>
/// Configuration options for EasyAuth CORS behavior
/// </summary>
public class EAuthCorsOptions
{
    /// <summary>
    /// Explicitly allowed origins
    /// </summary>
    public List<string> AllowedOrigins { get; set; } = new()
    {
        // React development servers
        "https://localhost:3000", "http://localhost:3000",
        "https://127.0.0.1:3000", "http://127.0.0.1:3000",
        
        // Vite development servers (Vue, React, etc)
        "https://localhost:5173", "http://localhost:5173",
        "https://127.0.0.1:5173", "http://127.0.0.1:5173",
        
        // Vue CLI development servers
        "https://localhost:8080", "http://localhost:8080",
        "https://127.0.0.1:8080", "http://127.0.0.1:8080",
        
        // Angular development servers
        "https://localhost:4200", "http://localhost:4200",
        "https://127.0.0.1:4200", "http://127.0.0.1:4200",
        
        // Next.js development servers
        "https://localhost:3001", "http://localhost:3001",
        "https://127.0.0.1:3001", "http://127.0.0.1:3001",
        
        // Svelte development servers
        "https://localhost:5000", "http://localhost:5000",
        "https://127.0.0.1:5000", "http://127.0.0.1:5000",
        
        // Nuxt.js development servers
        "https://localhost:3002", "http://localhost:3002",
        "https://127.0.0.1:3002", "http://127.0.0.1:3002",
        
        // Common development ports
        "https://localhost:8000", "http://localhost:8000",
        "https://localhost:8001", "http://localhost:8001",
        "https://localhost:8888", "http://localhost:8888",
        "https://localhost:9000", "http://localhost:9000",
        "https://localhost:9001", "http://localhost:9001"
    };

    /// <summary>
    /// Allowed HTTP methods
    /// </summary>
    public List<string> AllowedMethods { get; set; } = new()
    {
        "GET",
        "POST",
        "PUT",
        "DELETE",
        "OPTIONS"
    };

    /// <summary>
    /// Allowed HTTP headers
    /// </summary>
    public List<string> AllowedHeaders { get; set; } = new()
    {
        // Standard CORS and auth headers
        "Authorization",
        "Content-Type",
        "Accept",
        "Origin",
        "Access-Control-Request-Method",
        "Access-Control-Request-Headers",
        "X-Requested-With",
        
        // API and authentication headers
        "X-API-Key",
        "X-Client-Version",
        "X-Client-Id",
        "X-Session-Token",
        "X-CSRF-Token",
        
        // Framework-specific headers
        "X-EasyAuth-Provider",
        "X-EasyAuth-Session",
        "X-EasyAuth-Nonce",
        
        // Common frontend headers
        "Cache-Control",
        "Pragma",
        "Expires",
        "If-Modified-Since",
        "If-None-Match",
        
        // Development and debugging headers
        "X-Debug-Info",
        "X-Trace-Id",
        "X-Request-Id",
        
        // Mobile and PWA headers
        "User-Agent",
        "X-User-Agent",
        "X-Platform",
        "X-App-Version"
    };

    /// <summary>
    /// Enable automatic origin detection and learning
    /// </summary>
    public bool EnableAutoDetection { get; set; } = true;

    /// <summary>
    /// Automatically learn and remember new development origins
    /// </summary>
    public bool AutoLearnOrigins { get; set; } = true;

    /// <summary>
    /// Maximum age for preflight cache in seconds
    /// </summary>
    public int PreflightMaxAge { get; set; } = 86400; // 24 hours
}

/// <summary>
/// Middleware to automatically detect and configure CORS for frontend origins
/// </summary>
public class EAuthCorsDetectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<EAuthCorsDetectionMiddleware> _logger;
    private readonly EAuthCorsOptions _options;
    private readonly HashSet<string> _detectedOrigins = new();

    public EAuthCorsDetectionMiddleware(
        RequestDelegate next,
        ILogger<EAuthCorsDetectionMiddleware> logger,
        IOptions<EAuthCorsOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.FirstOrDefault();
        
        if (!string.IsNullOrEmpty(origin) && IsEAuthRequest(context))
        {
            await DetectAndConfigureCorsAsync(context, origin);
        }

        await _next(context);
    }

    private async Task DetectAndConfigureCorsAsync(HttpContext context, string origin)
    {
        if (!_detectedOrigins.Contains(origin))
        {
            _logger.LogInformation("Detected new frontend origin for EasyAuth: {Origin}", origin);
            _detectedOrigins.Add(origin);

            // Add common CORS headers for detected origins
            if (IsDevOrigin(origin) || _options.AllowedOrigins.Contains(origin))
            {
                context.Response.Headers.AccessControlAllowOrigin = origin;
                context.Response.Headers.AccessControlAllowCredentials = "true";
                context.Response.Headers.AccessControlAllowMethods = string.Join(", ", _options.AllowedMethods);
                context.Response.Headers.AccessControlAllowHeaders = string.Join(", ", _options.AllowedHeaders);
                context.Response.Headers.AccessControlMaxAge = _options.PreflightMaxAge.ToString();
            }
        }

        await Task.CompletedTask;
    }

    private static bool IsEAuthRequest(HttpContext context)
    {
        var path = context.Request.Path.Value;
        return path?.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase) == true ||
               path?.StartsWith("/easyauth", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsDevOrigin(string origin)
    {
        if (string.IsNullOrEmpty(origin)) return false;

        try
        {
            var uri = new Uri(origin);
            var host = uri.Host.ToLowerInvariant();
            
            return uri.IsLoopback || 
                   host.Contains("localhost") ||
                   host.Contains("127.0.0.1") ||
                   host.Contains("192.168.") ||
                   host.Contains("10.") ||
                   host.Contains("172.") ||
                   host.EndsWith(".local") ||
                   host.EndsWith(".localhost") ||
                   host.EndsWith(".dev") ||
                   host.EndsWith(".test") ||
                   host.Contains("host.docker.internal") ||
                   origin.ToLowerInvariant().Contains("preview") ||
                   origin.ToLowerInvariant().Contains("staging") ||
                   IsCommonDevPortForMiddleware(uri.Port);
        }
        catch
        {
            return false;
        }
    }

    public static bool IsAutoDetectedFramework(string origin)
    {
        if (string.IsNullOrEmpty(origin)) return false;

        try
        {
            var uri = new Uri(origin);
            var port = uri.Port.ToString();
            
            // Check if this origin matches any of our auto-detected development patterns
            return uri.IsLoopback && EasyAuthDefaults.CommonDevPorts.Contains(port);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsCommonDevPortForMiddleware(int port)
    {
        // Common development server ports
        var commonPorts = new[]
        {
            3000, 3001, 3002, 3003, // React, Next.js
            4200, 4201, 4202,       // Angular
            5000, 5001, 5002,       // ASP.NET, Svelte
            5173, 5174,             // Vite
            8000, 8001, 8080, 8081, 8888, // Various dev servers
            9000, 9001, 9002,       // Webpack, various
            8080, 8888              // Vue CLI, other tools
        };
        
        return commonPorts.Contains(port) || 
               (port >= 3000 && port <= 3010) || // React range
               (port >= 8000 && port <= 8010) || // General dev range
               (port >= 9000 && port <= 9010);   // Build tool range
    }
}