namespace BARANG.Backoffice.Tests;

public sealed class OrderWizardRoutingTests
{
    [Fact]
    public void WizardHasOneAuthoritativeBodyForEachExistingStep()
    {
        string root = FindRepositoryRoot();
        string wizard = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "OrderWizard.razor"));

        Assert.Contains("@if (OrderId is null || CurrentStep == 1){", wizard, StringComparison.Ordinal);
        Assert.DoesNotContain(@"<section class=""panel order-print-step""", wizard, StringComparison.Ordinal);
        Assert.Equal(1, Count(wizard, "<OrderPrintStep "));
        Assert.Equal(1, Count(wizard, "@if(OrderId is Guid && CurrentStep==2)"));
        Assert.Equal(1, Count(wizard, "@if(OrderId is Guid && CurrentStep==3)"));
        Assert.Equal(1, Count(wizard, "@if(OrderId is Guid && CurrentStep==4)"));
        Assert.Equal(1, Count(wizard, "@if(OrderId is Guid && CurrentStep==5)"));
        Assert.Equal(1, Count(wizard, "@if(OrderId is Guid && CurrentStep==6)"));
        Assert.Equal(1, Count(wizard, "@if(OrderId is Guid && CurrentStep==7)"));
    }

    [Fact]
    public void PrintRouteDoesNotCarryLegacyBodyOrStrayMarkup()
    {
        string root = FindRepositoryRoot();
        string wizard = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "OrderWizard.razor"));
        string print = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "OrderPrintStep.razor"));

        Assert.DoesNotContain("OrderStep1Description", print, StringComparison.Ordinal);
        Assert.DoesNotContain("CustomerCode", print, StringComparison.Ordinal);
        Assert.DoesNotContain("> {<", wizard + print, StringComparison.Ordinal);
        Assert.DoesNotContain("Choose File", wizard + print, StringComparison.Ordinal);
        Assert.DoesNotContain("No file chosen", wizard + print, StringComparison.Ordinal);
    }

    [Fact]
    public void PrintValidationUsesItsOwnContextualSummaryWithoutParentPopup()
    {
        string root = FindRepositoryRoot();
        string wizard = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "OrderWizard.razor"));
        string print = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "OrderPrintStep.razor"));

        Assert.Contains("ValidationErrors.Count > 0 && CurrentStep != 3", wizard, StringComparison.Ordinal);
        Assert.Contains("PrintOperationValidationTitle", print, StringComparison.Ordinal);
        Assert.DoesNotContain("اطلاعات واردشده را بررسی کنید", print, StringComparison.Ordinal);
    }

    [Fact]
    public void StepOneDoesNotRenderSaveDraftAction()
    {
        string root = FindRepositoryRoot();
        string wizard = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "OrderWizard.razor"));
        string stepOne = wizard[..wizard.IndexOf("@if(OrderId is Guid && CurrentStep==2)", StringComparison.Ordinal)];

        Assert.DoesNotContain("SaveDraft", stepOne, StringComparison.Ordinal);
        Assert.DoesNotContain("ذخیره پیش‌نویس", stepOne, StringComparison.Ordinal);
    }

    [Fact]
    public void ItemsCrudUsesStableIdentityConfirmationAndActiveReload()
    {
        string root = FindRepositoryRoot();
        string wizard = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Backoffice", "Components", "Pages", "OrderWizard.razor"));
        string administration = File.ReadAllText(Path.Combine(root, "01_CORE", "src", "BARANG.Core.Infrastructure", "Orders", "OrderAdministration.cs"));

        Assert.Contains("<BarangConfirmDialog", wizard, StringComparison.Ordinal);
        Assert.Contains("RequestDeleteItem(row)", wizard, StringComparison.Ordinal);
        Assert.Contains("Icon=\"trash\"", wizard, StringComparison.Ordinal);
        Assert.Contains("<tr @key=\"row.Id\">", wizard, StringComparison.Ordinal);
        Assert.Contains("await Core.SendAsync(HttpMethod.Post,$\"api/v1/orders/{o}/items/{row.Id}/remove\"", wizard, StringComparison.Ordinal);
        Assert.Contains("x.Id == itemId && x.OrderId == orderId", administration, StringComparison.Ordinal);
        Assert.Contains("x.OrderId == orderId && x.IsActive", administration, StringComparison.Ordinal);
    }

    private static int Count(string value, string token) => value.Split(token, StringSplitOptions.None).Length - 1;

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !Directory.Exists(Path.Combine(current.FullName, "01_CORE"))) current = current.Parent;
        return current?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
