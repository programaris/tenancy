namespace Programaris.Tenancy.Abstractions
{
    /// <summary>Represents a selected tenant, not proof of authorization.</summary>
    /// <param name="Slug">Canonical tenant slug (lowercase ASCII).</param>
    /// <param name="TenantId">Optional stable identifier provided by a trusted resolver.</param>
    public sealed record TenantContext(string Slug, Guid? TenantId = null);
}