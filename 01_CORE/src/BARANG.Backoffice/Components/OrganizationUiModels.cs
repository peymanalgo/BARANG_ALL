namespace BARANG.Backoffice.Components;

using System.Text.Json;
using BARANG.Backoffice.Localization;

public enum FormMode
{
    Create,
    View,
    Edit
}

public enum OrganizationChartDialogKind
{
    Company,
    Branch,
    Unit,
    Assignment,
    Position
}

public enum OrganizationChartNodeType
{
    Company,
    Branch,
    Unit,
    Assignment,
    Position
}

public enum OrganizationChartActionType
{
    ToggleNode,
    AddCompany,
    ViewCompany,
    EditCompany,
    ActivateCompany,
    DeactivateCompany,
    DeleteCompany,

    AddBranch,
    ViewBranch,
    EditBranch,
    ActivateBranch,
    DeactivateBranch,
    DeleteBranch,

    AddUnit,
    ViewUnit,
    EditUnit,
    ActivateUnit,
    DeactivateUnit,
    DeleteUnit,
    AddPosition,
    ViewPosition,
    EditPosition,
    ActivatePosition,
    DeactivatePosition,
    DeletePosition,

    ViewPerson,
    ViewAssignment,
    EditAssignment,
    AddSubordinate,
    AddPerson,
    ChangeManager,
    MoveUnit,
    ActivateAssignment,
    DeactivateAssignment,
    DeleteAssignment
}

public sealed record OrganizationChartAction(
    OrganizationChartActionType ActionType,
    OrganizationChartNodeType NodeType,
    Guid NodeId,
    Guid? ParentId = null);

public sealed class CompanyFormModel
{
    public Guid? Id { get; set; }
    public long Version { get; set; }
    public string Code { get; set; } = "";
    public string LegalName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string NationalId { get; set; } = "";
    public string RegistrationNumber { get; set; } = "";
    public string EconomicCode { get; set; } = "";
    public string TaxId { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Mobile { get; set; } = "";
    public string Email { get; set; } = "";
    public string Website { get; set; } = "";
    public string CountryCode { get; set; } = "";
    public string Province { get; set; } = "";
    public string City { get; set; } = "";
    public string Address { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string DefaultCurrencyCode { get; set; } = "";
    public string Notes { get; set; } = "";
}

public sealed class BranchFormModel
{
    public Guid? Id { get; set; }
    public Guid CompanyId { get; set; }
    public long Version { get; set; }
    public string Code { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Mobile { get; set; } = "";
    public string Email { get; set; } = "";
    public string Province { get; set; } = "";
    public string City { get; set; } = "";
    public string Address { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Notes { get; set; } = "";
}

public sealed class UnitFormModel
{
    public Guid? Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid? ParentId { get; set; }
    public long Version { get; set; }
    public string Code { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public int DisplayOrder { get; set; }
}

public sealed class AssignmentFormModel
{
    public Guid? Id { get; set; }
    public long Version { get; set; }
    public Guid PersonId { get; set; }
    public Guid UnitId { get; set; }
    public Guid? ManagerId { get; set; }
    public string PositionCode { get; set; } = "";
    public string PositionTitle { get; set; } = "";
    public bool Primary { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Notes { get; set; } = "";
    public string PersonDisplayName { get; set; } = "";
    public string PersonPersonnelCode { get; set; } = "";
    public string PersonMobile { get; set; } = "";
    public bool Active { get; set; } = true;
}

public sealed class PositionFormModel
{
    public Guid? Id { get; set; }
    public Guid UnitId { get; set; }
    public Guid? ReportsToPositionId { get; set; }
    public long Version { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string SemanticRole { get; set; } = "";
    public string Notes { get; set; } = "";
    public bool Active { get; set; } = true;
}

public sealed record CompanyRow(
    Guid Id,
    string Code,
    string LegalName,
    string DisplayName,
    bool Active,
    long Version,
    string NationalId,
    string RegistrationNumber,
    string EconomicCode,
    string TaxId,
    string Phone,
    string Mobile,
    string Email,
    string Website,
    string CountryCode,
    string Province,
    string City,
    string Address,
    string PostalCode,
    string DefaultCurrencyCode,
    string Notes);

public sealed record BranchRow(
    Guid Id,
    Guid CompanyId,
    string Code,
    string DisplayName,
    bool Active,
    long Version,
    string Phone,
    string Mobile,
    string Email,
    string Province,
    string City,
    string Address,
    string PostalCode,
    string Notes);

public sealed record UnitRow(
    Guid Id,
    Guid CompanyId,
    Guid? BranchId,
    Guid? ParentId,
    string Code,
    string DisplayName,
    bool Active,
    long Version,
    string Description,
    int DisplayOrder = 0,
    string UnitType = "Other");

public sealed record AssignmentRow(
    Guid Id,
    Guid PersonId,
    Guid UnitId,
    Guid? ManagerId,
    string PersonName,
    string PositionCode,
    string PositionTitle,
    bool Primary,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool Active,
    long Version,
    string Notes);

public sealed record PositionRow(Guid Id, Guid UnitId, Guid? ReportsToPositionId, string Code, string Title, bool Active, long Version, IReadOnlyList<PositionIncumbentRow> Incumbents);
public sealed record PositionIncumbentRow(Guid AssignmentId, Guid PersonId, string PersonName, DateOnly? StartDate, DateOnly? EndDate, bool Primary, string Status, long Version);

public sealed record PersonSearchRow(
    Guid Id,
    string PersonnelCode,
    string Name,
    string Phone);

public sealed record OrganizationChartData(
    IReadOnlyList<CompanyRow> Companies,
    IReadOnlyList<BranchRow> Branches,
    IReadOnlyList<UnitRow> Units,
    IReadOnlyList<AssignmentRow> Assignments,
    IReadOnlyList<PositionRow>? Positions = null)
{
    public static OrganizationChartData Empty { get; } = new([], [], [], []);
}

public static class OrganizationChartJson
{
    private static readonly ILocaleDateService Dates = new LocaleDateService();
    public static OrganizationChartData ParseChart(JsonElement root)
    {
        List<CompanyRow> companies = [];
        List<BranchRow> branches = [];
        List<UnitRow> units = [];
        List<AssignmentRow> assignments = [];
        List<PositionRow> positions = [];

        if (root.TryGetProperty("companies", out JsonElement companyElement))
        {
            foreach (JsonElement company in companyElement.EnumerateArray())
            {
                companies.Add(ParseCompany(company));
            }
        }

        if (root.TryGetProperty("branches", out JsonElement branchElement))
        {
            foreach (JsonElement branch in branchElement.EnumerateArray())
            {
                branches.Add(ParseBranch(branch));
            }
        }

        if (root.TryGetProperty("units", out JsonElement unitElement))
        {
            foreach (JsonElement unit in unitElement.EnumerateArray())
            {
                units.Add(ParseUnit(unit));
            }
        }

        if (root.TryGetProperty("assignments", out JsonElement assignmentElement))
        {
            foreach (JsonElement assignment in assignmentElement.EnumerateArray())
            {
                assignments.Add(ParseAssignment(assignment));
            }
        }

        if (root.TryGetProperty("positions", out JsonElement positionElement))
        {
            foreach (JsonElement position in positionElement.EnumerateArray()) positions.Add(ParsePosition(position));
        }

        return new OrganizationChartData([.. companies], [.. branches], [.. units], [.. assignments], [.. positions]);
    }

    public static PersonSearchRow ParsePersonSearchRow(JsonElement row)
    {
        return new PersonSearchRow(
            row.GetProperty("id").GetGuid(),
            Read(row, "personnelCode"),
            Read(row, "displayName"),
            Read(row, "phone"));
    }

    public static CompanyFormModel ToFormModel(this CompanyRow row) => new()
    {
        Id = row.Id,
        Version = row.Version,
        Code = row.Code,
        LegalName = row.LegalName,
        DisplayName = row.DisplayName,
        NationalId = row.NationalId,
        RegistrationNumber = row.RegistrationNumber,
        EconomicCode = row.EconomicCode,
        TaxId = row.TaxId,
        Phone = row.Phone,
        Mobile = row.Mobile,
        Email = row.Email,
        Website = row.Website,
        CountryCode = row.CountryCode,
        Province = row.Province,
        City = row.City,
        Address = row.Address,
        PostalCode = row.PostalCode,
        DefaultCurrencyCode = row.DefaultCurrencyCode,
        Notes = row.Notes
    };

    public static BranchFormModel ToFormModel(this BranchRow row) => new()
    {
        Id = row.Id,
        CompanyId = row.CompanyId,
        Version = row.Version,
        Code = row.Code,
        DisplayName = row.DisplayName,
        Phone = row.Phone,
        Mobile = row.Mobile,
        Email = row.Email,
        Province = row.Province,
        City = row.City,
        Address = row.Address,
        PostalCode = row.PostalCode,
        Notes = row.Notes
    };

    public static UnitFormModel ToFormModel(this UnitRow row) => new()
    {
        Id = row.Id,
        BranchId = row.BranchId ?? Guid.Empty,
        ParentId = row.ParentId,
        Version = row.Version,
        Code = row.Code,
        DisplayName = row.DisplayName,
        Description = row.Description,
        DisplayOrder = row.DisplayOrder
    };

    public static AssignmentFormModel ToFormModel(this AssignmentRow row) => new()
    {
        Id = row.Id,
        Version = row.Version,
        PersonId = row.PersonId,
        UnitId = row.UnitId,
        ManagerId = row.ManagerId,
        PositionCode = row.PositionCode,
        PositionTitle = row.PositionTitle,
        Primary = row.Primary,
        StartDate = row.StartDate,
        EndDate = row.EndDate,
        Notes = row.Notes,
        PersonDisplayName = row.PersonName,
        Active = row.Active
    };

    private static CompanyRow ParseCompany(JsonElement source) => new(
        source.GetProperty("id").GetGuid(),
        Read(source, "code"),
        Read(source, "legalName"),
        Read(source, "displayName"),
        source.GetProperty("isActive").GetBoolean(),
        ReadLong(source, "version"),
        Read(source, "nationalId"),
        Read(source, "registrationNumber"),
        Read(source, "economicCode"),
        Read(source, "taxId"),
        Read(source, "phone"),
        Read(source, "mobile"),
        Read(source, "email"),
        Read(source, "website"),
        Read(source, "countryCode"),
        Read(source, "province"),
        Read(source, "city"),
        Read(source, "address"),
        Read(source, "postalCode"),
        Read(source, "defaultCurrencyCode"),
        Read(source, "notes"));

    private static BranchRow ParseBranch(JsonElement source) => new(
        source.GetProperty("id").GetGuid(),
        source.GetProperty("companyId").GetGuid(),
        Read(source, "code"),
        Read(source, "displayName"),
        source.GetProperty("isActive").GetBoolean(),
        ReadLong(source, "version"),
        Read(source, "phone"),
        Read(source, "mobile"),
        Read(source, "email"),
        Read(source, "province"),
        Read(source, "city"),
        Read(source, "address"),
        Read(source, "postalCode"),
        Read(source, "notes"));

    private static UnitRow ParseUnit(JsonElement source) => new(
        source.GetProperty("id").GetGuid(),
        source.GetProperty("companyId").GetGuid(),
        source.TryGetProperty("branchId", out JsonElement branch) && branch.ValueKind != JsonValueKind.Null ? branch.GetGuid() : null,
        source.TryGetProperty("parentId", out JsonElement parent) && parent.ValueKind != JsonValueKind.Null ? parent.GetGuid() : null,
        Read(source, "code"),
        Read(source, "displayName"),
        source.GetProperty("isActive").GetBoolean(),
        ReadLong(source, "version"),
        Read(source, "description"),
        ReadInt(source, "displayOrder"),
        ReadScalar(source, "unitType"));

    private static AssignmentRow ParseAssignment(JsonElement source) => new(
        source.GetProperty("id").GetGuid(),
        source.GetProperty("personId").GetGuid(),
        source.GetProperty("organizationalUnitId").GetGuid(),
        source.TryGetProperty("managerAssignmentId", out JsonElement manager) && manager.ValueKind != JsonValueKind.Null ? manager.GetGuid() : null,
        Read(source, "personName"),
        Read(source, "positionCode"),
        Read(source, "positionTitle"),
        source.TryGetProperty("isPrimary", out JsonElement primary) && primary.ValueKind != JsonValueKind.Null && primary.GetBoolean(),
        ParseDate(source, "startDate"),
        ParseDate(source, "endDate"),
        source.GetProperty("isActive").GetBoolean(),
        ReadLong(source, "version"),
        Read(source, "notes"));

    private static PositionRow ParsePosition(JsonElement source)
    {
        List<PositionIncumbentRow> incumbents = [];
        if (source.TryGetProperty("incumbents", out JsonElement values))
            foreach (JsonElement value in values.EnumerateArray()) incumbents.Add(new PositionIncumbentRow(value.GetProperty("assignmentId").GetGuid(), value.GetProperty("personId").GetGuid(), Read(value, "personName"), ParseDate(value, "startDate"), ParseDate(value, "endDate"), value.GetProperty("isPrimary").GetBoolean(), ReadScalar(value, "status"), ReadLong(value, "version")));
        return new PositionRow(source.GetProperty("id").GetGuid(), source.GetProperty("organizationalUnitId").GetGuid(), source.TryGetProperty("reportsToPositionId", out JsonElement reports) && reports.ValueKind != JsonValueKind.Null ? reports.GetGuid() : null, Read(source, "code"), Read(source, "title"), source.GetProperty("isActive").GetBoolean(), ReadLong(source, "version"), incumbents);
    }

    private static string Read(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) && value.ValueKind != JsonValueKind.Null
            ? value.GetString() ?? ""
            : "";

    // Enum properties may be emitted as numeric values by the API serializer. Keep the
    // read model tolerant of both numeric and string enum representations so a valid
    // vacant/incumbent organization graph cannot tear down the Blazor circuit.
    private static string ReadScalar(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
        {
            return "";
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : value.ToString();
    }

    private static long ReadLong(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) && value.ValueKind != JsonValueKind.Null && value.TryGetInt64(out long raw)
            ? raw
            : 0L;

    private static int ReadInt(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) && value.ValueKind != JsonValueKind.Null && value.TryGetInt32(out int raw)
            ? Math.Max(0, raw)
            : 0;

    private static DateOnly? ParseDate(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return Dates.TryParseCanonical(value.GetString() ?? "", out DateOnly parsed) ? parsed : null;
    }
}
