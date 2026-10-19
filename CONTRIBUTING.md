# Contributing

Thanks for considering a contribution to PROGRAMARIS Tenancy.

## Before proposing changes

- Check for existing issues and discussions.
- Document public-contract changes and compatibility consequences.
- Never introduce dependencies on private PROGRAMARIS repositories.
- Never submit credentials, customer data, private endpoints, or exploit details in PRs.

## Development

Requirements: .NET 10 SDK. Run:

```bash
dotnet restore Programaris.Tenancy.slnx
dotnet build Programaris.Tenancy.slnx --no-restore -warnaserror
dotnet test Programaris.Tenancy.slnx --no-restore
```

New behavior should include xUnit v3 tests and documentation. Prefer small changes with a single clear purpose.

## Workflow

Open feature branches against `dev`. Maintainers promote tested versions to `main`. Do not merge your own changes unless granted maintainer rights. Use conventional commit messages (`feat:`, `fix:`, `docs:`, `test:`, `build:`, `ci:`, `chore:`).

By submitting a contribution you agree it can be distributed under the repository's Apache-2.0 license. You must have the right to contribute the code; do not copy third-party content without respecting its license.