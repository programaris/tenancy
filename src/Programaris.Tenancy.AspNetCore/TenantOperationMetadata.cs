namespace Programaris.Tenancy.AspNetCore
{
    /// <summary>Names the tenant operation to evaluate for a selected endpoint.</summary>
    public sealed class TenantOperationMetadata
    {
        public TenantOperationMetadata(string operation)
        {
            if (string.IsNullOrWhiteSpace(operation))
            {
                throw new ArgumentException("An operation is required.", nameof(operation));
            }

            Operation = operation;
        }

        public string Operation { get; }
    }
}
