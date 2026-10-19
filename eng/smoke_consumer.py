
#!/usr/bin/env python3
"""Validate the public NuGet packages using an isolated .NET consumer."""

import argparse
import os
import subprocess
import tempfile

from pathlib import Path


PACKAGES = (
    "Programaris.Tenancy.Abstractions",
    "Programaris.Tenancy",
    "Programaris.Tenancy.AspNetCore",
)


PROGRAM = r"""
using Programaris.Tenancy;
using Programaris.Tenancy.Abstractions;
using Programaris.Tenancy.AspNetCore;

var tenant = new TenantContext("programaris");

if (!TenantSlug.TryParse(tenant.Slug, out string slug) ||
    slug != "programaris")
{
    throw new InvalidOperationException(
        "Portable tenant contract failed.");
}

using var request = new HttpRequestMessage(
    HttpMethod.Get,
    "https://example.invalid/");

request.AddProgramarisTenant(tenant);

if (request.Headers.GetValues("X-Tenant").Single() != "programaris")
{
    throw new InvalidOperationException(
        "HTTP tenant propagation failed.");
}

Console.WriteLine("Package consumer smoke test passed.");
"""


def run_command(command, environment):
    print(f"Executing: {' '.join(command)}", flush=True)

    subprocess.run(
        command,
        check=True,
        env=environment,
    )


def validate_packages(directory, version):
    if not directory.is_dir():
        raise FileNotFoundError(
            f"NuGet directory not found: {directory}"
        )

    for package_id in PACKAGES:
        package = directory / f"{package_id}.{version}.nupkg"

        if not package.is_file():
            raise FileNotFoundError(
                f"Required NuGet package not found: {package}"
            )


def main():
    parser = argparse.ArgumentParser()

    parser.add_argument(
        "--directory",
        default="artifacts/nuget",
    )

    parser.add_argument(
        "--version",
        required=True,
    )

    args = parser.parse_args()

    local_feed = Path(args.directory).resolve()

    validate_packages(local_feed, args.version)

    with tempfile.TemporaryDirectory(
        prefix="tenancy-nuget-consumer-"
    ) as temp:
        work = Path(temp)

        environment = os.environ.copy()

        # Use an isolated package cache to ensure that dependencies
        # are resolved from the generated local NuGet packages.
        environment["NUGET_PACKAGES"] = str(work / "packages")

        project = work / "Consumer.csproj"

        project.write_text(
            f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <NuGetAudit>false</NuGetAudit>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Programaris.Tenancy.AspNetCore"
                      Version="{args.version}" />
  </ItemGroup>
</Project>
""",
            encoding="utf-8",
        )

        (work / "Program.cs").write_text(
            PROGRAM,
            encoding="utf-8",
        )

        run_command(
            [
                "dotnet",
                "restore",
                str(project),
                "--source",
                str(local_feed),
            ],
            environment,
        )

        run_command(
            [
                "dotnet",
                "run",
                "--no-restore",
                "--project",
                str(project),
            ],
            environment,
        )

    print("NuGet consumer validation completed successfully.")


if __name__ == "__main__":
    main()
