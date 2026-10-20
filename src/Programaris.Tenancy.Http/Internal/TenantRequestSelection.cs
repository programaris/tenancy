namespace Programaris.Tenancy.Http.Internal
{
    internal sealed class TenantRequestSelection
    {
        internal TenantRequestSelection(string slug, bool fromContext)
        {
            Slug = slug;
            FromContext = fromContext;
        }

        internal string Slug { get; }

        internal bool FromContext { get; }
    }
}
