namespace BARANG.Backoffice.Auth;
public enum CompanyFlow { NoAccess, Recovery, AutoSelect, RequireSelection }
public static class CompanyFlowResolver
{
    public static CompanyFlow Resolve(int companyCount,bool isSystemAdministrator=false) => companyCount switch
    {
        <= 0 when isSystemAdministrator=>CompanyFlow.Recovery,
        <= 0 => CompanyFlow.NoAccess,
        1 => CompanyFlow.AutoSelect,
        _ => CompanyFlow.RequireSelection
    };
}
