dotnet restore Programaris.Tenancy.slnx

dotnet build Programaris.Tenancy.slnx `
    -c Release -warnaserror

$Projects = @(
    "src/Programaris.Tenancy.Abstractions/Programaris.Tenancy.Abstractions.csproj",
    "src/Programaris.Tenancy/Programaris.Tenancy.csproj",
    "src/Programaris.Tenancy.AspNetCore/Programaris.Tenancy.AspNetCore.csproj"
)

foreach ($Project in $Projects) {
    dotnet pack $Project `
        -c Release `
        --no-build `
        --no-restore `
        -o artifacts/nuget

    if ($LASTEXITCODE -ne 0) {
        throw "NuGet pack failed: $Project"
    }
}

python eng/verify_packages.py `
    --version 0.1.0-preview.1 `
    --directory artifacts/nuget