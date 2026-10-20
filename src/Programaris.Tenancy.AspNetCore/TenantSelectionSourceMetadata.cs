namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Enables exactly one host-provided selection source type on an endpoint.</summary>
    public sealed class TenantSelectionSourceMetadata
    {
        /// <summary>Creates metadata for a DI-registered selection source.</summary>
        public TenantSelectionSourceMetadata(Type sourceType)
        {
            ArgumentNullException.ThrowIfNull(sourceType);

            if (!typeof(ITenantSelectionSource).IsAssignableFrom(sourceType) || sourceType.IsAbstract || sourceType.IsInterface)
            {
                throw new ArgumentException("Selection source must be a concrete ITenantSelectionSource.", nameof(sourceType));
            }

            SourceType = sourceType;
        }

        /// <summary>The exact source implementation type resolved from request DI.</summary>
        public Type SourceType { get; }
    }
}
