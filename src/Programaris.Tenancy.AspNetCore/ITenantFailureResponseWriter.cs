using Microsoft.AspNetCore.Http;

namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Host-replaceable presentation policy. Must never continue the request on a failed check.</summary>
    public interface ITenantFailureResponseWriter
    {
        /// <summary>Writes a response to a rejected tenant-dependent request.</summary>
        Task WriteAsync(HttpContext context, TenantFailureKind failure);
    }
}
