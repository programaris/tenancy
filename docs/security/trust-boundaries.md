# Trust boundaries

## Non-negotiable rule

An inbound `X-Tenant` value is **untrusted input**. The SDK parses and carries it; it does not authenticate the caller, authorize the tenant, or grant data access.

## Responsibilities of the receiving service

1. Authenticate the caller when the endpoint requires it.
2. Resolve/validate tenant existence and operational status against trusted sources.
3. Check that the caller is permitted to act for the requested tenant.
4. Apply tenant-aware data authorization and isolation in every access path.
5. Reject conflicting or untrusted claims/headers rather than silently picking one.
6. Remove externally supplied privileged/internal context headers at trust boundaries.
7. Never treat a platform-owned tenant (e.g. `programaris`) as a supertenant.

## HTTP propagation

The helper does **not** automatically copy headers between requests. Call `AddProgramarisTenant` only when sending to a known, approved downstream endpoint. A receiving service still has to perform the checks above; network location and `X-Tenant` alone are never proof of authorization.

## Endpoint opt-out

Only endpoints that explicitly do not require tenant selection should use `AllowMissingTenant()`. It bypasses a header **presence** requirement, not authentication or authorization.

## Public / private boundary

This repository may contain transport and client contracts. Do not add internal connection strings, privileged tokens, membership bypass logic, private tenant registries, or internal authorization implementation.

## Reporting vulnerabilities

See the root [SECURITY.md](../../SECURITY.md). Never disclose exploit details in a public issue.
