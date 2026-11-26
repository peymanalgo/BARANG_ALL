using BARANG.Core.Domain.Organization;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class OrganizationDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("شرکت")]
    [InlineData("INVALID CODE")]
    public void CompanyRejectsInvalidTechnicalCode(string code) => Assert.True(Company.Create(Guid.NewGuid(), code, "Company", Now).IsFailure);

    [Fact]
    public void CompanyNormalizesCodeAndAdvancesVersionOnChange()
    {
        Company company = Company.Create(Guid.NewGuid(), " acme-01 ", "Acme", Now).Value;
        company.Deactivate(Now.AddMinutes(1));
        Assert.Equal("ACME-01", company.Code);
        Assert.False(company.IsActive);
        Assert.Equal(2, company.Version);
    }

    [Theory]
    [InlineData(CompanySettingValueType.Boolean, "not-a-boolean")]
    [InlineData(CompanySettingValueType.WholeNumber, "1.5")]
    [InlineData(CompanySettingValueType.DecimalNumber, "one")]
    public void CompanySettingValidatesCanonicalValue(CompanySettingValueType type, string value) => Assert.True(CompanySetting.Create(Guid.NewGuid(), Guid.NewGuid(), "SETTING_KEY", value, type, Now).IsFailure);

    [Fact]
    public void BranchRequiresCompanyAndPreservesImmutableCode()
    {
        Assert.True(Branch.Create(Guid.NewGuid(), Guid.Empty, "MAIN", "Main", Now).IsFailure);
        Branch branch = Branch.Create(Guid.NewGuid(), Guid.NewGuid(), " main ", "Main", Now).Value;
        branch.Update("Updated", Now.AddMinutes(1));
        Assert.Equal("MAIN", branch.Code);
        Assert.Equal(2, branch.Version);
    }

    [Fact]
    public void OrganizationalUnitRejectsItselfAsParent()
    {
        Guid id = Guid.NewGuid();
        Assert.True(OrganizationalUnit.Create(id, Guid.NewGuid(), null, id, "UNIT", "Unit", Now).IsFailure);
    }

    [Fact]
    public void WarehouseCreatesControlledMasterWithoutInventoryBehavior()
    {
        Warehouse warehouse = Warehouse.Create(Guid.NewGuid(), Guid.NewGuid(), null, "RAW", "Raw materials", WarehouseType.RawMaterials, Now).Value;
        Assert.Equal(WarehouseType.RawMaterials, warehouse.Type);
        Assert.True(warehouse.IsActive);
        Assert.Equal(1, warehouse.Version);
    }
}
