using Programaris.Tenancy.Abstractions;

namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>
    /// Accessor for the current TenantContext that requires initialization and prevents reinitialization.
    /// </summary>
    /// <remarks>Call Initialize to set the context once; it throws ArgumentNullException if context is null
    /// and InvalidOperationException if already initialized. GetRequired returns the current context or throws
    /// TenantContextUnavailableException when the context is not set. Acts as a simple ambient holder and is not
    /// intended for concurrent reinitialization.</remarks>
    public sealed class TenantContextAccessor : ITenantContextAccessor
    {
        /// <summary>
        /// Gets the current TenantContext for the ambient execution context, or null if none is available.
        /// </summary>
        /// <remarks>Set internally by tenant resolution; intended for external read-only access within
        /// the current scope.</remarks>
        public TenantContext? Current { get; private set; }

        /// <summary>
        /// Gets the current tenant context.
        /// </summary>
        /// <returns>The current TenantContext.</returns>
        /// <exception cref="TenantContextUnavailableException">Thrown when no TenantContext is available.</exception>
        public TenantContext GetRequired()
        {
            return Current ?? throw new TenantContextUnavailableException();
        }

        internal void Initialize(TenantContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (Current is not null)
            {
                throw new InvalidOperationException("The tenant context has already been initialized.");
            }

            Current = context;
        }
    }
}