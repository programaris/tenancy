param(
    [string] $Version = "0.1.0-preview.1"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-Checked
{
    param(
        [string] $Name,
        [scriptblock] $Operation
    )

    Write-Host "`n=== $Name ==="

    & $Operation

    if ($LASTEXITCODE -ne 0)
    {
        throw "$Name failed with exit code $LASTEXITCODE."
    }
}

$Root = Split-Path -Parent $PSScriptRoot

Push-Location $Root

try
{
    $Solution = "Programaris.Tenancy.slnx"
    $Output = "artifacts/nuget"

    $Projects = @(
        "src/Programaris.Tenancy.Abstractions/Programaris.Tenancy.Abstractions.csproj",
        "src/Programaris.Tenancy/Programaris.Tenancy.csproj",
        "src/Programaris.Tenancy.AspNetCore/Programaris.Tenancy.AspNetCore.csproj"
    )

    Invoke-Checked "Validate distribution" {
        python eng/validate_distribution.py
    }

    Invoke-Checked "Python tests" {
        python -m unittest discover -s eng -p "test_*.py"
    }

    Invoke-Checked "Restore" {
        dotnet restore $Solution
    }

    Invoke-Checked "Build Release" {
        dotnet build $Solution `
            --configuration Release `
            --no-restore `
            -warnaserror `
            "-p:Version=$Version"
    }

    Invoke-Checked ".NET tests" {
        dotnet test $Solution `
            --configuration Release `
            --no-build `
            --no-restore `
            "-p:Version=$Version"
    }

    New-Item -ItemType Directory -Force -Path $Output | Out-Null

    Get-ChildItem -Path $Output -File |
        Where-Object {
            $_.Extension -in @(".nupkg", ".snupkg")
        } |
        Remove-Item -Force

    foreach ($Project in $Projects)
    {
        Invoke-Checked "Pack $Project" {
            dotnet pack $Project `
                --configuration Release `
                --no-build `
                --no-restore `
                --output $Output `
                "-p:Version=$Version"
        }
    }

    Invoke-Checked "Verify NuGet packages" {
        python eng/verify_packages.py `
            --version $Version `
            --directory $Output
    }

    Invoke-Checked "Isolated NuGet consumer" {
        python eng/smoke_consumer.py `
            --version $Version `
            --directory $Output
    }

    Write-Host "`nLocal validation passed: $Version"
}
finally
{
    Pop-Location
}
