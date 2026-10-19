namespace Programaris.Tenancy
{
    /// <summary>Validates canonical tenant slugs without inferring tenant authorization.</summary>
    public static class TenantSlug
    {
        /// <summary>
        /// Accepts 1–63 lowercase ASCII letters, digits and internal hyphens.
        /// Whitespace, upper-case letters and leading/trailing hyphens are rejected.
        /// </summary>
        public static bool TryParse(string? value, out string slug)
        {
            slug = string.Empty;

            if (value is null || value.Length == 0 || value.Length > 63)
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                bool alphanumeric = c is (>= 'a' and <= 'z') or (>= '0' and <= '9');

                if (alphanumeric)
                {
                    continue;
                }

                if (c != '-' || i == 0 || i == value.Length - 1)
                {
                    return false;
                }
            }

            slug = value;

            return true;
        }
    }
}