using Xunit;

namespace EasyAuth.Framework.Integration.Tests;

/// <summary>
/// Custom Fact attribute that skips the test if Docker is not available
/// </summary>
public sealed class DockerRequiredFactAttribute : FactAttribute
{
    public DockerRequiredFactAttribute()
    {
        if (!IsDockerAvailable())
        {
            Skip = "Docker is not running or not available. Please start Docker to run integration tests.";
        }
    }

    /// <summary>
    /// Check if Docker is available and running
    /// </summary>
    private static bool IsDockerAvailable()
    {
        try
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "info",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            process.Start();
            process.WaitForExit(5000); // 5 second timeout
            
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}