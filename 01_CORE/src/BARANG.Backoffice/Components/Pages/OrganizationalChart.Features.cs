using System.Text.Json;
using BARANG.Backoffice.Api;
using BARANG.Backoffice.Components;

namespace BARANG.Backoffice.Components.Pages;

public partial class OrganizationalChart : IDisposable
{
    private string SearchText = "";
    private string AppliedSearch = "";
    private CancellationTokenSource? SearchDelay;
    private AssignmentRow? FocusedAssignment;
    private OrganizationChartActionType? FocusedAssignmentAction;
    private string FocusedValue = "";
    private string FocusedWorkflow => FocusedAssignmentAction == OrganizationChartActionType.ChangeManager ? "change-manager" : "move-unit";
    private string FocusedAssignmentTitle => O[FocusedAssignmentAction == OrganizationChartActionType.ChangeManager ? "ChangeManager" : "MoveUnitTitle"];
    private bool PrintDialogOpen;
    private bool PrintLoading;
    private string PrintCompanyValue = "";
    private string? PrintError;
    private IReadOnlyList<SearchableSelectOption> PrintCompanyOptions = [];

    private async Task SearchChart(Microsoft.AspNetCore.Components.ChangeEventArgs args)
    {
        SearchText = args.Value?.ToString() ?? "";
        SearchDelay?.Cancel();
        SearchDelay?.Dispose();
        SearchDelay = new CancellationTokenSource();
        try
        {
            if (!string.IsNullOrWhiteSpace(SearchText)) await Task.Delay(250, SearchDelay.Token);
            AppliedSearch = SearchText.Trim();
        }
        catch (OperationCanceledException) { }
    }

    private void OpenFocusedAssignment(AssignmentRow? row, OrganizationChartActionType action)
    {
        if (row is null || !row.Active || !CanManageUnits || FindUnit(row.UnitId)?.CompanyId != CurrentCompanyId) return;
        FocusedAssignment = row;
        FocusedAssignmentAction = action;
        FocusedValue = action == OrganizationChartActionType.ChangeManager ? row.ManagerId?.ToString() ?? "" : row.UnitId.ToString();
        ActiveDialog = OrganizationChartDialogKind.Assignment;
        DialogMode = FormMode.Edit;
        OperationMessage = null;
    }

    private IReadOnlyList<SearchableSelectOption> FocusedManagerOptions =>
        [new("", O["NoManager"]), .. _chart.Assignments
            .Where(row => row.Active && FindUnit(row.UnitId)?.CompanyId == CurrentCompanyId && !CreatesReportingCycle(row))
            .OrderBy(row => row.PersonName)
            .Select(row => new SearchableSelectOption(row.Id.ToString(), $"{row.PersonName} · {row.PositionTitle} · {GetUnitName(row.UnitId)}"))];

    private bool CreatesReportingCycle(AssignmentRow candidate)
    {
        if (FocusedAssignment is null) return true;
        HashSet<Guid> visitedPeople = [];
        Stack<Guid> pending = new();
        pending.Push(candidate.PersonId);
        while (pending.TryPop(out Guid personId))
        {
            if (personId == FocusedAssignment.PersonId) return true;
            if (!visitedPeople.Add(personId)) continue;
            foreach (AssignmentRow position in _chart.Assignments.Where(row => row.PersonId == personId))
                if (position.ManagerId is Guid managerId && FindAssignment(managerId) is AssignmentRow manager)
                    pending.Push(manager.PersonId);
        }
        return false;
    }

    private IReadOnlyList<SearchableSelectOption> FocusedUnitOptions => _chart.Units
        .Where(unit => unit.Active && unit.CompanyId == CurrentCompanyId &&
            !_chart.Assignments.Any(row => row.Active && row.Id != FocusedAssignment?.Id && row.PersonId == FocusedAssignment?.PersonId && row.UnitId == unit.Id))
        .OrderBy(unit => unit.Code)
        .Select(unit => new SearchableSelectOption(unit.Id.ToString(), $"{unit.DisplayName} ({unit.Code})"))
        .ToArray();

    private void SelectFocusedValue(string value) => FocusedValue = value;

    private async Task SaveFocusedAssignment()
    {
        if (Busy || FocusedAssignment is null || FocusedAssignmentAction is null) return;
        Busy = true;
        OperationMessage = null;
        try
        {
            var session = await CurrentSession();
            if (session is null || !CanManageUnits || FindUnit(FocusedAssignment.UnitId)?.CompanyId != session.Value.Company)
            {
                OperationMessage = O["InvalidScope"];
                return;
            }
            bool managerOnly = FocusedAssignmentAction == OrganizationChartActionType.ChangeManager;
            var options = managerOnly ? FocusedManagerOptions : FocusedUnitOptions;
            if (!options.Any(option => option.Value == FocusedValue))
            {
                OperationMessage = O["InvalidScope"];
                return;
            }
            AssignmentRow original = FocusedAssignment;
            AssignmentRow changed = managerOnly
                ? original with { ManagerId = Guid.TryParse(FocusedValue, out Guid manager) ? manager : null }
                : original with { UnitId = Guid.Parse(FocusedValue) };
            ApiResult<JsonElement> response = await Core.SendAsync(HttpMethod.Put,
                $"api/v1/organization/chart/assignments/{original.Id}", new
                {
                    personId = original.PersonId,
                    organizationalUnitId = changed.UnitId,
                    managerAssignmentId = changed.ManagerId,
                    positionCode = original.PositionCode,
                    positionTitle = original.PositionTitle,
                    isPrimary = original.Primary,
                    startDate = original.StartDate,
                    endDate = original.EndDate,
                    notes = original.Notes,
                    version = original.Version
                }, session.Value.Token, session.Value.Company, default);
            if (!response.IsSuccess)
            {
                OperationMessage = ProblemMessage(response);
                return;
            }
            ExpandUnitPath(changed.UnitId);
            if (changed.ManagerId is Guid managerId && FindAssignment(managerId) is AssignmentRow managerRow)
                ExpandUnitPath(managerRow.UnitId);
            PageActionSucceeded = true;
            PageMessage = O["SaveSuccess"];
            await LoadChart();
            Busy = false;
            CloseDialog();
        }
        finally { Busy = false; }
    }

    private void ExpandUnitPath(Guid unitId)
    {
        HashSet<Guid> visited = [];
        for (UnitRow? unit = FindUnit(unitId); unit is not null && visited.Add(unit.Id); unit = unit.ParentId is Guid parent ? FindUnit(parent) : null)
        {
            Expanded.Add(unit.Id);
            Expanded.Add(unit.CompanyId);
            if (unit.BranchId is Guid branch) Expanded.Add(branch);
        }
    }

    private async Task OpenPrintDialog()
    {
        PrintDialogOpen = true;
        PrintLoading = true;
        PrintError = null;
        PrintCompanyValue = "";
        PrintCompanyOptions = [];
        try
        {
            var session = await CurrentSession();
            if (session is null) { PrintError = O["PrintError"]; return; }
            var companies = await Core.GetMyCompaniesAsync(session.Value.Token, default);
            if (!companies.IsSuccess || companies.Value is null) { PrintError = O["PrintError"]; return; }
            PrintCompanyOptions = companies.Value.OrderBy(company => company.DisplayName)
                .Select(company => new SearchableSelectOption(company.CompanyId.ToString(), $"{company.DisplayName} ({company.Code})")).ToArray();
        }
        finally { PrintLoading = false; }
    }

    private void SelectPrintCompany(string value) => PrintCompanyValue = value;
    private void ClosePrintDialog() { if (!PrintLoading) PrintDialogOpen = false; }
    private void OpenPrintPreview()
    {
        if (PrintCompanyOptions.Any(option => option.Value == PrintCompanyValue) && Guid.TryParse(PrintCompanyValue, out Guid company))
            Navigator.NavigateTo($"/master/organization-chart/print/{company}");
    }

    public void Dispose()
    {
        SearchDelay?.Cancel();
        SearchDelay?.Dispose();
    }
}
