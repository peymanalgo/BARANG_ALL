using System.Net.Http.Json;
using System.Text.Json;
using BARANG.TestSupport;
using Microsoft.Playwright;
using Xunit;

namespace BARANG.Backoffice.E2ETests;

public sealed class SupplierLifecycleE2ETests : IAsyncLifetime
{
    private static readonly Guid Company = Guid.Parse("42f0e459-6095-45bb-aa13-f38126d91d9c");
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";
    private readonly HttpClient client = new() { BaseAddress = new Uri(Environment.GetEnvironmentVariable("BARANG_E2E_API_URL") ?? "http://localhost:8080") };
    private IPlaywright playwright = null!; private IBrowser browser = null!; private IBrowserContext context = null!; private IPage page = null!;
    private string token = ""; private Guid supplier; private string criterionLabel = ""; private string contractTypeLabel = ""; private string documentTypeLabel = "";

    public async ValueTask InitializeAsync()
    {
        JsonElement login = await SupplierReferenceScenario.Send(client, HttpMethod.Post, "/api/v1/auth/login", null, null,
            new { username = Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME"), password = Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD") }, 200, TestContext.Current.CancellationToken);
        token = login.GetProperty("accessToken").GetString()!;
        await SupplierReferenceScenario.Send(client, HttpMethod.Post, "/api/v1/auth/me/company", token, Company, new { companyId = Company }, 200, TestContext.Current.CancellationToken);
        JsonElement suppliers = await SupplierReferenceScenario.Send(client, HttpMethod.Get, "/api/v1/suppliers/core?pageNumber=1&pageSize=1", token, Company, null, 200, TestContext.Current.CancellationToken);
        supplier = suppliers.GetProperty("items")[0].GetProperty("supplier").GetProperty("id").GetGuid();
        string stamp = DateTime.UtcNow.Ticks.ToString();
        JsonElement criterion = await SupplierReferenceScenario.Send(client, HttpMethod.Post, "/api/v1/lookups/types/supplier.evaluation.criterion/values", token, Company, new { code = "E2E_CRITERION_" + stamp, displayOrder = 1, translations = new Dictionary<string, string> { ["fa"] = "معیار آزمون", ["ar"] = "معيار الاختبار", ["en"] = "Delivery quality" } }, 201, TestContext.Current.CancellationToken);
        JsonElement contractType = await SupplierReferenceScenario.Send(client, HttpMethod.Post, "/api/v1/lookups/types/supplier.contract.type/values", token, Company, new { code = "E2E_CONTRACT_" + stamp, displayOrder = 1, translations = new Dictionary<string, string> { ["fa"] = "قرارداد آزمون", ["ar"] = "عقد الاختبار", ["en"] = "E2E contract" } }, 201, TestContext.Current.CancellationToken);
        JsonElement documentType = await SupplierReferenceScenario.Send(client, HttpMethod.Post, "/api/v1/lookups/types/supplier.document.type/values", token, Company, new { code = "E2E_DOCUMENT_" + stamp, displayOrder = 1, translations = new Dictionary<string, string> { ["fa"] = "سند آزمون", ["ar"] = "وثيقة الاختبار", ["en"] = "E2E document" } }, 201, TestContext.Current.CancellationToken);
        criterionLabel = "Delivery quality"; contractTypeLabel = "E2E contract"; documentTypeLabel = "E2E document";
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH") });
        context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 1000 } }); page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/culture?culture=en&returnUrl=%2Flogin");
        await page.Locator("#username").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME")!); await page.Locator("#password").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD")!);
        Task loginNavigation = page.WaitForURLAsync(url => new Uri(url).AbsolutePath != "/login", new() { WaitUntil = WaitUntilState.Commit }); await page.Locator("button[type=submit]").ClickAsync(); await loginNavigation;
        await page.Locator($".company-list form:has(input[name=companyId][value='{Company}']) button[type=submit]").ClickAsync();
        await page.WaitForURLAsync(url => new Uri(url).AbsolutePath != "/company", new() { WaitUntil = WaitUntilState.Commit });
        await page.GotoAsync(baseUrl + "/master/suppliers", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        ILocator grid = page.Locator("[data-supplier-directory]"); await Assertions.Expect(grid).ToBeVisibleAsync();
        await page.WaitForFunctionAsync("()=>{const el=document.querySelector('[data-supplier-directory] .barang-icon-button');return !!el&&Object.keys(el).some(k=>k.startsWith('_blazorEvents_')&&!!el[k].getHandler?.('click'));}");
        await grid.Locator("tbody tr").First.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("[data-supplier-editor]")).ToBeVisibleAsync();
        foreach (string tab in new[] { "evaluation", "quality", "quality-incidents", "contracts", "documents" })
        {
            ILocator button = page.Locator($"[data-supplier-editor] [data-supplier-tab='{tab}']"); await Assertions.Expect(button).ToBeVisibleAsync(); await button.ClickAsync(); await Assertions.Expect(button).ToHaveAttributeAsync("aria-selected", "true"); await Assertions.Expect(page.Locator($"[data-supplier-lifecycle='{tab}']")).ToBeVisibleAsync();
        }
    }

    public async ValueTask DisposeAsync() { if (context is not null) await context.DisposeAsync(); if (browser is not null) await browser.DisposeAsync(); playwright?.Dispose(); client.Dispose(); }
    [Fact] public void Lifecycle_workspaces_render_under_authorized_supplier_editor() => Assert.True(page is not null);

    [Fact]
    public async Task Lifecycle_crud_criteria_and_file_flows_are_real_browser_acceptance()
    {
        await OpenTab("evaluation");
        await page.GetByRole(AriaRole.Button, new() { Name = "Add evaluation", Exact = true }).ClickAsync();
        ILocator dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Add evaluation" });
        string evaluationNote = "browser evaluation " + DateTime.UtcNow.Ticks;
        await Assertions.Expect(dialog.Locator("input[type=number]").First).ToBeVisibleAsync();
        await dialog.Locator("input[type=number]").Last.FillAsync("80");
        await dialog.Locator("textarea").FillAsync(evaluationNote);
        await page.WaitForFunctionAsync("()=>[...document.querySelectorAll('section[role=dialog]')].some(d=>d.querySelector('h2')?.textContent?.trim()==='Add evaluation'&&Object.keys(d.querySelector('button.btn-primary')??{}).some(k=>k.startsWith('_blazorEvents_')&&!!d.querySelector('button.btn-primary')[k].getHandler?.('click')))");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
        await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(page.Locator("[data-supplier-lifecycle='evaluation']")).ToContainTextAsync("80");
        ILocator evaluationRow = page.Locator("[data-supplier-lifecycle='evaluation'] tbody tr").Filter(new() { HasText = evaluationNote }).First;
        await evaluationRow.GetByRole(AriaRole.Button, new() { Name = "View", Exact = true }).ClickAsync();
        dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Add evaluation" }); await Assertions.Expect(dialog.Locator("input[type=number]").Last).ToHaveValueAsync("80.00");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
        await evaluationRow.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
        dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Add evaluation" }); await dialog.Locator("input[type=number]").Last.FillAsync("90");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync(); await Assertions.Expect(dialog).ToBeHiddenAsync();
        await Assertions.Expect(evaluationRow).ToContainTextAsync("90");
        await page.ReloadAsync();
        ILocator reloadedGrid = page.Locator("[data-supplier-directory]"); await Assertions.Expect(reloadedGrid).ToBeVisibleAsync();
        await page.WaitForFunctionAsync("()=>{const el=document.querySelector('[data-supplier-directory] .barang-icon-button');return !!el&&Object.keys(el).some(k=>k.startsWith('_blazorEvents_')&&!!el[k].getHandler?.('click'));}");
        await reloadedGrid.Locator("tbody tr").First.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("[data-supplier-editor]")).ToBeVisibleAsync();
        await OpenTab("evaluation");
        evaluationRow = page.Locator("[data-supplier-lifecycle='evaluation'] tbody tr").Filter(new() { HasText = evaluationNote }).First;
        await Assertions.Expect(evaluationRow).ToContainTextAsync("90");

        await OpenTab("quality-incidents");
        await page.GetByRole(AriaRole.Button, new() { Name = "Add incident", Exact = true }).ClickAsync();
        string incidentType = "Reject-" + DateTime.UtcNow.Ticks;
        dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Add incident" }); await dialog.Locator("input").First.FillAsync(incidentType); await dialog.Locator("textarea").FillAsync("browser incident");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync(); await Assertions.Expect(dialog).ToBeHiddenAsync();
        ILocator incidentRow = page.Locator("[data-supplier-lifecycle='quality-incidents'] tbody tr").Filter(new() { HasText = incidentType }).First;
        await incidentRow.GetByRole(AriaRole.Button, new() { Name = "View", Exact = true }).ClickAsync(); dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Add incident" }); await Assertions.Expect(dialog.Locator("textarea")).ToHaveValueAsync("browser incident"); await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
        await incidentRow.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync(); dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Add incident" }); await dialog.Locator("textarea").FillAsync("edited browser incident"); await dialog.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync(); await Assertions.Expect(dialog).ToBeHiddenAsync();
        await page.ReloadAsync();
        reloadedGrid = page.Locator("[data-supplier-directory]"); await Assertions.Expect(reloadedGrid).ToBeVisibleAsync();
        await page.WaitForFunctionAsync("()=>{const el=document.querySelector('[data-supplier-directory] .barang-icon-button');return !!el&&Object.keys(el).some(k=>k.startsWith('_blazorEvents_')&&!!el[k].getHandler?.('click'));}");
        await reloadedGrid.Locator("tbody tr").First.GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
        await Assertions.Expect(page.Locator("[data-supplier-editor]")).ToBeVisibleAsync();
        await OpenTab("quality-incidents");
        incidentRow = page.Locator("[data-supplier-lifecycle='quality-incidents'] tbody tr").Filter(new() { HasText = incidentType }).First;
        await Assertions.Expect(incidentRow).ToContainTextAsync("Open");

        await OpenTab("contracts");
        await page.WaitForFunctionAsync("()=>[...document.querySelectorAll('button')].some(b=>b.textContent?.trim()==='Add contract'&&Object.keys(b).some(k=>k.startsWith('_blazorEvents_')&&!!b[k].getHandler?.('click')))");
        await page.GetByRole(AriaRole.Button, new() { Name = "Add contract", Exact = true }).ClickAsync(); dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Contract" });
        string contractNumber = "E2E-CONTRACT-" + DateTime.UtcNow.Ticks; byte[] contractPdf = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj\n<< /E2E " + contractNumber + " >>\nendobj\ntrailer\n<<>>\n%%EOF"); await Assertions.Expect(dialog).ToBeVisibleAsync(); await dialog.GetByRole(AriaRole.Textbox, new() { Name = "Contract Number", Exact = true }).FillAsync(contractNumber); await SelectOption(dialog, "Contract Type", contractTypeLabel); await dialog.Locator("input[type=file]").SetInputFilesAsync(new FilePayload { Name = "contract.pdf", MimeType = "application/pdf", Buffer = contractPdf }); await dialog.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync(); await Assertions.Expect(dialog).ToBeHiddenAsync();
        ILocator contractRow = page.Locator("[data-supplier-lifecycle='contracts'] tbody tr").Filter(new() { HasText = contractNumber }).First; await Assertions.Expect(contractRow).ToContainTextAsync("Draft"); await contractRow.GetByRole(AriaRole.Button, new() { Name = "Download", Exact = true }).ClickAsync();

        await OpenTab("documents"); await page.WaitForFunctionAsync("()=>[...document.querySelectorAll('button')].some(b=>b.textContent?.trim()==='Add document'&&Object.keys(b).some(k=>k.startsWith('_blazorEvents_')&&!!b[k].getHandler?.('click')))"); await page.GetByRole(AriaRole.Button, new() { Name = "Add document", Exact = true }).ClickAsync(); dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Document" }); string documentNumber = "E2E-DOCUMENT-" + DateTime.UtcNow.Ticks; byte[] documentPdf = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj\n<< /E2E " + documentNumber + " >>\nendobj\ntrailer\n<<>>\n%%EOF"); await SelectOption(dialog, "Document Type", documentTypeLabel); await dialog.GetByRole(AriaRole.Textbox, new() { Name = "Document Number", Exact = true }).FillAsync(documentNumber); await dialog.Locator("input[type=file]").SetInputFilesAsync(new FilePayload { Name = "certificate.pdf", MimeType = "application/pdf", Buffer = documentPdf }); await dialog.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync(); await Assertions.Expect(dialog).ToBeHiddenAsync();
        ILocator documentRow = page.Locator("[data-supplier-lifecycle='documents'] tbody tr").Filter(new() { HasText = documentNumber }).First; await documentRow.GetByRole(AriaRole.Button, new() { Name = "Download", Exact = true }).ClickAsync();
        JsonElement documents = await SupplierReferenceScenario.Send(client, HttpMethod.Get, $"/api/v1/suppliers/{supplier}/lifecycle/documents", token, Company, null, 200, TestContext.Current.CancellationToken);
        JsonElement document = documents.EnumerateArray().Select(x => x.GetProperty("document")).Single(x => x.GetProperty("documentNumber").GetString() == documentNumber);
        string fileId = document.GetProperty("fileId").GetGuid().ToString(); string fileVersionId = document.GetProperty("fileVersionId").GetGuid().ToString(); string downloadUrl = $"http://localhost:8080/api/v1/files/{fileId}/versions/{fileVersionId}/download";
        async Task<int> FileStatus(string company){using HttpRequestMessage request=new(HttpMethod.Get,downloadUrl);request.Headers.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",token);request.Headers.Add("X-Company-Id",company);using HttpResponseMessage response=await client.SendAsync(request,TestContext.Current.CancellationToken);return (int)response.StatusCode;}
        int authorizedStatus = await FileStatus(Company.ToString()); int unauthorizedStatus = await FileStatus("d82f44d5-8d22-4154-8eed-e7075ee6ce9f");
        Assert.Equal(200, authorizedStatus); Assert.Equal(404, unauthorizedStatus);
    }

    private async Task OpenTab(string tab)
    {
        ILocator button = page.Locator($"[data-supplier-editor] [data-supplier-tab='{tab}']"); await button.ClickAsync(); await Assertions.Expect(page.Locator($"[data-supplier-lifecycle='{tab}']")).ToBeVisibleAsync();
    }
    private static async Task SelectOption(ILocator dialog, string name, string label)
    {
        ILocator input = dialog.GetByRole(AriaRole.Combobox, new() { Name = name, Exact = true }); await input.ClickAsync(); await dialog.GetByRole(AriaRole.Option, new() { Name = label, Exact = true }).First.ClickAsync();
    }
}
