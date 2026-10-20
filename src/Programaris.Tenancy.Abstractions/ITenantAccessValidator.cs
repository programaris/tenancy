using System.Threading;
using System.Threading.Tasks;

namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Authorizes an authenticated principal against a trusted membership or policy source.</summary>
    public interface ITenantAccessValidator
    {
        Task<TenantAccessResult> ValidateAsync(
            TenantAccessRequest request,
            CancellationToken cancellationToken = default);
    }
}
