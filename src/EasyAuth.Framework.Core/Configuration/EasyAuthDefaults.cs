using System.Diagnostics;

namespace EasyAuth.Framework.Core.Configuration;

/// <summary>
/// Default configuration values and auto-detection logic for EasyAuth Framework
/// Provides zero-configuration development experience
/// </summary>
public static class EasyAuthDefaults
{
    /// <summary>
    /// Common development server ports used by popular frontend frameworks
    /// </summary>
    public static readonly string[] CommonDevPorts = 
    {
        "3000", // React (Create React App), Node.js, Next.js
        "5173", // Vite (Vue, React, Svelte)
        "4173", // Vite preview mode
        "8080", // Vue CLI, webpack-dev-server, serve
        "4200", // Angular CLI
        "3001", // Next.js alternate, React alternate
        "8000", // Python Django, simple HTTP servers
        "5000", // ASP.NET Core default (for SPA proxy)
        "5001", // ASP.NET Core HTTPS default
        "8081", // Vue CLI alternate
        "9000", // Various build tools
        "1234", // Parcel bundler default
        "4000", // Gatsby development server
        "6006", // Storybook default port
        "3333", // Nuxt.js alternate
        "8888"  // Jupyter, some dev servers
    };

    /// <summary>
    /// Framework-specific port mappings for targeted auto-configuration
    /// </summary>
    public static readonly Dictionary<string, string[]> FrameworkPorts = new()
    {
        ["React"] = new[] { "3000", "3001", "5173" },
        ["Vue"] = new[] { "8080", "5173", "8081" },
        ["Angular"] = new[] { "4200" },
        ["Next.js"] = new[] { "3000", "3001" },
        ["Nuxt"] = new[] { "3000", "3333" },
        ["Svelte"] = new[] { "5173", "8080" },
        ["Vite"] = new[] { "5173", "4173" },
        ["Parcel"] = new[] { "1234" },
        ["Webpack"] = new[] { "8080", "3000" },
        ["Gatsby"] = new[] { "8000", "4000" },
        ["Storybook"] = new[] { "6006" }
    };

    /// <summary>
    /// Common localhost patterns that should be allowed in development
    /// </summary>
    public static readonly string[] LocalhostPatterns = 
    {
        "localhost",
        "127.0.0.1",
        "0.0.0.0"
    };

    /// <summary>
    /// Cloud development environment domains that should be allowed
    /// </summary>
    public static readonly string[] CloudDevDomains = 
    {
        "*.vercel.app",
        "*.netlify.app", 
        "*.surge.sh",
        "*.herokuapp.com",
        "*.firebaseapp.com",
        "*.pages.dev", // Cloudflare Pages
        "*.azurestaticapps.net",
        "*.github.io",
        "*.gitpod.io",
        "*.codespaces.new",
        "*.replit.dev"
    };

    /// <summary>
    /// Generates all possible localhost origins for common development ports
    /// </summary>
    /// <param name="includePorts">Specific ports to include, null for all common ports</param>
    /// <returns>List of localhost origins</returns>
    public static List<string> GenerateLocalhostOrigins(string[]? includePorts = null)
    {
        var ports = includePorts ?? CommonDevPorts;
        var origins = new List<string>();

        foreach (var pattern in LocalhostPatterns)
        {
            foreach (var port in ports)
            {
                origins.Add($"http://{pattern}:{port}");
                origins.Add($"https://{pattern}:{port}");
            }
        }

        return origins;
    }

    /// <summary>
    /// Detects running development servers by scanning active processes
    /// </summary>
    /// <returns>List of detected origins from running dev servers</returns>
    public static List<string> DetectRunningDevServers()
    {
        var detectedOrigins = new List<string>();

        try
        {
            var commonProcesses = new[] 
            { 
                "node", "npm", "yarn", "pnpm", "bun",
                "vite", "ng", "vue-cli-service", "nuxt",
                "webpack", "parcel", "gatsby", "next"
            };

            var processes = Process.GetProcesses()
                .Where(p => commonProcesses.Any(proc => 
                    p.ProcessName.Contains(proc, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var process in processes)
            {
                try
                {
                    // Look for common dev server patterns in command line
                    var commandLine = GetCommandLine(process);
                    var extractedPorts = ExtractPortsFromCommandLine(commandLine);
                    
                    foreach (var port in extractedPorts)
                    {
                        detectedOrigins.Add($"http://localhost:{port}");
                        detectedOrigins.Add($"https://localhost:{port}");
                    }
                }
                catch
                {
                    // Ignore access denied or other exceptions
                }
            }
        }
        catch
        {
            // If process detection fails, fall back to common ports
            return GenerateLocalhostOrigins();
        }

        // If no processes detected, include common ports as fallback
        if (!detectedOrigins.Any())
        {
            detectedOrigins.AddRange(GenerateLocalhostOrigins());
        }

        return detectedOrigins.Distinct().ToList();
    }

    /// <summary>
    /// Gets command line arguments for a process (platform-specific implementation)
    /// </summary>
    private static string GetCommandLine(Process process)
    {
        try
        {
            // This is a simplified implementation
            // In a full implementation, you'd use platform-specific APIs
            return process.StartInfo.Arguments ?? "";
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Extracts port numbers from command line arguments
    /// </summary>
    private static List<string> ExtractPortsFromCommandLine(string commandLine)
    {
        var ports = new List<string>();
        
        // Common patterns for port specification
        var portPatterns = new[]
        {
            @"--port[=\s]+(\d+)",
            @"-p[=\s]+(\d+)", 
            @"--serve[=\s]+(\d+)",
            @"localhost:(\d+)",
            @"127\.0\.0\.1:(\d+)",
            @":(\d{4,5})\b" // Generic 4-5 digit port pattern
        };

        foreach (var pattern in portPatterns)
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(commandLine, pattern);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    ports.Add(match.Groups[1].Value);
                }
            }
        }

        return ports.Distinct().ToList();
    }

    /// <summary>
    /// Checks if the current environment is development
    /// </summary>
    public static bool IsDevelopmentEnvironment()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
                         ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        
        return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets framework-specific origins for a particular frontend framework
    /// </summary>
    public static List<string> GetFrameworkOrigins(string framework)
    {
        if (FrameworkPorts.TryGetValue(framework, out var ports))
        {
            return GenerateLocalhostOrigins(ports);
        }

        return new List<string>();
    }

    /// <summary>
    /// Generates a comprehensive list of development origins
    /// Combines detected servers, common ports, and framework-specific ports
    /// </summary>
    public static List<string> GetAllDevelopmentOrigins()
    {
        var allOrigins = new List<string>();

        // Add detected running servers
        allOrigins.AddRange(DetectRunningDevServers());

        // Add all common development origins as fallback
        allOrigins.AddRange(GenerateLocalhostOrigins());

        // Remove duplicates and return
        return allOrigins.Distinct().ToList();
    }
}