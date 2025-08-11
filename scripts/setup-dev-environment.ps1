# PowerShell script to set up the development environment
# Run this script after cloning the repository

param(
    [switch]$InstallTools = $false,
    [switch]$SetupGitHooks = $false,
    [switch]$All = $false
)

Write-Host "EasyAuth Framework - Development Environment Setup" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green

if ($All) {
    $InstallTools = $true
    $SetupGitHooks = $true
}

# Check if .NET 8 SDK is installed
Write-Host "`nChecking .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ .NET 8 SDK not found. Please install .NET 8 SDK first." -ForegroundColor Red
    exit 1
}

# Restore NuGet packages
Write-Host "`nRestoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ NuGet packages restored successfully" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to restore NuGet packages" -ForegroundColor Red
    exit 1
}

# Build solution
Write-Host "`nBuilding solution..." -ForegroundColor Yellow
dotnet build --configuration Debug --no-restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Solution built successfully" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to build solution" -ForegroundColor Red
    exit 1
}

# Run tests to verify everything is working
Write-Host "`nRunning tests..." -ForegroundColor Yellow
dotnet test --configuration Debug --no-build --verbosity minimal
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ All tests passed" -ForegroundColor Green
} else {
    Write-Host "⚠️ Some tests failed - this might be expected during development" -ForegroundColor Yellow
}

# Install development tools
if ($InstallTools) {
    Write-Host "`nInstalling development tools..." -ForegroundColor Yellow
    
    # Install global .NET tools
    $tools = @(
        "dotnet-ef",
        "dotnet-outdated-tool",
        "dotnet-sonarscanner",
        "security-scan"
    )
    
    foreach ($tool in $tools) {
        Write-Host "Installing $tool..." -ForegroundColor Cyan
        dotnet tool install --global $tool 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ $tool installed/updated" -ForegroundColor Green
        } else {
            Write-Host "⚠️ $tool installation failed or already installed" -ForegroundColor Yellow
        }
    }
    
    # Check if Python is available for pre-commit
    try {
        $pythonVersion = python --version 2>$null
        Write-Host "✅ Python found: $pythonVersion" -ForegroundColor Green
        
        # Install pre-commit
        Write-Host "Installing pre-commit..." -ForegroundColor Cyan
        pip install pre-commit 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ pre-commit installed" -ForegroundColor Green
        }
    } catch {
        Write-Host "⚠️ Python not found. Pre-commit hooks will not be available." -ForegroundColor Yellow
    }
}

# Setup Git hooks
if ($SetupGitHooks) {
    Write-Host "`nSetting up Git hooks..." -ForegroundColor Yellow
    
    if (Get-Command pre-commit -ErrorAction SilentlyContinue) {
        pre-commit install
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Pre-commit hooks installed" -ForegroundColor Green
        } else {
            Write-Host "❌ Failed to install pre-commit hooks" -ForegroundColor Red
        }
    } else {
        Write-Host "⚠️ pre-commit not available. Install Python and pre-commit first." -ForegroundColor Yellow
    }
}

# Create necessary directories
Write-Host "`nCreating necessary directories..." -ForegroundColor Yellow
$directories = @("logs", "coverage", "artifacts", "temp")
foreach ($dir in $directories) {
    if (!(Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force > $null
        Write-Host "✅ Created directory: $dir" -ForegroundColor Green
    }
}

# Display next steps
Write-Host "`n=== Development Environment Ready! ===" -ForegroundColor Green
Write-Host "Next steps:" -ForegroundColor White
Write-Host "1. Configure your IDE (VS Code, Visual Studio, or JetBrains Rider)" -ForegroundColor Cyan
Write-Host "2. Review the README.md for project overview" -ForegroundColor Cyan
Write-Host "3. Check appsettings.Development.json for local configuration" -ForegroundColor Cyan
Write-Host "4. Run 'docker-compose up -d' for local SQL Server instance" -ForegroundColor Cyan
Write-Host "5. Start coding! All quality checks are configured." -ForegroundColor Cyan

Write-Host "`nUseful commands:" -ForegroundColor White
Write-Host "• dotnet build              - Build the solution" -ForegroundColor Gray
Write-Host "• dotnet test               - Run all tests" -ForegroundColor Gray
Write-Host "• dotnet test --logger trx  - Run tests with detailed output" -ForegroundColor Gray
Write-Host "• pre-commit run --all-files - Run all quality checks" -ForegroundColor Gray
Write-Host "• docker-compose up -d      - Start development services" -ForegroundColor Gray

Write-Host "`nDevelopment environment setup completed successfully! 🚀" -ForegroundColor Green