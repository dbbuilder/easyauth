# SonarCloud Local Analysis Script
# This script runs SonarCloud analysis locally for development and testing

param(
    [Parameter(Mandatory=$true)]
    [string]$SonarToken,
    
    [Parameter(Mandatory=$false)]
    [string]$ProjectKey = "dbbuilder_easyauth",
    
    [Parameter(Mandatory=$false)]
    [string]$Organization = "dbbuilder",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipTests
)

Write-Host "🔍 Starting SonarCloud Local Analysis" -ForegroundColor Green
Write-Host "Project: $ProjectKey" -ForegroundColor Yellow
Write-Host "Organization: $Organization" -ForegroundColor Yellow

# Check if we're in the right directory
if (!(Test-Path "EasyAuth.Framework.sln")) {
    Write-Error "Please run this script from the repository root directory"
    exit 1
}

# Check if SonarScanner is installed
if (!(Get-Command "dotnet-sonarscanner" -ErrorAction SilentlyContinue)) {
    Write-Host "📦 Installing SonarScanner..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-sonarscanner
}

try {
    # Step 1: Clean previous builds
    Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Blue
    dotnet clean
    
    # Step 2: Start SonarCloud analysis
    Write-Host "🚀 Starting SonarCloud analysis..." -ForegroundColor Blue
    dotnet sonarscanner begin `
        /k:"$ProjectKey" `
        /o:"$Organization" `
        /d:sonar.host.url="https://sonarcloud.io" `
        /d:sonar.token="$SonarToken" `
        /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml" `
        /d:sonar.cs.vstest.reportsPaths="**/*.trx" `
        /d:sonar.javascript.lcov.reportPaths="packages/easyauth-js-sdk/coverage/lcov.info" `
        /d:sonar.typescript.lcov.reportPaths="packages/easyauth-js-sdk/coverage/lcov.info"
    
    if ($LASTEXITCODE -ne 0) {
        throw "SonarScanner begin failed"
    }
    
    # Step 3: Restore dependencies
    Write-Host "📦 Restoring .NET dependencies..." -ForegroundColor Blue
    dotnet restore
    
    # Step 4: Build solution
    Write-Host "🔨 Building .NET solution..." -ForegroundColor Blue
    dotnet build --no-restore --configuration Release
    
    if ($LASTEXITCODE -ne 0) {
        throw ".NET build failed"
    }
    
    # Step 5: Run tests if not skipped
    if (!$SkipTests) {
        Write-Host "🧪 Running .NET tests with coverage..." -ForegroundColor Blue
        dotnet test --no-build --configuration Release --logger trx --collect:"XPlat Code Coverage" --settings coverage.settings
        
        if ($LASTEXITCODE -ne 0) {
            Write-Warning ".NET tests failed, but continuing with analysis"
        }
        
        # Run JavaScript SDK tests
        if (Test-Path "packages/easyauth-js-sdk/package.json") {
            Write-Host "🧪 Running JavaScript SDK tests..." -ForegroundColor Blue
            Push-Location "packages/easyauth-js-sdk"
            
            npm ci
            if ($LASTEXITCODE -ne 0) {
                Write-Warning "npm ci failed"
            }
            
            npm run test:coverage
            if ($LASTEXITCODE -ne 0) {
                Write-Warning "JavaScript tests failed, but continuing with analysis"
            }
            
            Pop-Location
        }
    } else {
        Write-Host "⏭️ Skipping tests as requested" -ForegroundColor Yellow
    }
    
    # Step 6: End SonarCloud analysis
    Write-Host "📊 Finalizing SonarCloud analysis..." -ForegroundColor Blue
    dotnet sonarscanner end /d:sonar.token="$SonarToken"
    
    if ($LASTEXITCODE -ne 0) {
        throw "SonarScanner end failed"
    }
    
    Write-Host "✅ SonarCloud analysis completed successfully!" -ForegroundColor Green
    Write-Host "View results at: https://sonarcloud.io/project/overview?id=$ProjectKey" -ForegroundColor Cyan
    
} catch {
    Write-Error "❌ SonarCloud analysis failed: $_"
    
    # Try to end the analysis session to clean up
    try {
        dotnet sonarscanner end /d:sonar.token="$SonarToken"
    } catch {
        Write-Warning "Failed to clean up SonarScanner session"
    }
    
    exit 1
}

Write-Host "🎉 Analysis complete! Check SonarCloud dashboard for detailed results." -ForegroundColor Green