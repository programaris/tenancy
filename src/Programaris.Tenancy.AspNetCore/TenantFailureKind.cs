namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Internal trust-boundary outcome presented without exposing registry details.</summary>
    public enum TenantFailureKind
    {
        /// <summary>A tenant selection was required but not supplied.</summary>
        MissingTenant,
        /// <summary>Selected tenant was not found, inactive or otherwise not eligible.</summary>
        NotFoundOrInactive,
        /// <summary>The operation requires authentication.</summary>
        AuthenticationRequired,
        /// <summary>The authenticated principal was not granted access.</summary>
        AccessDenied,
        /// <summary>The trusted tenant registry is unavailable or inconsistent.</summary>
        RegistryUnavailable,
        /// <summary>The trusted access authority is unavailable.</summary>
        AuthorizationUnavailable
    }
}
