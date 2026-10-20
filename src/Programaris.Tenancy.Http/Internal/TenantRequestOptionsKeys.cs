using System.Net.Http;

namespace Programaris.Tenancy.Http.Internal
{
    internal static class TenantRequestOptionsKeys
    {
        internal static readonly HttpRequestOptionsKey<TenantRequestSelection> Selection =
            new HttpRequestOptionsKey<TenantRequestSelection>("Programaris.Tenancy.Http.Selection");
    }
}
