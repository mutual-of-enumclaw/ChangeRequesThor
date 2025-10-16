# Setup script for SolarWinds Change Creator
# This script helps configure the NuGet authentication token

Write-Host "SolarWinds Change Creator - Setup Script" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host

# Check if NUGET_AUTH_TOKEN is already set
$currentToken = $env:NUGET_AUTH_TOKEN
if ($currentToken) {
    Write-Host "? NUGET_AUTH_TOKEN is already set" -ForegroundColor Green
    Write-Host "  Token: $($currentToken.Substring(0, [Math]::Min(10, $currentToken.Length)))..." -ForegroundColor Gray
} else {
    Write-Host "? NUGET_AUTH_TOKEN is not set" -ForegroundColor Yellow
    Write-Host
    Write-Host "To set up the NuGet authentication token:" -ForegroundColor Cyan
    Write-Host "1. Go to Azure DevOps: https://dev.azure.com/mutualofenumclaw" -ForegroundColor White
    Write-Host "2. Click your profile picture ? Personal Access Tokens" -ForegroundColor White
    Write-Host "3. Create a new token with 'Packaging (read)' permissions" -ForegroundColor White
    Write-Host "4. Run: " -NoNewline -ForegroundColor White
    Write-Host "`$env:NUGET_AUTH_TOKEN = 'your-token-here'" -ForegroundColor Yellow
    Write-Host "5. Or set it permanently in your user environment variables" -ForegroundColor White
    Write-Host
    
    $token = Read-Host "Enter your NuGet auth token (or press Enter to skip)"
    if ($token) {
        $env:NUGET_AUTH_TOKEN = $token
        Write-Host "? Token set for current session" -ForegroundColor Green
        
        $setPermanent = Read-Host "Set permanently for your user account? (y/n)"
        if ($setPermanent -eq 'y' -or $setPermanent -eq 'Y') {
            try {
                [Environment]::SetEnvironmentVariable("NUGET_AUTH_TOKEN", $token, [EnvironmentVariableTarget]::User)
                Write-Host "? Token set permanently" -ForegroundColor Green
            } catch {
                Write-Host "? Could not set permanent token: $($_.Exception.Message)" -ForegroundColor Yellow
            }
        }
    }
}

Write-Host
Write-Host "Testing build..." -ForegroundColor Cyan
try {
    $buildResult = dotnet build --verbosity quiet 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Build successful" -ForegroundColor Green
    } else {
        Write-Host "? Build failed" -ForegroundColor Yellow
        Write-Host $buildResult -ForegroundColor Red
    }
} catch {
    Write-Host "? Could not test build: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host
Write-Host "Configuration Summary:" -ForegroundColor Cyan
Write-Host "- Project: SolarWinds Change Creator" -ForegroundColor White
Write-Host "- Purpose: Creates SolarWinds change tickets for production deployments" -ForegroundColor White
Write-Host "- Trigger: When DEPLOYMENT_ENVIRONMENT is set to PRD/PROD/PRODUCTION" -ForegroundColor White
Write-Host "- Output: Release ID and Change Ticket Number" -ForegroundColor White
Write-Host
Write-Host "Ready to use! ??" -ForegroundColor Green