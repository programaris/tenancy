namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Provides read-only access to the selected tenant in the current operation.</summary>
    public interface ITenantContextAccessor
    {
        /// <summary>The current selected tenant, or null if none was established.</summary>
        TenantContext? Current { get; }

        /// <summary>Returns the selected tenant, or throws if unavailable.</summary>
        TenantContext GetRequired();
    }
}