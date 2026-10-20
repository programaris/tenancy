namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Names the tenant operation to evaluate for a selected endpoint.</summary>
    public sealed class TenantOperationMetadata
    {
        /// <summary>Creates metadata for a tenant-scoped operation.</summary>
        public TenantOperationMetadata(string operation)
        {
            if (string.IsNullOrWhiteSpace(operation))
            {
                throw new ArgumentException("An operation is required.", nameof(operation));
            }

            Operation = operation;
        }

        /// <summary>Gets the operation used by the authorization provider.</summary>
        public string Operation { get; }
    }
}
