using Microsoft.AspNetCore.Http;

namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>
    /// Host-supplied request-local selection source, activated only by explicit endpoint metadata.
    /// Values are untrusted until independently validated and authorized.
    /// </summary>
    public interface ITenantSelectionSource
    {
        /// <summary>
        /// Returns zero or one tenant slugs. Multiple values, including duplicates, are rejected
        /// by the SDK. The host must validate its protocol-specific source (e.g., OIDC state).
        /// </summary>
        Task<IReadOnlyList<string>> GetCandidatesAsync(HttpContext context, CancellationToken cancellationToken = default);
    }
}
