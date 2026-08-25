namespace BARANG.Core.Domain.Organization;

internal static class OrganizationText
{
    public static string? NormalizeCode(string? value, int maximumLength = 64)
    {
        string code = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (code.Length is < 2 || code.Length > maximumLength ||
            code.Any(static character =>
                character is not (>= 'A' and <= 'Z') and not (>= '0' and <= '9') and not '-' and not '_'))
        {
            return null;
        }

        return code;
    }

    public static string? NormalizeName(string? value, int maximumLength = 200)
    {
        string name = value?.Trim() ?? string.Empty;
        return name.Length is > 0 && name.Length <= maximumLength ? name : null;
    }
}
