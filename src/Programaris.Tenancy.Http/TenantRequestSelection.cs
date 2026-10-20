namespace Programaris.Tenancy.Http
{
    /// <summary>A request-scoped tenant selection, never proof of authorization.</summary>
    internal sealed class TenantRequestSelection
    {
        internal TenantRequestSelection(string slug, string headerName, bool fromContext)
        {
            Slug = slug;
            HeaderName = headerName;
            FromContext = fromContext;
        }

        internal string Slug { get; }

        internal string HeaderName { get; }

        internal bool FromContext { get; }
    }
}
