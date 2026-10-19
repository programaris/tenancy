namespace Programaris.Tenancy.AspNetCore
{
    internal static class TenantHeaderName
    {
        internal static bool IsValid(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            foreach (char character in value)
            {
                bool isAlphaNumeric = character is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9');

                if (!isAlphaNumeric && character != '-')
                {
                    return false;
                }
            }

            return true;
        }
    }
}