namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Describes the tenant-scoped operation to evaluate with a trusted access validator.</summary>
    public sealed class TenantOperationMetadata
    {
        /// <summary>Creates metadata for a tenant-scoped endpoint operation.</summary>
        public TenantOperationMetadata(string operation)
        {
            if (string.IsNullOrWhiteSpace(operation))
            {
                throw new ArgumentException("A tenant operation is required.", nameof(operation));
            }

            Operation = operation;
        }

        /// <summary>Gets the operation passed to the tenant access validator.</summary>
        public string Operation { get; }
    }
}
