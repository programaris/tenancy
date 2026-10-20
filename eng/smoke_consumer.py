#!/usr/bin/env python3
"""Validate the NuGet SDK with isolated .NET 10 and .NET 8 consumers."""

import argparse
import os
import subprocess
import tempfile
import xml.etree.ElementTree as ET

from pathlib import Path


PACKAGES = (
    "Programaris.Tenancy.Abstractions",
    "Programaris.Tenancy",
    "Programaris.Tenancy.AspNetCore",
    "Programaris.Tenancy.Http",
)


ASP_NET_CORE_PROGRAM = r"""
using System;
using System.Linq;
using System.Net.Http;

using Programaris.Tenancy;
using Programaris.Tenancy.Abstractions;
using Programaris.Tenancy.AspNetCore;
using Programaris.Tenancy.Http;

namespace Programaris.Tenancy.Consumer
{
    internal static class Program
    {
        private static void Main()
        {
            var tenant = new TenantContext("programaris");

            if (!TenantSlug.TryParse(tenant.Slug, out string slug) ||
                slug != "programaris")
            {
                throw new InvalidOperationException("Tenant contract failed.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.invalid/");

            request.AddProgramarisTenant(tenant);

            if (request.Headers.GetValues("X-Tenant").Single() != "programaris")
            {
                throw new InvalidOperationException("HTTP tenant propagation failed.");
            }

            using var outgoing = new HttpRequestMessage(HttpMethod.Get, "https://identity.programaris.com.br/v1/users");
            outgoing.WithProgramarisTenant(tenant);

            if (!outgoing.Options.Any() || outgoing.Headers.Contains("X-Tenant"))
            {
                throw new InvalidOperationException("The outbound HTTP SDK did not capture request-local tenant selection.");
            }

            Console.WriteLine("ASP.NET Core and outbound HTTP consumer smoke tests passed.");
        }
    }
}
"""


PORTABLE_PROGRAM = r"""
using System;
using System.Linq;
using System.Net.Http;

using Programaris.Tenancy;
using Programaris.Tenancy.Abstractions;
using Programaris.Tenancy.Http;

namespace Programaris.Tenancy.PortableConsumer
{
    internal static class Program
    {
        private static void Main()
        {
            var tenant = new TenantContext("programaris");

            if (!TenantSlug.TryParse(tenant.Slug, out string slug) ||
                slug != "programaris")
            {
                throw new InvalidOperationException("Portable tenant context failed.");
            }

            if (TenantSlug.TryParse("INVALID", out _))
            {
                throw new InvalidOperationException("Invalid portable tenant was accepted.");
            }

            using var outgoing = new HttpRequestMessage(
                HttpMethod.Get,
                "https://identity.programaris.com.br/v1/users");
            outgoing.WithProgramarisTenant(tenant);

            if (!outgoing.Options.Any() || outgoing.Headers.Contains("X-Tenant"))
            {
                throw new InvalidOperationException("The .NET 8 HTTP SDK must capture selection before header injection.");
            }

            Console.WriteLine("Portable and HTTP package consumer smoke test passed.");
        }
    }
}
"""


CONSUMERS = {
    "net10.0": (("Programaris.Tenancy.AspNetCore", "Programaris.Tenancy.Http"), ASP_NET_CORE_PROGRAM),
    "net8.0": (("Programaris.Tenancy.Http",), PORTABLE_PROGRAM),
}


def run_command(command, environment):
    print(f"Executing: {' '.join(command)}", flush=True)

    subprocess.run(
        command,
        check=True,
        env=environment,
    )


def write_nuget_config(path, local_feed):
    root = ET.Element("configuration")
    sources = ET.SubElement(root, "packageSources")
    ET.SubElement(sources, "clear")
    ET.SubElement(sources, "add", {"key": "local", "value": str(local_feed)})
    ET.SubElement(sources, "add", {"key": "nuget.org", "value": "https://api.nuget.org/v3/index.json"})
    ET.ElementTree(root).write(path, encoding="utf-8", xml_declaration=True)


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

    parser.add_argument(
        "--framework",
        choices=tuple(CONSUMERS),
        default="net10.0",
        help="net8.0 exercises the portable contracts and outbound HTTP SDK; requires .NET 8.",
    )

    args = parser.parse_args()
    local_feed = Path(args.directory).resolve()

    validate_packages(local_feed, args.version)

    package_ids, program = CONSUMERS[args.framework]
    package_references = "\n".join(
        f'    <PackageReference Include="{package_id}" Version="{args.version}" />'
        for package_id in package_ids
    )

    with tempfile.TemporaryDirectory(prefix="tenancy-nuget-consumer-") as temp:
        work = Path(temp)
        environment = os.environ.copy()
        environment["NUGET_PACKAGES"] = str(work / "packages")

        project = work / "Consumer.csproj"

        project.write_text(
            f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>{args.framework}</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <NuGetAudit>false</NuGetAudit>
  </PropertyGroup>
  <ItemGroup>
{package_references}
  </ItemGroup>
</Project>
""",
            encoding="utf-8",
        )

        (work / "Program.cs").write_text(program, encoding="utf-8")

        config = work / "NuGet.Config"
        write_nuget_config(config, local_feed)

        run_command(
            ["dotnet", "restore", str(project), "--configfile", str(config)],
            environment,
        )

        run_command(
            ["dotnet", "run", "--no-restore", "--project", str(project)],
            environment,
        )

    print(f"NuGet {args.framework} consumer validation completed successfully.")


if __name__ == "__main__":
    main()
