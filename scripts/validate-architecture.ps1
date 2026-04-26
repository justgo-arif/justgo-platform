#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates architecture boundaries via JustGo architecture tests.
    
.DESCRIPTION
    Runs `dotnet test tests/JustGo.ArchitectureTests` and blocks deployment
    if any module boundary violations are detected. Designed for PostToolUse hooks.

.EXAMPLE
    ./scripts/validate-architecture.ps1
#>

param()

$ErrorActionPreference = "Stop"

Write-Host "🔍 Running architecture boundary tests..." -ForegroundColor Cyan

try {
    $testResult = & dotnet test tests/JustGo.ArchitectureTests --no-build --verbosity minimal 2>&1
    $exitCode = $LASTEXITCODE
    
    if ($exitCode -ne 0) {
        Write-Host "❌ Architecture tests FAILED" -ForegroundColor Red
        Write-Host $testResult
        
        # Return blocking error to agent
        $output = @{
            hookSpecificOutput = @{
                hookEventName = "PostToolUse"
                decision = "block"
                reason = "Architecture boundary violations detected. Run 'dotnet test tests/JustGo.ArchitectureTests' to review."
            }
        }
        
        Write-Output ($output | ConvertTo-Json -Depth 10)
        exit 2  # Blocking error
    }
    else {
        Write-Host "✅ Architecture tests passed" -ForegroundColor Green
        exit 0  # Success
    }
}
catch {
    Write-Host "⚠️  Architecture validation error: $_" -ForegroundColor Yellow
    # Non-blocking warning
    exit 1
}
