namespace BARANG.Backoffice.Auth;

public static class AuthClaimTypes
{
    public const string AccessToken = "barang:access_token";
    public const string DisplayName = "barang:display_name";
    public const string CompanyId = "barang:company_id";
    public const string CompanyName = "barang:company_name";
    public const string CompanyCode = "barang:company_code";
}

public static class AuthPolicies
{
    public const string CompanySelected = "CompanySelected";
}
