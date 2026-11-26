namespace BARANG.Backoffice.Components;

public enum OrganizationTreeEntityType
{
    Company,
    OrganizationalUnit,
    Position,
    Person
}

public static class OrganizationTreeIconResolver
{
    public static string Icon(OrganizationTreeEntityType entityType, string? unitType = null) =>
        entityType == OrganizationTreeEntityType.OrganizationalUnit && IsOffice(unitType)
            ? "office"
            : entityType switch
            {
                OrganizationTreeEntityType.Company => "company",
                OrganizationTreeEntityType.OrganizationalUnit => "unit",
                OrganizationTreeEntityType.Position => "position",
                OrganizationTreeEntityType.Person => "person",
                _ => "unit"
            };

    public static string CaptionKey(OrganizationTreeEntityType entityType, string? unitType = null) =>
        entityType == OrganizationTreeEntityType.OrganizationalUnit && IsOffice(unitType)
            ? "Office"
            : entityType switch
            {
                OrganizationTreeEntityType.Company => "Company",
                OrganizationTreeEntityType.OrganizationalUnit => "OrganizationalUnit",
                OrganizationTreeEntityType.Position => "Position",
                OrganizationTreeEntityType.Person => "Person",
                _ => "OrganizationalUnit"
            };

    private static bool IsOffice(string? unitType) => unitType is "1" or "4" or "Branch" or "Office";
}
