using System.Text.Json;
using BARANG.Core.Application.Workflow;

namespace BARANG.Backoffice.Tests;

public sealed class CartableV2GridTests
{
    [Fact]
    public void CartableUiHasBoundedServerFiltersAndIndependentGridState()
    {
        string root = FindRepositoryRoot();
        string page = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Cartable.razor");

        Assert.Contains("stageKey=", page);
        Assert.Contains("customerId=", page);
        Assert.Contains("fromDate=", page);
        Assert.Contains("toDate=", page);
        Assert.Contains("outcome=", page);
        Assert.Contains("sort=", page);
        Assert.Contains("ReceivedState", page);
        Assert.Contains("ReferralState", page);
        Assert.Contains("ClearFilters", page);
        Assert.Contains("SearchCancel", page);
    }

    [Fact]
    public void CartableApiAndRuntimeExposeDeterministicServerSortingAndContextProjection()
    {
        string root = FindRepositoryRoot();
        string endpoint = Read(root, "01_CORE", "src", "BARANG.Core.Api", "Workflow", "WorkflowEndpoints.cs");
        string runtime = Read(root, "01_CORE", "src", "BARANG.Core.Infrastructure", "Workflow", "WorkflowDefinitionRuntime.cs");

        Assert.Contains("DateOnly?fromDate", endpoint);
        Assert.Contains("string?outcome", endpoint);
        Assert.Contains("string?sort", endpoint);
        Assert.Contains("order by {sort} {direction},t.id offset @skip limit @take", runtime);
        Assert.Contains("TaskPage(query", runtime);
        Assert.Contains("order_number", runtime);
        Assert.Contains("t.company_id=@company", runtime);
    }

    [Fact]
    public void CartableGridKeepsWorkflowActionsOutOfRows()
    {
        string root = FindRepositoryRoot();
        string page = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Cartable.razor");

        Assert.Contains("Icon=\"eye\"", page);
        Assert.DoesNotContain("CompleteTaskAsync", page);
        Assert.DoesNotContain("ClaimTaskAsync", page);
    }

    [Fact]
    public void CartablePersonalStateIsOccurrenceScopedAndServerFiltered()
    {
        string root = FindRepositoryRoot();
        string page = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Cartable.razor");
        string contract = Read(root, "01_CORE", "src", "BARANG.Core.Application", "Workflow", "WorkflowDesignerContracts.cs");
        string runtime = Read(root, "01_CORE", "src", "BARANG.Core.Infrastructure", "Workflow", "WorkflowDefinitionRuntime.cs");

        Assert.Contains("RuntimeTaskId", contract);
        Assert.Contains("on conflict (company_id,user_id,runtime_task_id)", runtime);
        Assert.Contains("StarredOnly", page);
        Assert.Contains("personal-state", page);
        Assert.Contains("runtime_task_id", runtime);
        Assert.Contains("seen = true", Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "CartableTaskDetail.razor"));
        Assert.Contains("star-filled", page);
    }

    [Fact]
    public void CartableAgingIsDerivedAndServerFilteredWithoutSlaArtifacts()
    {
        string root = FindRepositoryRoot();
        string page = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Cartable.razor");
        string contract = Read(root, "01_CORE", "src", "BARANG.Core.Application", "Workflow", "WorkflowDesignerContracts.cs");
        string runtime = Read(root, "01_CORE", "src", "BARANG.Core.Infrastructure", "Workflow", "WorkflowDefinitionRuntime.cs");

        Assert.Contains("TimeInStage", page);
        Assert.Contains("CartableAgingPolicy.Category", page);
        Assert.Contains("aging=", page);
        Assert.Contains("value=\"age:desc\"", page);
        Assert.Contains("CartableAgingPolicy.AttentionHours", runtime);
        Assert.Contains("CreatedAtUtc", contract);
        Assert.DoesNotContain("Sla", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Hangfire", page, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void QuickPreviewIsOnDemandReadOnlyAndDistinctFromFullView()
    {
        string root = FindRepositoryRoot();
        string page = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Cartable.razor");
        string endpoint = Read(root, "01_CORE", "src", "BARANG.Core.Api", "Workflow", "WorkflowEndpoints.cs");
        string runtime = Read(root, "01_CORE", "src", "BARANG.Core.Infrastructure", "Workflow", "WorkflowDefinitionRuntime.cs");

        Assert.Contains("quick-preview", page);
        Assert.Contains("OpenPreview", page);
        Assert.Contains("Icon=\"preview\"", page);
        Assert.Contains("Icon=\"eye\"", page);
        Assert.Contains("quick-preview", endpoint);
        Assert.Contains("QuickPreviewAsync", runtime);
        Assert.DoesNotContain("SetPersonalStateAsync", page);
        Assert.DoesNotContain("CompleteTaskAsync", page);
    }

    [Fact]
    public void CartableUsesLocalizedCaptionsAndOnlyHumanWorkflowStages()
    {
        string root = FindRepositoryRoot();
        string page = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Cartable.razor");
        string fa = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "CartableStrings.resx");
        string en = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "CartableStrings.en.resx");
        string ar = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "CartableStrings.ar.resx");

        Assert.DoesNotContain("BARANG / WORKFLOW", page, StringComparison.Ordinal);
        Assert.Contains("Where(IsHumanStage)", page);
        Assert.Contains("Text(node, \"type\").Equals(\"UserTask\"", page);
        Assert.Contains("name=\"OrderNumber\"", fa);
        Assert.Contains("name=\"CurrentStatus\"", fa);
        Assert.Contains("name=\"OrderNumber\"", en);
        Assert.Contains("name=\"CurrentStatus\"", en);
        Assert.Contains("name=\"OrderNumber\"", ar);
        Assert.Contains("name=\"CurrentStatus\"", ar);
    }

    [Fact]
    public void CartableFilterRowsKeepSearchSeparateAndUseResourceBackedStarredCaption()
    {
        string root = FindRepositoryRoot();
        string page = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Cartable.razor");
        string css = Read(root, "01_CORE", "src", "BARANG.Backoffice", "wwwroot", "cartable-v2.css");
        string fa = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "CartableGridStrings.resx");

        Assert.Contains("class=\"cartable-v2-search\"", page);
        Assert.Contains("class=\"cartable-filter-row\"", page);
        Assert.Contains("class=\"cartable-v2-actions\"", page);
        Assert.Contains("@G[\"StarredOnly\"]", page);
        Assert.Contains("name=\"StarredOnly\"", fa);
        Assert.Contains("grid-template-columns:repeat(3,minmax(0,1fr))", css);
        Assert.DoesNotContain("فقط نشانشدهها", page, StringComparison.Ordinal);
    }

    [Fact]
    public void CartableV3PriorityAndSavedViewsRemainServerScoped()
    {
        string root = FindRepositoryRoot();
        string page = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Cartable.razor");
        string query = Read(root, "01_CORE", "src", "BARANG.Core.Application", "Workflow", "WorkflowDesignerContracts.cs");
        string runtime = Read(root, "01_CORE", "src", "BARANG.Core.Infrastructure", "Workflow", "WorkflowDefinitionRuntime.cs");
        string saved = Read(root, "01_CORE", "src", "BARANG.Core.Infrastructure", "Workflow", "CartableSavedFilterAdministration.cs");
        Assert.Contains("priority=", page);
        Assert.Contains("string? Priority", query);
        Assert.Contains("priority", runtime, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("where company_id=@company and user_id=@user and tab=@tab", saved);
        Assert.Contains("Tab", saved);
        Assert.Contains("priority:desc", page);
        Assert.Contains("cartable-saved-views", page);
    }

    [Fact]
    public void SavedViewsUseTheCanonicalRootArrayContractForEmptySingleAndMultipleViews()
    {
        string root = FindRepositoryRoot();
        string page = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Cartable.razor");
        string endpoint = Read(root, "01_CORE", "src", "BARANG.Core.Api", "Workflow", "WorkflowEndpoints.cs");

        Assert.Contains("JsonValueKind.Array", page);
        Assert.Contains("JsonSerializer.Deserialize<List<CartableSavedFilterView>>", page);
        Assert.DoesNotContain("SavedViews = Items(response.Value)", page, StringComparison.Ordinal);
        Assert.Contains("tab={(Tab == \"received\" ? \"Received\" : \"MyReferrals\")}", page);
        Assert.Contains("Map(await s.ListAsync(tab,ct))", endpoint);

        Assert.Empty(JsonSerializer.Deserialize<List<CartableSavedFilterView>>("[]")!);
        const string one = "[{\"id\":\"11111111-1111-1111-1111-111111111111\",\"name\":\"Urgent\",\"tab\":\"Received\",\"definitionVersion\":1,\"definition\":{\"priority\":\"Urgent\",\"descending\":true,\"starredOnly\":false},\"updatedAtUtc\":\"2026-10-07T00:00:00Z\",\"version\":1}]";
        const string two = "[{\"id\":\"11111111-1111-1111-1111-111111111111\",\"name\":\"Urgent\",\"tab\":\"Received\",\"definitionVersion\":1,\"definition\":{},\"updatedAtUtc\":\"2026-10-07T00:00:00Z\",\"version\":1},{\"id\":\"22222222-2222-2222-2222-222222222222\",\"name\":\"Long waiting\",\"tab\":\"MyReferrals\",\"definitionVersion\":1,\"definition\":{\"aging\":\"LongWaiting\"},\"updatedAtUtc\":\"2026-10-07T00:00:00Z\",\"version\":1}]";
        Assert.Single(JsonSerializer.Deserialize<List<CartableSavedFilterView>>(one)!);
        Assert.Equal(2, JsonSerializer.Deserialize<List<CartableSavedFilterView>>(two)!.Count);
    }

    [Fact]
    public void SavedViewsUnexpectedContractIsSectionRecoverableAndScopeRemainsServerSide()
    {
        string root = FindRepositoryRoot();
        string page = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Cartable.razor");
        string saved = Read(root, "01_CORE", "src", "BARANG.Core.Infrastructure", "Workflow", "CartableSavedFilterAdministration.cs");

        Assert.Contains("response.Value.ValueKind != JsonValueKind.Array", page);
        Assert.Contains("SavedViewsMessage", page);
        Assert.Contains("catch (JsonException)", page);
        Assert.Contains("where company_id=@company and user_id=@user and tab=@tab", saved);
    }

    [Fact]
    public void CartableV3Step2KeepsFollowUpAndReleaseOccurrenceScoped()
    {
        string root = FindRepositoryRoot();
        string page = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Cartable.razor");
        string detail = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "CartableTaskDetail.razor");
        string runtime = Read(root, "01_CORE", "src", "BARANG.Core.Infrastructure", "Workflow", "WorkflowDefinitionRuntime.cs");
        string endpoint = Read(root, "01_CORE", "src", "BARANG.Core.Api", "Workflow", "WorkflowEndpoints.cs");

        Assert.Contains("followUp=", page);
        Assert.Contains("FollowUpAtUtc", page);
        Assert.Contains("followUpAtUtc", detail);
        Assert.Contains("/release", detail);
        Assert.Contains("update barang.workflow_runtime_tasks set status='Open'", runtime);
        Assert.Contains("claimed_by_user_id=null", runtime);
        Assert.Contains("tasks/{id:guid}/release", endpoint);
        Assert.Contains("CartablePersonalState", runtime);
    }

    [Fact]
    public void DecisionAttachmentsStayOnTheCompletedOccurrenceAndHistoryNode()
    {
        string root = FindRepositoryRoot();
        string detail = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "CartableTaskDetail.razor");
        string runtime = Read(root, "01_CORE", "src", "BARANG.Core.Infrastructure", "Workflow", "WorkflowDefinitionRuntime.cs");
        string endpoint = Read(root, "01_CORE", "src", "BARANG.Core.Api", "Workflow", "WorkflowEndpoints.cs");

        Assert.Contains("attachments = Attachments.Select", detail);
        Assert.Contains("workflow_decision_attachments", runtime);
        Assert.Contains("historyId", runtime);
        Assert.Contains("SaveAttachments", runtime);
        Assert.Contains("DecisionAttachmentsAsync", runtime);
        Assert.Contains("decision-attachments/{id:guid}/download", endpoint);
        Assert.Contains("NoAttachments", detail);
    }

    [Fact]
    public void SharedOrder360UsesLocaleDateEngineAndResponsiveFullDetailsTitle()
    {
        string root = FindRepositoryRoot();
        string page = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "OrderDetails.razor");
        string css = Read(root, "01_CORE", "src", "BARANG.Backoffice", "wwwroot", "order360-additions.css");
        string fa = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "Order360AdditionsStrings.resx");
        string en = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "Order360AdditionsStrings.en.resx");
        string ar = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "Order360AdditionsStrings.ar.resx");

        Assert.Contains("Dates.Format", page);
        Assert.DoesNotContain("Text(x, \"nodeKey\"", page);
        Assert.Contains("مشاهده کامل جزئیات سفارش", fa);
        Assert.Contains("View Full Order Details", en);
        Assert.Contains("عرض تفاصيل الطلب بالكامل", ar);
        Assert.Contains("overflow-x:hidden", css);
        Assert.Contains("grid-template-columns:1fr", css);
        Assert.Contains("max-height:100dvh", css);
    }

    [Fact]
    public void FinalRuntimeFixesKeepKeyboardLocalizationAndOrder360LayeringSemantic()
    {
        string root = FindRepositoryRoot();
        string page = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Cartable.razor");
        string keyboardFa = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "CartableKeyboardStrings.resx");
        string keyboardEn = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "CartableKeyboardStrings.en.resx");
        string keyboardAr = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "CartableKeyboardStrings.ar.resx");
        string timeline = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "OrderDetails.razor");
        string additions = Read(root, "01_CORE", "src", "BARANG.Backoffice", "wwwroot", "order360-additions.css");
        string cartableCss = Read(root, "01_CORE", "src", "BARANG.Backoffice", "wwwroot", "cartable-actions-v5.css");
        string compose = Read(root, "01_CORE", "compose.yaml");
        string runtime = Read(root, "01_CORE", "src", "BARANG.Core.Infrastructure", "Workflow", "WorkflowDefinitionRuntime.cs");

        Assert.Contains("IStringLocalizer<CartableKeyboardStrings>", page);
        Assert.DoesNotContain("@C[\"KeyboardShortcuts\"]", page);
        Assert.Contains("میانبرهای صفحه‌کلید", keyboardFa);
        Assert.Contains("Keyboard Shortcuts", keyboardEn);
        Assert.Contains("اختصارات لوحة المفاتيح", keyboardAr);
        Assert.Contains("<PriorityIndicator Priority=\"@row.Priority\" />", page);
        Assert.Contains("order-priority", Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "OrderWizard.razor"));
        string orderDetails = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "OrderDetails.razor");
        Assert.DoesNotContain("PriorityChanged", orderDetails);
        Assert.DoesNotContain("order360-priority-editor", orderDetails);
        Assert.Contains("PriorityIndicator", orderDetails);
        Assert.Contains("prefers-reduced-motion:reduce", cartableCss);
        Assert.Contains("order360-timeline-entry", timeline);
        Assert.Contains("display:grid", additions);
        Assert.Contains("z-index:var(--barang-z-modal)", additions);
        Assert.Contains("${BACKOFFICE_PORT:-5100}:5100", compose);
        Assert.DoesNotContain("ILike(node.TitleJson", runtime);
        Assert.DoesNotContain("ILike(task.ResultJson", runtime);
        Assert.Contains("result_json=@result::jsonb", runtime);
        Assert.Contains("JsonDocument.Parse", runtime);
    }

    [Fact]
    public void CartableSearchIsOneLocalizedAccessibleAccordionAndFullDetailsHasLocalizedSubtitle()
    {
        string root = FindRepositoryRoot();
        string page = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Cartable.razor");
        string accordion = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "CartableAccordionStrings.resx");
        string accordionEn = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "CartableAccordionStrings.en.resx");
        string accordionAr = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "CartableAccordionStrings.ar.resx");
        string entry = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "Order360EntryStrings.resx");
        string entryEn = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "Order360EntryStrings.en.resx");
        string entryAr = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "Order360EntryStrings.ar.resx");

        Assert.Contains("cartable-search-accordion", page);
        Assert.Contains("aria-expanded=\"@SearchPanelOpen\"", page);
        Assert.Contains("aria-controls=\"cartable-search-panel-content\"", page);
        Assert.Contains("ActiveFilterCount", page);
        Assert.Contains("SearchPanelOpen = false", page);
        Assert.Contains("پنل جستجو", accordion);
        Assert.Contains("Search Panel", accordionEn);
        Assert.Contains("لوحة البحث", accordionAr);
        Assert.Contains("برای مشاهده کلیک کنید", entry);
        Assert.Contains("Click to view", entryEn);
        Assert.Contains("انقر للعرض", entryAr);
    }

    [Fact]
    public void CartableSixRuntimeFixesKeepFullDetailsLocalizedPreviewSafeHistoryConsistentAndPriorityLtr()
    {
        string root = FindRepositoryRoot();
        string details = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "OrderDetails.razor");
        string cartable = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Cartable.razor");
        string priority = Read(root, "01_CORE", "src", "BARANG.Backoffice", "wwwroot", "cartable-actions-v5.css");
        string fieldsFa = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "Order360FieldsStrings.resx");
        string fieldsEn = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "Order360FieldsStrings.en.resx");
        string fieldsAr = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "Order360FieldsStrings.ar.resx");

        Assert.Contains("IStringLocalizer<Order360FieldsStrings>", details);
        Assert.DoesNotContain("L[\"CustomerCode\"]", details);
        Assert.DoesNotContain("L[\"CustomerType\"]", details);
        Assert.DoesNotContain("L[\"Size\"]", details);
        Assert.Contains("کد مشتری", fieldsFa);
        Assert.Contains("Customer code", fieldsEn);
        Assert.Contains("رمز العميل", fieldsAr);
        Assert.Contains("catch (JsonException)", cartable);
        Assert.Contains("finally", cartable);
        Assert.Contains("SelectedWorkflowHistory.Length", cartable);
        Assert.DoesNotContain("SelectedDetail.HistoryCount", cartable);
        Assert.Contains("border-left:2px solid currentColor", priority);
        Assert.Contains("barang-priority-beacon::before", priority);
        Assert.Contains("prefers-reduced-motion:reduce", priority);
        Assert.DoesNotContain("<span>}", cartable);
    }

    [Fact]
    public void RuntimeHotfixUsesOneFullDetailsLabelResolverAndStaleSafePreviewStateMachine()
    {
        string root = FindRepositoryRoot();
        string details = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "OrderDetails.razor");
        string json = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "JsonDataView.razor");
        string resolver = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "Order360FieldLabelResolver.cs");
        string cartable = Read(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "Cartable.razor");

        Assert.Contains("Order360FieldLabelResolver", json);
        Assert.Contains("ResourceNotFound", resolver);
        Assert.DoesNotContain("Regex.Replace(v", json);
        Assert.Contains("IStringLocalizer<Order360FieldsStrings>", details);
        Assert.Contains("requestCancellation.Token", cartable);
        Assert.Contains("PreviewGeneration", cartable);
        Assert.Contains("catch (ObjectDisposedException", cartable);
        Assert.Contains("catch (Exception exception)", cartable);
        Assert.Contains("IsCurrentPreview", cartable);
    }

    private static string Read(string root, params string[] parts) => File.ReadAllText(Path.Combine([root, .. parts]));

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !Directory.Exists(Path.Combine(current.FullName, "01_CORE"))) current = current.Parent;
        return current?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
