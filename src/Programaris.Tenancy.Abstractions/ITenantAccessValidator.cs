using System.Threading.Tasks;

namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Decides whether an already authenticated identity may access a tenant.</summary>
    public interface ITenantAccessValidator
    {
        /// <summary>Evaluates tenant membership/access. Must never infer access from X-Tenant alone.</summary>
        ValueTask<TenantAccessResult> ValidateAsync(TenantAccessRequest request, CancellationToken cancellationToken = default);
    }
}
