# Compatibility policy

| Package | TFM | Consumer notes |
| --- | --- | --- |
| Programaris.Tenancy.Abstractions | netstandard2.0 | Portable contracts for compatible .NET and Unity managed profiles |
| Programaris.Tenancy | netstandard2.0;net10.0 | Portable core, with .NET 10 target for first-party services |
| Programaris.Tenancy.AspNetCore | net10.0 | ASP.NET Core 10 application integration only |
| Programaris.Tenancy.Http | net8.0;net10.0 | Tenant-aware outbound HTTP with request-local capture and HTTPS origin policy |

- The .NET 10 SDK is required to **build this repository**; consumers choose the compatible assembly selected by NuGet.
- Runtime support follows Microsoft's lifecycle. Target-framework compatibility is not a guarantee of security support for obsolete runtimes.
- Unity may consume the portable contract assemblies, but **Unity Mono and IL2CPP have not been certified** by this repository's .NET test suite. Unity Editor/Player smoke tests must run before advertising official Unity compatibility.
- Do not reference `Programaris.Tenancy.AspNetCore` from Unity. It depends on the ASP.NET Core shared framework.
- Background jobs/workers remain outside the current request-scoped accessor contract; outbound HTTP tenant selection must be explicitly supplied for each request.
- All four packages use one synchronized semantic version in a release.
- The independent `.NET 8` NuGet consumer exercises the `netstandard2.0` contracts and native `Programaris.Tenancy.Http` `net8.0` binary. This is not a claim of Unity Mono/IL2CPP compatibility.
