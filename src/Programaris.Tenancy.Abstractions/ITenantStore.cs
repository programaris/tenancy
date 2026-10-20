namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Resolves tenant existence and operational status from a trusted source.</summary>
    public interface ITenantStore
    {
        /// <summary>Looks up the canonical tenant slug. Infrastructure failures must return Unavailable or throw.</summary>
        ValueTask<TenantValidationResult> FindAsync(string tenantSlug, CancellationToken cancellationToken = default);
    }
}
