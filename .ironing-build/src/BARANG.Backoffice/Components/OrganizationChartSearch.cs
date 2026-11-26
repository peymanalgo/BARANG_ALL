namespace BARANG.Backoffice.Components;

public sealed class OrganizationChartSearch
{
    public HashSet<Guid> Matches { get; } = [];
    public HashSet<Guid> Visible { get; } = [];

    public static OrganizationChartSearch Build(OrganizationChartData data, string query)
    {
        OrganizationChartSearch result = new();
        if (string.IsNullOrWhiteSpace(query)) return result;
        bool Match(params string[] values) => values.Any(value => value.Contains(query.Trim(), StringComparison.CurrentCultureIgnoreCase));
        var units = data.Units.ToDictionary(row => row.Id);
        var branches = data.Branches.ToDictionary(row => row.Id);
        var assignments = data.Assignments.ToDictionary(row => row.Id);
        void AddUnit(Guid id)
        {
            HashSet<Guid> visited = [];
            while (visited.Add(id) && units.TryGetValue(id, out UnitRow? unit))
            {
                result.Visible.Add(unit.Id);
                result.Visible.Add(unit.CompanyId);
                if (unit.BranchId is Guid branchId) result.Visible.Add(branchId);
                if (unit.ParentId is not Guid parentId) break;
                id = parentId;
            }
        }
        foreach (CompanyRow row in data.Companies)
            if (Match(row.DisplayName, row.LegalName, row.Code)) { result.Matches.Add(row.Id); result.Visible.Add(row.Id); }
        foreach (BranchRow row in data.Branches)
            if (Match(row.DisplayName, row.Code)) { result.Matches.Add(row.Id); result.Visible.Add(row.Id); result.Visible.Add(row.CompanyId); }
        foreach (UnitRow row in data.Units)
            if (Match(row.DisplayName, row.Code)) { result.Matches.Add(row.Id); AddUnit(row.Id); }
        foreach (AssignmentRow row in data.Assignments)
        {
            if (!Match(row.PersonName, row.PositionTitle, row.PositionCode)) continue;
            result.Matches.Add(row.Id);
            HashSet<Guid> visited = [];
            for (AssignmentRow? current = row; current is not null && visited.Add(current.Id);
                 current = current.ManagerId is Guid managerId ? assignments.GetValueOrDefault(managerId) : null)
            {
                result.Visible.Add(current.Id);
                AddUnit(current.UnitId);
            }
        }
        return result;
    }
}
