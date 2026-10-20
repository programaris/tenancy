using System.Threading;
using System.Threading.Tasks;

namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Reads tenant existence and operational state from a trusted source.</summary>
    public interface ITenantStore
    {
        /// <summary>Asynchronously looks up a selected tenant in a trusted store.</summary>
        Task<TenantValidationResult> FindAsync(
            string tenantSlug,
            CancellationToken cancellationToken = default);
    }
}
