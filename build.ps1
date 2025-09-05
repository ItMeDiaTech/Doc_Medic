# Doc_Medic Build Script for Velopack
param(
    [string]$Version = "1.0.0",
    [string]$Configuration = "Release",
    [switch]$SkipTests = $false,
    [switch]$CleanFirst = $false
)

$ErrorActionPreference = "Stop"

Write-Host "Building Doc_Medic v$Version" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan

# Paths
$SolutionPath = "src/App.sln"
$ProjectPath = "src/App.UI/App.UI.csproj"
$PublishPath = "src/App.UI/bin/Publish"
$ReleasesPath = "releases"

# Clean if requested
if ($CleanFirst) {
    Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
    if (Test-Path $PublishPath) { Remove-Item $PublishPath -Recurse -Force }
    if (Test-Path $ReleasesPath) { Remove-Item $ReleasesPath -Recurse -Force }
    dotnet clean $SolutionPath --configuration $Configuration
}

# Restore dependencies
Write-Host "Restoring dependencies..." -ForegroundColor Yellow
dotnet restore $SolutionPath

# Build solution
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build $SolutionPath --configuration $Configuration --no-restore

# Run tests (unless skipped)
if (-not $SkipTests) {
    Write-Host "Running tests..." -ForegroundColor Yellow
    dotnet test $SolutionPath --configuration $Configuration --no-build --verbosity normal
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Tests failed. Build aborted."
        exit 1
    }
}

# Publish application
Write-Host "Publishing application..." -ForegroundColor Yellow
dotnet publish $ProjectPath `
    --configuration $Configuration `
    --output $PublishPath `
    --runtime win-x64 `
    --self-contained false `
    -p:PublishSingleFile=false `
    -p:Version=$Version `
    -p:AssemblyVersion=$Version `
    -p:FileVersion=$Version

# Check if vpk tool is available
try {
    vpk --version | Out-Null
} catch {
    Write-Host "Installing Velopack CLI tool..." -ForegroundColor Yellow
    dotnet tool install -g vpk
}

# Create Velopack package
Write-Host "Creating Velopack package..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path $ReleasesPath | Out-Null

vpk pack --packId DocMedic --packVersion $Version --packDir $PublishPath --outputDir $ReleasesPath

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build completed successfully!" -ForegroundColor Green
    Write-Host "Package created in: $ReleasesPath" -ForegroundColor Cyan
    
    # List created files
    Write-Host "Created files:" -ForegroundColor Cyan
    Get-ChildItem -Path $ReleasesPath -Recurse | ForEach-Object {
        Write-Host "  $($_.FullName)" -ForegroundColor Gray
    }
} else {
    Write-Error "Velopack packaging failed."
    exit 1
}