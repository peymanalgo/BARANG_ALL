using Bunit;
using BARANG.Backoffice.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace BARANG.Backoffice.Tests;

public sealed class OrganizationalChartInteractionTests
{
    [Fact]
    public void Company_add_branch_emits_add_branch_action()
    {
        using BunitContext context = new();
        context.Services.AddLocalization();
        var companyId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var branchId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var unitId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var personId = Guid.Parse("50000000-0000-0000-0000-000000000001");

        OrganizationChartAction? action = null;

        CompanyRow company = new(
            companyId,
            "CP-01",
            "Legal One",
            "Company One",
            true,
            1L,
            "NID",
            "REG",
            "EC",
            "TAX",
            "+9891",
            "+9892",
            "a@b.com",
            "https://example.com",
            "IR",
            "Tehran",
            "Iran",
            "Address",
            "12345",
            "USD",
            "notes");

        BranchRow branch = new(
            branchId,
            companyId,
            "BR-01",
            "Branch One",
            true,
            1L,
            "+9890",
            "+9890",
            "branch@b.com",
            "Tehran",
            "Tehran",
            "Address",
            "",
            "");

        UnitRow unit = new(
            unitId,
            companyId,
            branchId,
            null,
            "UN-01",
            "Unit One",
            true,
            1L,
            "Description");

        AssignmentRow assignment = new(
            Guid.Parse("60000000-0000-0000-0000-000000000001"),
            personId,
            unitId,
            null,
            "Person Name",
            "MGR",
            "Manager",
            true,
            null,
            null,
            true,
            1L,
            "");

        IRenderedComponent<OrganizationalChartTree> chart = context.Render<OrganizationalChartTree>(ps => ps
            .Add(x => x.Companies, new[] { company })
            .Add(x => x.Branches, new[] { branch })
            .Add(x => x.Units, new[] { unit })
            .Add(x => x.Assignments, new[] { assignment })
            .Add(x => x.Expanded, new HashSet<Guid> { companyId, branchId, unitId })
            .Add(x => x.Editable, true)
            .Add(x => x.ActionRequested, EventCallback.Factory.Create<OrganizationChartAction>(this, emitted => action = emitted)));

        chart.Find("article[data-chart-node='company'][data-chart-node-id='" + companyId + "'] button[data-chart-action='branch-create']").Click();
        Assert.NotNull(action);
        Assert.Equal(OrganizationChartActionType.AddBranch, action!.ActionType);
        Assert.Equal(OrganizationChartNodeType.Company, action!.NodeType);
        Assert.Equal(companyId, action!.NodeId);

        action = null;
        chart.Find("article[data-chart-node='branch'][data-chart-node-id='" + branchId + "'] button[data-chart-action='unit-create']").Click();
        Assert.NotNull(action);
        Assert.Equal(OrganizationChartActionType.AddUnit, action!.ActionType);
        Assert.Equal(OrganizationChartNodeType.Branch, action!.NodeType);
        Assert.Equal(branchId, action!.NodeId);

        action = null;
        chart.Find("article[data-chart-node='unit'][data-chart-node-id='" + unitId + "'] button[data-chart-action='assignment-create']").Click();
        Assert.NotNull(action);
        Assert.Equal(OrganizationChartActionType.AddPerson, action!.ActionType);
        Assert.Equal(OrganizationChartNodeType.Unit, action!.NodeType);
        Assert.Equal(unitId, action!.NodeId);
    }

    [Fact]
    public void Expansion_uses_the_same_typed_chart_action_contract()
    {
        using BunitContext context = new();
        context.Services.AddLocalization();
        Guid companyId = Guid.NewGuid();
        OrganizationChartAction? action = null;
        CompanyRow company = new(companyId, "CP-02", "Legal Two", "Company Two", true, 1, "", "", "", "", "", "", "", "", "", "", "", "", "", "", "");
        BranchRow branch = new(Guid.NewGuid(), companyId, "BR-02", "Branch Two", true, 1, "", "", "", "", "", "", "", "");

        IRenderedComponent<OrganizationalChartTree> chart = context.Render<OrganizationalChartTree>(parameters => parameters
            .Add(x => x.Companies, new[] { company })
            .Add(x => x.Branches, new[] { branch })
            .Add(x => x.ActionRequested, EventCallback.Factory.Create<OrganizationChartAction>(this, emitted => action = emitted)));

        chart.Find("article[data-chart-node='company'] button.org-expander").Click();
        Assert.NotNull(action);
        Assert.Equal(OrganizationChartActionType.ToggleNode, action!.ActionType);
        Assert.Equal(OrganizationChartNodeType.Company, action.NodeType);
        Assert.Equal(companyId, action.NodeId);
    }

    [Fact]
    public void Assignment_actions_emit_expected_action_payloads()
    {
        using BunitContext context = new();
        context.Services.AddLocalization();
        var companyId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var branchId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var unitId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var managerId = Guid.Parse("60000000-0000-0000-0000-000000000001");
        var subAssignmentId = Guid.Parse("60000000-0000-0000-0000-000000000002");

        OrganizationChartAction? action = null;

        CompanyRow company = new(
            companyId,
            "CP-01",
            "Legal",
            "Company",
            true,
            1L,
            "NID",
            "REG",
            "EC",
            "TAX",
            "+9891",
            "+9892",
            "a@b.com",
            "https://example.com",
            "IR",
            "Tehran",
            "Iran",
            "Address",
            "12345",
            "USD",
            "notes");

        BranchRow branch = new(
            branchId,
            companyId,
            "BR-01",
            "Branch",
            true,
            1L,
            "+9890",
            "+9890",
            "branch@b.com",
            "Tehran",
            "Tehran",
            "Address",
            "",
            "");

        UnitRow unit = new(
            unitId,
            companyId,
            branchId,
            null,
            "UN-01",
            "Unit",
            true,
            1L,
            "Description");

        AssignmentRow manager = new(
            managerId,
            Guid.Parse("50000000-0000-0000-0000-000000000001"),
            unitId,
            null,
            "Manager Person",
            "MGR",
            "Manager",
            true,
            null,
            null,
            true,
            1L,
            "");

        AssignmentRow child = new(
            subAssignmentId,
            Guid.Parse("50000000-0000-0000-0000-000000000002"),
            unitId,
            managerId,
            "Worker",
            "WRK",
            "Worker",
            true,
            null,
            null,
            true,
            1L,
            "");

        IRenderedComponent<OrganizationalChartTree> chart = context.Render<OrganizationalChartTree>(ps => ps
            .Add(x => x.Companies, new[] { company })
            .Add(x => x.Branches, new[] { branch })
            .Add(x => x.Units, new[] { unit })
            .Add(x => x.Assignments, new[] { manager, child })
            .Add(x => x.Expanded, new HashSet<Guid> { companyId, branchId, unitId })
            .Add(x => x.Editable, true)
            .Add(x => x.ActionRequested, EventCallback.Factory.Create<OrganizationChartAction>(this, emitted => action = emitted)));

        chart.Find("article[data-chart-node='assignment'][data-chart-node-id='" + managerId + "'] button[data-chart-action='assignment-create-child']").Click();
        Assert.NotNull(action);
        Assert.Equal(OrganizationChartActionType.AddSubordinate, action!.ActionType);
        Assert.Equal(OrganizationChartNodeType.Assignment, action!.NodeType);
        Assert.Equal(managerId, action!.NodeId);

        action = null;
        chart.Find("article[data-chart-node='assignment'][data-chart-node-id='" + subAssignmentId + "'] button[data-chart-action='assignment-edit']").Click();
        Assert.NotNull(action);
        Assert.Equal(OrganizationChartActionType.EditAssignment, action!.ActionType);
        Assert.Equal(subAssignmentId, action!.NodeId);
        Assert.NotNull(chart.Find(".org-assignment-children article[data-chart-node='assignment'][data-chart-node-id='" + subAssignmentId + "']"));
    }
}
