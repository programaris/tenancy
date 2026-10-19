# Security policy

## Supported releases

This project is in early development. No stable NuGet package has been published yet; security fixes are developed against the active development branch.

## Reporting a vulnerability

**Do not open a public issue containing vulnerability details.** Use the repository's private vulnerability reporting / Security Advisory flow, once enabled by maintainers: [Report a vulnerability](https://github.com/programaris/tenancy/security/advisories/new).

If private vulnerability reporting has not yet been enabled, request a private communication channel from the maintainers via their verified GitHub profile contact methods; do not share reproduction steps publicly.

Please include affected revision or package version, impact, steps to reproduce, and a minimal proof of concept if safe to share. We aim to acknowledge reports, assess severity, and coordinate disclosure with reporters; response times are not guaranteed during the preview period.

## Threat model

`X-Tenant` is untrusted input and is never evidence of membership, authorization or data isolation. The accepting application must independently authenticate and authorize the request before accessing protected tenant resources.