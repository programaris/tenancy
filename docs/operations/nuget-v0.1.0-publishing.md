# Publishing PROGRAMARIS Tenancy v0.1.0

The CI workflow validates and packs but never publishes automatically.
The stable release is deliberately triggered from `dev` using
[Publish NuGet v0.1.0](../../.github/workflows/publish.yml).
Only `v0.1.0` is permitted.

## One-time authorization

1. On [NuGet.org Trusted Publishing](https://www.nuget.org/account/TrustedPublishing), create a GitHub policy for your designated NuGet publisher/owner.
2. Set GitHub owner `programaris`, repository `tenancy`, workflow file `publish.yml`, environment `nuget-production`; grant permission to publish *new packages and new versions* for the three public NuGet IDs.
3. In the GitHub repository, create an environment called `nuget-production` and protect it with required reviewers. Configure deployment branches/tags appropriately for the manual `dev` dispatch.
4. No long-lived API key is stored in the repository or in Actions secrets.

## Candidate certification

1. Merge all source/release preparation changes into protected `dev`.
2. Tag its exact HEAD `v0.1.0-rc.1`. A tag push automatically runs `ci.yml`.
3. Confirm Windows and Ubuntu jobs succeed, including the SourceLink probe and both NuGet consumer smoke tests. Inspect `tenancy-nuget-0.1.0-rc.1` and its manifest.
4. Approve the public API, security review and immutable source revision.
5. Tag the **same** approved `dev` HEAD `v0.1.0` (never tag a different or unreviewed commit).

## Production promotion

1. Confirm the tag-triggered `ci.yml` run for `v0.1.0` is successful and its `tenancy-nuget-0.1.0` artifact exists.
2. From GitHub Actions, select `Publish NuGet v0.1.0` on branch `dev`; choose `v0.1.0`, enter your **NuGet account username** (not a password), and enter `PUBLISH`.
3. Approve the `nuget-production` deployment when prompted.
4. The workflow verifies that `dev` HEAD equals the stable tag, selects the successful tag-triggered CI run, downloads **those exact artifacts**, independently verifies package structure, complete SHA-256 manifest and SourceLink PDB checksums, and obtains a temporary NuGet.org credential over OIDC.
5. It publishes `Programaris.Tenancy.Abstractions`, `Programaris.Tenancy` and `Programaris.Tenancy.AspNetCore` (and matching symbols), checks public NuGet availability and creates a GitHub Release with hashes.

## Failure and recovery

NuGet package versions are immutable. If publication partially succeeds,
do **not** change/rebuild `v0.1.0` or blindly retry the whole workflow.
Inspect the exact published IDs and the failed step; use the already
certified artifacts to complete only missing packages with an explicitly
approved recovery plan. Never use `--skip-duplicate` to conceal a changed
or unverified package.

If the NuGet OIDC token exchange fails, verify that NuGet.org policy
owner, repository, workflow name, environment, and permitted package scope
match the GitHub workflow.
