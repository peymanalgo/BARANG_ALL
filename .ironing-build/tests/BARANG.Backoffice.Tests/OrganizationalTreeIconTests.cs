using BARANG.Backoffice.Components;

namespace BARANG.Backoffice.Tests;

public sealed class OrganizationalTreeIconTests
{
    [Theory]
    [InlineData(OrganizationTreeEntityType.Company, null, "company")]
    [InlineData(OrganizationTreeEntityType.OrganizationalUnit, "1", "office")]
    [InlineData(OrganizationTreeEntityType.OrganizationalUnit, "4", "office")]
    [InlineData(OrganizationTreeEntityType.OrganizationalUnit, "3", "unit")]
    [InlineData(OrganizationTreeEntityType.Position, null, "position")]
    [InlineData(OrganizationTreeEntityType.Person, null, "person")]
    public void Resolver_uses_stable_entity_metadata(OrganizationTreeEntityType type, string? unitType, string expected)
        => Assert.Equal(expected, OrganizationTreeIconResolver.Icon(type, unitType));

    [Fact]
    public void Tree_exposes_position_actions_as_position_actions()
    {
        string tree = File.ReadAllText(Path.Combine(Root(), "01_CORE", "src", "BARANG.Backoffice", "Components", "OrganizationalChartTree.razor"));

        Assert.Contains("data-chart-node=\"position\"", tree);
        Assert.Contains("OrganizationChartNodeType.Position", tree);
        Assert.Contains("position-view", tree);
        Assert.Contains("position-edit", tree);
        Assert.DoesNotContain("Contains(\"مدیر\"", tree, StringComparison.Ordinal);
    }

    private static string Root()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !Directory.Exists(Path.Combine(current.FullName, "01_CORE"))) current = current.Parent;
        return current?.FullName ?? throw new DirectoryNotFoundException();
    }
}
