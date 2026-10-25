namespace BARANG.Backoffice.Tests;

public sealed class OrderPrintStepTests
{
    [Fact]
    public void PrintStepUsesStableItemIdentityCardsAndPerItemOperations()
    {
        string root = FindRepositoryRoot();
        string component = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "OrderPrintStep.razor"));
        string css = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "OrderPrintStep.razor.css"));
        Assert.Contains("ItemId", component, StringComparison.Ordinal);
        Assert.Contains("print-item-grid", component, StringComparison.Ordinal);
        Assert.Contains("print-item-card", component, StringComparison.Ordinal);
        Assert.Contains("print-item-shirt", component, StringComparison.Ordinal);
        Assert.Contains("print-item-info", component, StringComparison.Ordinal);
        Assert.Contains("print-item-status", component, StringComparison.Ordinal);
        Assert.Contains("print-item-check", component, StringComparison.Ordinal);
        Assert.Contains("print-item-card-content", component, StringComparison.Ordinal);
        Assert.Contains("width=\"50\" height=\"60\"", component, StringComparison.Ordinal);
        Assert.DoesNotContain("linearGradient", component, StringComparison.Ordinal);
        Assert.DoesNotContain("shirtId", component, StringComparison.Ordinal);
        Assert.Contains("<svg viewBox=\"0 0 160 180\"", component, StringComparison.Ordinal);
        Assert.DoesNotContain("order-print-item-cards", component, StringComparison.Ordinal);
        Assert.DoesNotContain("<span class=\"print-shirt", component, StringComparison.Ordinal);
        Assert.Contains("aria-selected", component, StringComparison.Ordinal);
        Assert.Contains(@"@key=""item.Id""", component, StringComparison.Ordinal);
        Assert.Contains(@"@key=""operation.Id""", component, StringComparison.Ordinal);
        Assert.DoesNotContain("EditOperation(operation)", component, StringComparison.Ordinal);
        Assert.Contains("x.ItemId==itemId&&x.IsActive", component, StringComparison.Ordinal);
        Assert.Contains(@"role=""option""", component, StringComparison.Ordinal);
        Assert.Contains("print-item-product", component, StringComparison.Ordinal);
        Assert.Contains("print-item-sku", component, StringComparison.Ordinal);
        Assert.Contains("print-item-color", component, StringComparison.Ordinal);
        Assert.Contains("print-item-size", component, StringComparison.Ordinal);
        Assert.Contains("print-item-quantity", component, StringComparison.Ordinal);
        Assert.Contains("ColorValue(DisplayColor(item.Color),item.ColorHex)", component, StringComparison.Ordinal);
        Assert.Contains("api/v1/lookups/types/product.color/values?activeOnly=true", component, StringComparison.Ordinal);
        Assert.Contains("ColorCaptions", component, StringComparison.Ordinal);
        Assert.Contains("DisplaySku", component, StringComparison.Ordinal);
        Assert.Contains("SelectItemOnKeyDown", component, StringComparison.Ordinal);
        Assert.DoesNotContain(@"<button type=""button"" class=""print-item-card", component, StringComparison.Ordinal);
        Assert.Contains("ItemIsComplete(item.Id)", component, StringComparison.Ordinal);
        Assert.Contains("OperationIsComplete", component, StringComparison.Ordinal);
        Assert.Contains(".print-item-grid", css, StringComparison.Ordinal);
        Assert.Contains("repeat(3,minmax(0,1fr))", css, StringComparison.Ordinal);
        Assert.Contains("grid-auto-flow:row", css, StringComparison.Ordinal);
        Assert.Contains("grid-auto-columns:minmax(0,1fr)", css, StringComparison.Ordinal);
        Assert.Contains(".print-item-card{position:relative;display:grid", css, StringComparison.Ordinal);
        Assert.Contains("grid-template-rows:minmax(0,1fr) auto", css, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns:minmax(60px,72px) minmax(0,1fr)", css, StringComparison.Ordinal);
        Assert.Contains("overflow:hidden", css, StringComparison.Ordinal);
        Assert.Contains("flex:0 0 60px;width:60px;height:72px", css, StringComparison.Ordinal);
        Assert.Contains("max-width:60px;max-height:72px", css, StringComparison.Ordinal);
        Assert.Contains("width:50px;height:60px;max-width:50px;max-height:60px", css, StringComparison.Ordinal);
        Assert.Contains("@media(max-width:900px)", css, StringComparison.Ordinal);
        Assert.Contains("@media(max-width:680px)", css, StringComparison.Ordinal);
    }

    [Fact]
    public void PrintMethodUsesAuthoritativeLookupContractAndDraftBinding()
    {
        string root = FindRepositoryRoot();
        string component = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "OrderPrintStep.razor"));
        string selector = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "ReferenceSelect.razor"));

        Assert.Contains("name=\"print-method-step2\"", component, StringComparison.Ordinal);
        Assert.Contains("@bind=\"Draft.Method\"", component, StringComparison.Ordinal);
        Assert.Contains("LoadPrintMethods", component, StringComparison.Ordinal);
        Assert.Contains("api/v1/lookups/types/print.method/values?activeOnly=true", component, StringComparison.Ordinal);
        Assert.Contains("PrintMethodOption", component, StringComparison.Ordinal);
        Assert.Contains("api/v1/lookups/{TypeCode}/options", selector, StringComparison.Ordinal);
        Assert.Contains("api/v1/lookups/types/{TypeCode}/values?activeOnly={activeOnly}", selector, StringComparison.Ordinal);
        Assert.Contains("ParseOptions", selector, StringComparison.Ordinal);
        Assert.Contains("DisplayName", selector, StringComparison.Ordinal);
        Assert.Contains("Logger.LogWarning", selector, StringComparison.Ordinal);
        Assert.Contains("reference-empty", selector, StringComparison.Ordinal);
    }

    [Fact]
    public void PrintAddUsesTheLookupCodeAndExposesTheServerReasonInsteadOfGenericValidation()
    {
        string root = FindRepositoryRoot();
        string component = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "OrderPrintStep.razor"));
        Assert.Contains("printMethodCode = Null(Draft.Method)", component, StringComparison.Ordinal);
        Assert.Contains("for item {OrderItemId}; method={PrintMethod}", component, StringComparison.Ordinal);
        Assert.Contains("result.ProblemCode", component, StringComparison.Ordinal);
        Assert.Contains("result.ProblemDescription", component, StringComparison.Ordinal);
        Assert.Contains("result.StatusCode", component, StringComparison.Ordinal);
        Assert.Contains("SaveStage = \"http-request\"", component, StringComparison.Ordinal);
        Assert.Contains("SaveFailedWithReference", component, StringComparison.Ordinal);
        Assert.Contains("order.invalid_data", component, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationData", component, StringComparison.Ordinal);
        Assert.Contains("SaveFailedWithReason", component, StringComparison.Ordinal);
        Assert.DoesNotContain("ApiFailure.Validation => L[\"ValidationError\"].Value", component, StringComparison.Ordinal);
    }

    [Fact]
    public void PrintStepActivatesDesignBankAndUploadAndLocalizesAllLanguages()
    {
        string root = FindRepositoryRoot();
        string component = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "OrderPrintStep.razor"));
        string css = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "OrderPrintStep.razor.css"));
        string fa = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "OrderPrintStepStrings.resx"));
        string en = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "OrderPrintStepStrings.en.resx"));
        string ar = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "OrderPrintStepStrings.ar.resx"));
        Assert.Contains("@A[\"DesignBank\"]", component);
        Assert.Contains("@A[\"Upload\"]", component);
        Assert.Contains("DesignCategoryPicker".Replace("DesignCategoryPicker", "api/v1/artworks?search="), component, StringComparison.Ordinal);
        Assert.Contains("InputFile", component, StringComparison.Ordinal);
        Assert.Contains("ConfirmDesign", component, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@(TemporaryDesignId is null)\"", component, StringComparison.Ordinal);
        Assert.Contains("order-print-design-modal-footer", component, StringComparison.Ordinal);
        Assert.Contains("Drafts[id]=Draft", component, StringComparison.Ordinal);
        Assert.Contains("UploadFile", component, StringComparison.Ordinal);
        Assert.Contains("artworkVersionId = Draft.ArtworkVersionId", component, StringComparison.Ordinal);
        Assert.Contains("fileVersionId = Draft.FileVersionId", component, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"print-add-button\"", component, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"OnAddClicked\"", component, StringComparison.Ordinal);
        Assert.Contains("@onclick:preventDefault=\"true\"", component, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@Saving\"", component, StringComparison.Ordinal);
        Assert.Contains("private async Task SaveOperationCore()", component, StringComparison.Ordinal);
        Assert.Contains("private async Task OnAddClicked()", component, StringComparison.Ordinal);
        Assert.Contains("Saving = true", component, StringComparison.Ordinal);
        Assert.Contains("await InvokeAsync(StateHasChanged)", component, StringComparison.Ordinal);
        Assert.Contains("Logger.LogError", component, StringComparison.Ordinal);
        Assert.Contains("Print Add API request started", component, StringComparison.Ordinal);
        Assert.Contains("Print Add API response received", component, StringComparison.Ordinal);
        Assert.Contains("Print Add completed", component, StringComparison.Ordinal);
        Assert.Contains("position:relative;z-index:2;pointer-events:auto", css, StringComparison.Ordinal);
        Assert.Contains("order-print-validation-summary", component, StringComparison.Ordinal);
        Assert.Contains("@V[\"PrintOperationValidationTitle\"]", component, StringComparison.Ordinal);
        Assert.Contains("@if(Errors.Count>0)", component, StringComparison.Ordinal);
        Assert.Contains("else if (Draft.Width <= 0)", component, StringComparison.Ordinal);
        Assert.DoesNotContain("اطلاعات واردشده را بررسی کنید", component, StringComparison.Ordinal);
        Assert.Contains("BothArtworkSources", component, StringComparison.Ordinal);
        foreach (string key in new[] { "Intro", "ItemColor", "ItemSize", "ItemProduct", "ItemSku", "ConfiguredStatus", "IncompleteStatus", "Selected", "DesignBank", "UploadDesign", "PrintNote", "PrintNotes", "PrintNotePlaceholder", "Centimeters", "AllItemsRequired", "InvalidDimensions", "MissingWidth", "MissingHeight", "InvalidWidth", "InvalidHeight", "ColorWhite", "ColorRed", "IncompleteItem" })
        {
            Assert.Contains($"name=\"{key}\"", fa);
            Assert.Contains($"name=\"{key}\"", en);
            Assert.Contains($"name=\"{key}\"", ar);
        }
        Assert.Contains("برای تمامی اقلام، اطلاعات چاپ وارد کنید.", fa);
        foreach (string key in new[] { "DesignBankHelper", "UploadHelper" })
        {
            Assert.Contains($"name=\"{key}\"", File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "OrderPrintArtworkStrings.resx")));
            Assert.Contains($"name=\"{key}\"", File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "OrderPrintArtworkStrings.en.resx")));
            Assert.Contains($"name=\"{key}\"", File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "OrderPrintArtworkStrings.ar.resx")));
        }
    }

    [Fact]
    public void PrintStepUsesFinalArtworkLayoutAndStyledModalActions()
    {
        string root = FindRepositoryRoot();
        string component = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "OrderPrintStep.razor"));
        string css = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "OrderPrintStep.razor.css"));

        Assert.Contains("order-print-artwork-row", component, StringComparison.Ordinal);
        Assert.Contains("order-print-modal-backdrop", component, StringComparison.Ordinal);
        Assert.Contains("order-print-design-modal", component, StringComparison.Ordinal);
        Assert.Contains("order-print-purple-primary", component, StringComparison.Ordinal);
        Assert.Contains("order-print-purple-secondary", component, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns:repeat(2,minmax(0,1fr))", css, StringComparison.Ordinal);
        Assert.Contains("order-print-design-modal-header", css, StringComparison.Ordinal);
        Assert.Contains("order-print-design-modal-footer .order-print-purple-primary", css, StringComparison.Ordinal);
        Assert.Contains("grid-template-rows:auto auto minmax(0,1fr) auto", css, StringComparison.Ordinal);
        Assert.Contains("order-print-design-gallery>.empty-state", css, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion:reduce", css, StringComparison.Ordinal);
        Assert.Contains("@T[\"Back\"]", component, StringComparison.Ordinal);
        Assert.DoesNotContain("Choose File", component, StringComparison.Ordinal);
        Assert.DoesNotContain("No file chosen", component, StringComparison.Ordinal);
        Assert.DoesNotContain("> {<", component, StringComparison.Ordinal);
        Assert.DoesNotContain("</article>}}</div>", component, StringComparison.Ordinal);
        Assert.Contains("order-print-modal-backdrop", component, StringComparison.Ordinal);
        Assert.Contains("role=\"dialog\" aria-modal=\"true\"", component, StringComparison.Ordinal);
        Assert.Contains("@onclick:stopPropagation=\"true\"", component, StringComparison.Ordinal);
        Assert.DoesNotContain(@"style=""--shirt-color:", component, StringComparison.Ordinal);
    }

    [Fact]
    public void PrintStepArtworkContainerHasTwoDirectPanelsAndModalOwnsGallery()
    {
        string root = FindRepositoryRoot();
        string component = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "OrderPrintStep.razor"));
        int artworkStart = component.IndexOf("order-print-artwork-row", StringComparison.Ordinal);
        int firstPanel = component.IndexOf("<section class=\"order-print-design-panel\"", artworkStart, StringComparison.Ordinal);
        int secondPanel = component.IndexOf("<section class=\"order-print-design-panel\"", firstPanel + 1, StringComparison.Ordinal);
        int artworkEnd = component.IndexOf("</div>", secondPanel, StringComparison.Ordinal);
        int modalStart = component.IndexOf("class=\"order-print-design-modal\"", StringComparison.Ordinal);
        int gallery = component.IndexOf("order-print-design-gallery", modalStart, StringComparison.Ordinal);

        Assert.True(artworkStart >= 0 && firstPanel > artworkStart && secondPanel > firstPanel && artworkEnd > secondPanel);
        Assert.True(modalStart > artworkEnd);
        Assert.True(gallery > modalStart);
        Assert.Equal(2, component[artworkStart..artworkEnd].Split("class=\"order-print-design-panel\"", StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void PrintStepPreservesEditorByOrderItemAndValidatesBeforeNext()
    {
        string root = FindRepositoryRoot();
        string component = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "OrderPrintStep.razor"));
        Assert.Contains("Drafts[id]=Draft", component);
        Assert.Contains("LoadDraftFromPersisted", component);
        Assert.Contains("await NextRequested.InvokeAsync()", component);
        Assert.DoesNotContain("Items.Any(item => !Operations.Any(operation => operation.ItemId == item.Id))", component);
        Assert.Contains("printMethodCode", component);
        Assert.Contains("width = Draft.Width", component);
        Assert.Contains("height = Draft.Height", component);
        Assert.Contains("notes = Null(Draft.Note)", component);
        Assert.Contains("Draft.DesignId=null", component);
        Assert.Contains("Draft.FileVersionId=null", component);
        Assert.Contains("HasUnsavedEditorChanges", component);
        Assert.Contains("IncompleteItem", component);
        Assert.Contains("((x.ArtworkVersionId is not null)^(x.FileVersionId is not null))", component);
        Assert.Contains("Items.SelectMany(item => Operations.Where(x => x.IsActive && x.ItemId == item.Id))", component, StringComparison.Ordinal);
        Assert.Contains("PrintItem? operationItem = Items.FirstOrDefault", component, StringComparison.Ordinal);
        Assert.DoesNotContain("Operations.Where(x => x.ItemId == CurrentItem.Id && x.IsActive)", component, StringComparison.Ordinal);
    }

    [Fact]
    public void PrintOperationsUseOrderItemIdentityWithoutAnOperationsColumn()
    {
        string root = FindRepositoryRoot();
        string component = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "OrderPrintStep.razor"));

        Assert.Contains("x.ItemId == item.Id", component, StringComparison.Ordinal);
        Assert.DoesNotContain("DeleteOperation(operation)", component, StringComparison.Ordinal);
        Assert.DoesNotContain("<th>@L[\"Actions\"]</th>", component, StringComparison.Ordinal);
        Assert.DoesNotContain(@"aria-label=""@L[""Edit""]""", component, StringComparison.Ordinal);
        Assert.Contains("ItemDisplaySummary(incomplete[0])", component, StringComparison.Ordinal);
        Assert.Contains("operations.Length==1&&OperationIsComplete(operations[0])", component, StringComparison.Ordinal);
    }

    [Fact]
    public void BackendPrintWorkflowUsesExactOrderItemOwnershipAndOneOperationInvariant()
    {
        string root = FindRepositoryRoot();
        string administration = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Core.Infrastructure", "Orders", "OrderAdministration.cs"));

        Assert.Contains("x.Id == itemId && x.OrderId == orderId && x.IsActive", administration, StringComparison.Ordinal);
        Assert.Contains("p.OrderItemId == itemId && p.IsActive", administration, StringComparison.Ordinal);
        Assert.Contains("x.Sequence == c.Sequence && x.IsActive", administration, StringComparison.Ordinal);
        string domain = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Core.Domain", "Orders", "OrderModels.cs"));
        Assert.Contains("public sealed class OrderPrintOperation", domain, StringComparison.Ordinal);
        Assert.Contains("OrderPrintClassification.Positions", domain, StringComparison.Ordinal);
        Assert.Contains("type.Code == \"print.method\"", administration, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderPrintClassification.Methods", domain, StringComparison.Ordinal);
    }

    [Fact]
    public void PrintStepUsesLocalizedEditorLabelsAndImageUploadContract()
    {
        string root = FindRepositoryRoot();
        string component = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "OrderPrintStep.razor"));
        string infrastructure = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Core.Infrastructure", "Files", "ArtworkFileAdministration.cs"));
        string worker = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Core.Infrastructure", "Files", "FileProcessingWorker.cs"));
        Assert.Contains("@T[\"PrintNotes\"]", component, StringComparison.Ordinal);
        Assert.Contains("@T[\"PrintNotePlaceholder\"]", component, StringComparison.Ordinal);
        Assert.Contains("accept=\"image/*\"", component, StringComparison.Ordinal);
        Assert.Contains("image/png", infrastructure, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("image/jpeg", infrastructure, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("application/pdf", infrastructure, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("image/png", worker, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("image/jpeg", worker, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(">@A[\"Notes\"]<", component, StringComparison.Ordinal);
        Assert.DoesNotContain("@L[\"Centimeter\"]", component, StringComparison.Ordinal);
    }

    [Fact]
    public void PrintStepValidationResourcesCoverIncompleteAndUnsavedStateInAllLanguages()
    {
        string root = FindRepositoryRoot();
        foreach (string suffix in new[] { ".resx", ".en.resx", ".ar.resx" })
        {
            string resource = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "OrderPrintValidationStrings" + suffix));
            Assert.Contains("name=\"Incomplete\"", resource, StringComparison.Ordinal);
            Assert.Contains("name=\"MissingMethod\"", resource, StringComparison.Ordinal);
            Assert.Contains("name=\"SaveBeforeNext\"", resource, StringComparison.Ordinal);
            Assert.Contains("name=\"MissingArtwork\"", resource, StringComparison.Ordinal);
            Assert.Contains("name=\"PrintOperationValidationTitle\"", resource, StringComparison.Ordinal);
            Assert.Contains("name=\"SaveFailedWithReason\"", resource, StringComparison.Ordinal);
            Assert.Contains("name=\"InvalidOperationData\"", resource, StringComparison.Ordinal);
            Assert.Contains("name=\"InvalidPrintMethod\"", resource, StringComparison.Ordinal);
        }
        string fa = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Localization", "OrderPrintStepStrings.resx"));
        Assert.Contains("برای تمامی اقلام، اطلاعات چاپ وارد کنید.", fa, StringComparison.Ordinal);
    }

    [Fact]
    public void ReviewRendersPrintConfigurationPerStableOrderItem()
    {
        string root = FindRepositoryRoot();
        string review = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "OrderStep7Panel.razor"));
        Assert.Contains("PrintOperations(item)", review, StringComparison.Ordinal);
        Assert.Contains("SourceText(print)", review, StringComparison.Ordinal);
        Assert.Contains("ItemText(item)", review, StringComparison.Ordinal);
        Assert.Contains("printPositionCode", review, StringComparison.Ordinal);
        Assert.Contains("fileVersionId", review, StringComparison.Ordinal);
        Assert.Contains("artworkVersionId", review, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !Directory.Exists(Path.Combine(current.FullName, "01_CORE"))) current = current.Parent;
        return current?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
