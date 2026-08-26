namespace BARANG.Backoffice.Auth;
public enum CompanyFlow { NoAccess, AutoSelect, RequireSelection }
public static class CompanyFlowResolver
{
    public static CompanyFlow Resolve(int companyCount) => companyCount switch
    {
        <= 0 => CompanyFlow.NoAccess,
        1 => CompanyFlow.AutoSelect,
        _ => CompanyFlow.RequireSelection
    };
}
