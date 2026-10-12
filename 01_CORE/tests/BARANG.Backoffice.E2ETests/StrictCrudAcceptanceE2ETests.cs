using Microsoft.Playwright;

namespace BARANG.Backoffice.E2ETests;

public sealed class StrictCrudAcceptanceE2ETests : IAsyncLifetime
{
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";
    private IPlaywright playwright = null!;
    private IBrowser browser = null!;
    private IBrowserContext context = null!;
    private IPage page = null!;

    public async ValueTask InitializeAsync()
    {
        string username = Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME") ?? throw new InvalidOperationException("BARANG_E2E_USERNAME is required.");
        string password = Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD") ?? throw new InvalidOperationException("BARANG_E2E_PASSWORD is required.");
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH") });
        context = await browser.NewContextAsync(new() { Locale = "en-US", ViewportSize = new() { Width = 1440, Height = 1000 } });
        page = await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Flogin", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#username").FillAsync(username);
        await page.Locator("#password").FillAsync(password);
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.DoesNotContain("/login", page.Url, StringComparison.OrdinalIgnoreCase);
        if (new Uri(page.Url).AbsolutePath == "/company")
        {
            await page.Locator(".company-list form:has(input[name=companyId][value='42f0e459-6095-45bb-aa13-f38126d91d9c']) button[type=submit]").ClickAsync();
            await page.WaitForURLAsync(url => new Uri(url).AbsolutePath != "/company");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (context is not null) await context.DisposeAsync();
        if (browser is not null) await browser.DisposeAsync();
        playwright?.Dispose();
    }

    [Fact]
    public async Task PersonsCompleteCrudStatusProtectedDeleteAndLocaleDateRoundTrip()
    {
        string marker = $"Person{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        await GotoCulture("en", "/master/persons");
        await page.Locator("header.crud-header button.btn-primary").ClickAsync();
        ILocator modal = page.Locator(".barang-modal.modal-large");
        await Assertions.Expect(modal).ToBeVisibleAsync();
        await modal.Locator("[data-testid='person-first-name']").FillAsync("E2E");
        await modal.Locator("[data-testid='person-last-name']").FillAsync(marker);
        await modal.Locator("input[name='person-birth-date']").FillAsync("1985/06/08");
        await modal.Locator(".crud-form footer .btn-primary").ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync(new() { Timeout = 15_000 });

        await SearchPersons(marker);
        ILocator row = PersonRow(marker);
        await Assertions.Expect(row).ToBeVisibleAsync();
        string code = (await row.Locator(".code-pill").InnerTextAsync()).Trim();
        Assert.Matches("^[1-9][0-9]*$", code);
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await SearchPersons(marker);
        await Assertions.Expect(row).ToBeVisibleAsync();

        await PersonAction(row, "eye").ClickAsync();
        await Assertions.Expect(modal).ToBeVisibleAsync();
        await Assertions.Expect(modal.Locator("[data-testid='person-first-name']")).ToBeDisabledAsync();
        await modal.Locator(".crud-form footer .btn-secondary").ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync();

        await PersonAction(row, "edit").ClickAsync();
        await Assertions.Expect(modal).ToBeVisibleAsync();
        await Assertions.Expect(modal.Locator("input[name='person-birth-date']")).ToHaveValueAsync("1985/06/08");
        string edited = marker + "Edited";
        await modal.Locator("[data-testid='person-last-name']").FillAsync(edited);
        await modal.Locator(".crud-form footer .btn-primary").ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync(new() { Timeout = 15_000 });
        row = PersonRow(edited);
        await Assertions.Expect(row).ToBeVisibleAsync();
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await SearchPersons(edited);
        await Assertions.Expect(row).ToBeVisibleAsync();

        await PersonAction(row, "power").ClickAsync();
        await Assertions.Expect(row).ToContainTextAsync("Inactive");
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await SearchPersons(edited);
        await Assertions.Expect(row).ToContainTextAsync("Inactive");
        await PersonAction(row, "power").ClickAsync();
        await Assertions.Expect(row).ToContainTextAsync("Active");

        await SwitchCulture("fa");
        await SearchPersons(edited);
        row = PersonRow(edited);
        await PersonAction(row, "edit").ClickAsync();
        Assert.Equal("fa", await page.Locator("html").GetAttributeAsync("lang"));
        await Assertions.Expect(page.Locator(".language-switch a[hreflang='fa']")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("selected"));
        await Assertions.Expect(modal.Locator(".locale-date-input")).ToHaveAttributeAsync("data-calendar-culture", "fa");
        await modal.Locator(".date-picker-button").ClickAsync();
        await Assertions.Expect(modal.Locator(".calendar-period-button").Nth(1)).ToContainTextAsync("1364");
        await modal.Locator(".date-picker-button").ClickAsync();
        await Assertions.Expect(modal.Locator("input[name='person-birth-date']")).ToHaveValueAsync("1364/03/18");
        await modal.Locator(".crud-form footer .btn-secondary").ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync();

        await SwitchCulture("ar");
        await SearchPersons(edited);
        row = PersonRow(edited);
        await PersonAction(row, "edit").ClickAsync();
        await Assertions.Expect(modal.Locator("input[name='person-birth-date']")).ToHaveValueAsync("1985/06/08");
        await modal.Locator(".crud-form footer .btn-secondary").ClickAsync();

        await SwitchCulture("en");
        await SearchPersons(edited);
        row = PersonRow(edited);
        await PersonAction(row, "trash").ClickAsync();
        await ConfirmDanger();
        await Assertions.Expect(page.Locator(".inline-error")).ToContainTextAsync("protected history or references");

        string disposable = marker + "Safe";
        await page.Locator("header.crud-header button.btn-primary").ClickAsync();
        await modal.Locator("[data-testid='person-first-name']").FillAsync("Safe");
        await modal.Locator("[data-testid='person-last-name']").FillAsync(disposable);
        await modal.Locator(".crud-form footer .btn-primary").ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync(new() { Timeout = 15_000 });
        await SearchPersons(disposable);
        ILocator disposableRow = PersonRow(disposable);
        await Assertions.Expect(disposableRow).ToBeVisibleAsync();
        await PersonAction(disposableRow, "trash").ClickAsync();
        await ConfirmDanger();
        await Assertions.Expect(disposableRow).ToBeHiddenAsync();
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await SearchPersons(disposable);
        await Assertions.Expect(disposableRow).ToBeHiddenAsync();

        await SearchPersons("پیمان خوشه چین");
        ILocator protectedRow = page.Locator("table.data-grid tbody tr").Filter(new() { HasText = "پیمان خوشه چین" }).First;
        await Assertions.Expect(protectedRow).ToBeVisibleAsync();
        await PersonAction(protectedRow, "trash").ClickAsync();
        await ConfirmDanger();
        await Assertions.Expect(page.Locator(".inline-error")).ToContainTextAsync("protected history or references");
    }

    [Fact]
    public async Task CustomersCompleteCrudChildrenStatusProtectedDeleteAndLocaleDateRoundTrip()
    {
        string stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        string code = "E2E" + stamp;
        string lastName = "Customer" + stamp;
        string bankCaption="Acceptance Bank "+stamp;
        await CreateReference("bank","bank"+stamp,bankCaption);
        await GotoCulture("en", "/manage/customers");
        await page.Locator("header.customer-page-header button.btn-primary").ClickAsync();
        ILocator modal = RootCustomerEditor;
        await Assertions.Expect(modal).ToBeVisibleAsync();
        await Fill(modal, "Customer Number", code);
        await Fill(modal, "Mobile", "09120000000");
        await Fill(modal, "First Name", "E2E");
        await Fill(modal, "Last Name", lastName);
        await FillDate(modal, "1985/06/08");
        await modal.Locator(".customer-editor-actions .btn-primary").ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync(new() { Timeout = 15_000 });

        await SearchCustomers(code);
        ILocator row = CustomerRow(code);
        await Assertions.Expect(row).ToBeVisibleAsync();
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await SearchCustomers(code);
        await Assertions.Expect(row).ToBeVisibleAsync();

        await CustomerAction(row, "eye").ClickAsync();
        await Assertions.Expect(modal).ToBeVisibleAsync();
        await Assertions.Expect(Field(modal, "First Name")).ToBeDisabledAsync();
        Assert.Equal(0, await modal.Locator(".customer-editor-actions .btn-primary").CountAsync());
        await modal.Locator(".customer-editor-actions .btn-secondary").ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync();

        await CustomerAction(row, "edit").ClickAsync();
        await Assertions.Expect(modal).ToBeVisibleAsync();
        string editedName = "Edited " + stamp;
        await Fill(modal, "Customer Name", editedName);
        await modal.Locator(".customer-editor-actions .btn-primary").ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync(new() { Timeout = 15_000 });
        row = CustomerRow(code);
        await Assertions.Expect(row).ToContainTextAsync(editedName);
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await SearchCustomers(code);
        await Assertions.Expect(row).ToContainTextAsync(editedName);

        await CustomerAction(row, "edit").ClickAsync();
        await Assertions.Expect(modal).ToBeVisibleAsync();
        await Field(modal, "First Name").FillAsync("");
        await modal.Locator(".customer-editor-actions .btn-primary").ClickAsync();
        await Assertions.Expect(modal).ToBeVisibleAsync();
        await Assertions.Expect(modal.Locator(".inline-error")).ToBeVisibleAsync();
        await Field(modal, "First Name").FillAsync("E2E");
        await Fill(modal, "Customer Name", "Discarded " + stamp);
        await modal.Locator(".customer-editor-actions .btn-secondary").ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync();
        await Assertions.Expect(row).ToContainTextAsync(editedName);

        await CustomerAction(row, "power").ClickAsync();
        await Assertions.Expect(row).ToContainTextAsync("Inactive");
        await CustomerAction(row, "unlink").ClickAsync();
        await Assertions.Expect(row).ToContainTextAsync("Blocked");
        await CustomerAction(row, "restore").ClickAsync();
        await Assertions.Expect(row).ToContainTextAsync("Active");


        await CustomerAction(row,"edit").ClickAsync();
        await Assertions.Expect(modal.GetByRole(AriaRole.Tab,new(){Name="Contacts",Exact=true})).ToHaveCountAsync(0);
        await Fill(modal,"Customer Name","Unsaved parent "+stamp);
        await modal.GetByRole(AriaRole.Tab,new(){Name="Addresses",Exact=true}).ClickAsync();
        ILocator addresses=modal.Locator("[data-child-kind='addresses']");
        string addressTitle="Address "+stamp;
        await addresses.GetByRole(AriaRole.Button,new(){Name="Add Address",Exact=true}).ClickAsync();
        await PickReference(ChildEditor,"addressTypeId","Shipping");
        await ChildEditor.Locator("[name=title]").FillAsync(addressTitle);
        await ChildEditor.Locator("[name=addressLine1]").FillAsync("Acceptance address line");
        await ChildEditor.Locator("[name=email]").FillAsync("delivery@example.com");
        await ChildEditor.Locator("[name=notes]").FillAsync("Delivery instructions");
        await SaveChild();
        ILocator addressRow=addresses.Locator("tbody tr").Filter(new(){HasText=addressTitle});
        await Assertions.Expect(addressRow).ToBeVisibleAsync();
        await CustomerAction(addressRow,"eye").ClickAsync();
        await Assertions.Expect(ChildEditor.Locator("[name=title]")).ToBeDisabledAsync();
        await Assertions.Expect(ChildEditor.Locator("[name=email]")).ToHaveValueAsync("delivery@example.com");
        await Assertions.Expect(ChildEditor.Locator("footer .btn-primary")).ToHaveCountAsync(0);
        await CancelChild();
        await CustomerAction(addressRow,"edit").ClickAsync();
        string editedAddressTitle=addressTitle+" Updated";
        await ChildEditor.Locator("[name=title]").FillAsync(editedAddressTitle);
        await SaveChild();
        addressRow=addresses.Locator("tbody tr").Filter(new(){HasText=editedAddressTitle});
        await Assertions.Expect(addressRow).ToBeVisibleAsync();
        await CustomerAction(addressRow,"edit").ClickAsync();
        await ChildEditor.Locator("[name=title]").FillAsync("Discarded address");
        await CancelChild();
        await Assertions.Expect(addressRow).ToContainTextAsync(editedAddressTitle);
        await Assertions.Expect(addresses).Not.ToContainTextAsync("Discarded address");
        await addressRow.GetByRole(AriaRole.Button,new(){Name="Set Default",Exact=true}).ClickAsync();
        await Assertions.Expect(addressRow.Locator(".status-badge").Filter(new(){HasTextRegex=new System.Text.RegularExpressions.Regex("^Default$")})).ToBeVisibleAsync();

        await addresses.GetByRole(AriaRole.Button,new(){Name="Add Address",Exact=true}).ClickAsync();
        await PickReference(ChildEditor,"addressTypeId","Shipping");
        await ChildEditor.Locator("[name=title]").FillAsync("Second "+stamp);
        await ChildEditor.Locator("[name=addressLine1]").FillAsync("Second delivery address");
        await ChildEditor.Locator("[name=isDefault]").CheckAsync();
        await SaveChild();
        ILocator second=addresses.Locator("tbody tr").Filter(new(){HasText="Second "+stamp});
        await Assertions.Expect(second.Locator(".status-badge").Filter(new(){HasTextRegex=new System.Text.RegularExpressions.Regex("^Default$")})).ToBeVisibleAsync();
        await Assertions.Expect(addresses.Locator(".status-badge").Filter(new(){HasTextRegex=new System.Text.RegularExpressions.Regex("^Default$")})).ToHaveCountAsync(1);
        await Assertions.Expect(addressRow.Locator(".status-badge").Filter(new(){HasTextRegex=new System.Text.RegularExpressions.Regex("^Default$")})).ToHaveCountAsync(0);
        await CustomerAction(second,"trash").ClickAsync();
        await DeleteChild();
        await Assertions.Expect(second).ToBeHiddenAsync();
        await addressRow.GetByRole(AriaRole.Button,new(){Name="Set Default",Exact=true}).ClickAsync();
        await CustomerAction(addressRow,"power").ClickAsync();
        await Assertions.Expect(addressRow).ToContainTextAsync("Inactive");
        await Assertions.Expect(addressRow.Locator(".status-badge").Filter(new(){HasTextRegex=new System.Text.RegularExpressions.Regex("^Default$")})).ToHaveCountAsync(0);
        await CustomerAction(addressRow,"power").ClickAsync();
        await Assertions.Expect(addressRow.Locator(".status-badge").Filter(new(){HasTextRegex=new System.Text.RegularExpressions.Regex("^Active$")})).ToBeVisibleAsync();
        await modal.Locator(".customer-editor-actions .btn-secondary").ClickAsync();
        await Assertions.Expect(row).ToContainTextAsync(editedName);
        await Assertions.Expect(row).Not.ToContainTextAsync("Unsaved parent");

        await CustomerAction(row,"edit").ClickAsync();
        ILocator financialTab=modal.GetByRole(AriaRole.Tab,new(){Name="Financial & Commercial",Exact=true});
        await Assertions.Expect(financialTab).ToBeVisibleAsync();
        await financialTab.ClickAsync();
        await Fill(modal,"Credit Limit","250000");
        await modal.Locator(".customer-editor-actions .btn-primary").ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync();
        await CustomerAction(row,"edit").ClickAsync();
        await modal.GetByRole(AriaRole.Tab,new(){Name="Bank Accounts",Exact=true}).ClickAsync();
        ILocator banks=modal.Locator("[data-child-kind='bank-accounts']");
        await banks.GetByRole(AriaRole.Button,new(){Name="Add",Exact=true}).ClickAsync();
        await PickReference(ChildEditor,"bankId",bankCaption);
        await ChildEditor.Locator("[name=accountName]").FillAsync("Operations "+stamp);
        await ChildEditor.Locator("[name=accountNumber]").FillAsync(stamp);
        await ChildEditor.Locator("[name=accountHolderName]").FillAsync(editedName);
        await ChildEditor.Locator("[name=ownershipType]").SelectOptionAsync("Joint");
        await ChildEditor.Locator("[name=cardNumber]").FillAsync("1234567890123456");
        await ChildEditor.Locator("[name=branchCode]").FillAsync("HQ");
        await SaveChild();
        ILocator bankRow=banks.Locator("tbody tr").Filter(new(){HasText="Operations "+stamp});
        await Assertions.Expect(bankRow).ToBeVisibleAsync();
        await CustomerAction(bankRow,"eye").ClickAsync();
        await Assertions.Expect(ChildEditor.Locator("[name=accountNumber]")).ToHaveValueAsync(stamp);
        await Assertions.Expect(ChildEditor.Locator("[name=accountNumber]")).ToBeDisabledAsync();
        await Assertions.Expect(ChildEditor.Locator("[name=ownershipType]")).ToHaveValueAsync("Joint");
        await CancelChild();
        await CustomerAction(bankRow,"edit").ClickAsync();
        string editedBankName="Operations Updated "+stamp;
        await ChildEditor.Locator("[name=accountName]").FillAsync(editedBankName);
        await SaveChild();
        bankRow=banks.Locator("tbody tr").Filter(new(){HasText=editedBankName});
        await Assertions.Expect(bankRow).ToBeVisibleAsync();
        await CustomerAction(bankRow,"edit").ClickAsync();
        await ChildEditor.Locator("[name=accountName]").FillAsync("Discarded bank");
        await CancelChild();
        await Assertions.Expect(bankRow).ToContainTextAsync(editedBankName);
        await bankRow.GetByRole(AriaRole.Button,new(){Name="Set Default",Exact=true}).ClickAsync();
        await Assertions.Expect(bankRow.Locator(".status-badge").Filter(new(){HasTextRegex=new System.Text.RegularExpressions.Regex("^Default$")})).ToBeVisibleAsync();
        await CustomerAction(bankRow,"power").ClickAsync();
        await Assertions.Expect(bankRow).ToContainTextAsync("Inactive");
        await CustomerAction(bankRow,"power").ClickAsync();
        await Assertions.Expect(bankRow.Locator(".status-badge").Filter(new(){HasTextRegex=new System.Text.RegularExpressions.Regex("^Active$")})).ToBeVisibleAsync();
        await modal.Locator(".customer-editor-actions .btn-secondary").ClickAsync();
        await page.ReloadAsync(new(){WaitUntil=WaitUntilState.NetworkIdle});
        await SearchCustomers(code);
        row=CustomerRow(code);
        await CustomerAction(row,"edit").ClickAsync();
        await modal.GetByRole(AriaRole.Tab,new(){Name="Addresses",Exact=true}).ClickAsync();
        await Assertions.Expect(modal.Locator("[data-child-kind=addresses] tbody tr").Filter(new(){HasText=editedAddressTitle})).ToBeVisibleAsync();
        await Assertions.Expect(modal.GetByRole(AriaRole.Tab,new(){Name="Contacts",Exact=true})).ToHaveCountAsync(0);
        await modal.GetByRole(AriaRole.Tab,new(){Name="Financial & Commercial",Exact=true}).ClickAsync();
        await Assertions.Expect(Field(modal,"Credit Limit")).ToHaveValueAsync(new System.Text.RegularExpressions.Regex("^250000(?:\\.0+)?$"));
        await modal.GetByRole(AriaRole.Tab,new(){Name="Bank Accounts",Exact=true}).ClickAsync();
        bankRow=modal.Locator("[data-child-kind='bank-accounts'] tbody tr").Filter(new(){HasText=editedBankName});
        await Assertions.Expect(bankRow).ToBeVisibleAsync();
        await CustomerAction(bankRow,"eye").ClickAsync();
        await Assertions.Expect(ChildEditor.Locator("[name=cardNumber]")).ToHaveValueAsync("1234567890123456");
        await Assertions.Expect(ChildEditor.Locator("[name=branchCode]")).ToHaveValueAsync("HQ");
        await CancelChild();
        await CustomerAction(bankRow,"trash").ClickAsync();
        await DeleteChild();
        await Assertions.Expect(bankRow).ToBeHiddenAsync();
        await modal.Locator(".customer-editor-actions .btn-secondary").ClickAsync();
        await ExerciseLegalContacts(stamp);
        await SearchCustomers(code);
        row=CustomerRow(code);
        await CustomerAction(row, "trash").ClickAsync();
        await ConfirmDanger();
        await Assertions.Expect(page.Locator(".inline-error")).ToContainTextAsync("protected history or references");

        await SwitchCulture("fa");
        await SearchCustomers(code);
        row = CustomerRow(code);
        await CustomerAction(row, "edit").ClickAsync();
        await Assertions.Expect(modal.Locator(".locale-date-input input")).ToHaveValueAsync("1364/03/18");
        await modal.Locator(".customer-editor-actions .btn-secondary").ClickAsync();
        await SwitchCulture("ar");
        await SearchCustomers(code);
        row = CustomerRow(code);
        await CustomerAction(row, "edit").ClickAsync();
        await Assertions.Expect(modal.Locator(".locale-date-input input")).ToHaveValueAsync("1985/06/08");
        await modal.Locator(".customer-editor-actions .btn-secondary").ClickAsync();

        await SwitchCulture("en");
        string safeCode = code + "S";
        await page.Locator("header.customer-page-header button.btn-primary").ClickAsync();
        await Fill(modal, "Customer Number", safeCode);
        await Fill(modal, "Mobile", "09122222222");
        await Fill(modal, "First Name", "Safe");
        await Fill(modal, "Last Name", stamp);
        await modal.Locator(".customer-editor-actions .btn-primary").ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync(new() { Timeout = 15_000 });
        await SearchCustomers(safeCode);
        ILocator safeRow = CustomerRow(safeCode);
        await Assertions.Expect(safeRow).ToBeVisibleAsync();
        await CustomerAction(safeRow, "trash").ClickAsync();
        await ConfirmDanger();
        await Assertions.Expect(safeRow).ToBeHiddenAsync();
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await SearchCustomers(safeCode);
        await Assertions.Expect(safeRow).ToBeHiddenAsync();
    }

    [Fact]
    public async Task Legal_customer_fields_and_establishment_date_roundtrip_between_gregorian_and_jalali()
    {
        string stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        string code = "LEGAL" + stamp;
        string legalName = "Legal Customer " + stamp;
        await GotoCulture("en", "/manage/customers");
        await page.Locator("header.customer-page-header button.btn-primary").ClickAsync();
        ILocator modal = page.Locator(".barang-modal.modal-large");
        await Assertions.Expect(modal).ToBeVisibleAsync();
        await modal.Locator("label").Filter(new() { HasText = "Customer Type" }).Locator("select").SelectOptionAsync("LegalEntity");
        await Fill(modal, "Customer Number", code);
        await Fill(modal, "Legal Name", legalName);
        await FillDate(modal, "1985/06/08");
        Assert.Equal(0, await modal.Locator("label").Filter(new() { HasText = "First Name" }).CountAsync());
        await Assertions.Expect(Field(modal, "Legal Name")).ToBeEditableAsync();
        await modal.Locator(".customer-editor-actions .btn-primary").ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync(new() { Timeout = 15_000 });

        await SearchCustomers(code);
        ILocator row = CustomerRow(code);
        await Assertions.Expect(row).ToBeVisibleAsync();
        await CustomerAction(row, "edit").ClickAsync();
        await Assertions.Expect(modal.Locator(".locale-date-input input")).ToHaveValueAsync("1985/06/08");
        await modal.Locator(".customer-editor-actions .btn-secondary").ClickAsync();

        await SwitchCulture("fa");
        await SearchCustomers(code);
        row = CustomerRow(code);
        await CustomerAction(row, "edit").ClickAsync();
        await Assertions.Expect(modal.Locator(".locale-date-input input")).ToHaveValueAsync("1364/03/18");
        await modal.Locator(".customer-editor-actions .btn-secondary").ClickAsync();

        await SwitchCulture("en");
        await SearchCustomers(code);
        row = CustomerRow(code);
        await CustomerAction(row, "trash").ClickAsync();
        await ConfirmDanger();
        await Assertions.Expect(row).ToBeHiddenAsync();
    }


    private ILocator RootCustomerEditor=>page.Locator(".barang-modal:has(> .dialog-body > .customer-tabs)");
    private ILocator ChildEditor=>page.Locator(".barang-modal:has(> .dialog-body > .reference-editor)");
    private async Task SaveChild(){await ChildEditor.Locator(":scope > footer .btn-primary").ClickAsync();await Assertions.Expect(ChildEditor).ToBeHiddenAsync();await Assertions.Expect(RootCustomerEditor).ToBeVisibleAsync();}
    private async Task CancelChild(){await ChildEditor.Locator(":scope > footer .btn-secondary").ClickAsync();await Assertions.Expect(ChildEditor).ToBeHiddenAsync();await Assertions.Expect(RootCustomerEditor).ToBeVisibleAsync();}
    private async Task DeleteChild(){ILocator confirm=page.Locator(".barang-modal:has(> header h2:text-is(\"Delete\"))");await confirm.Locator(":scope > footer .btn-danger").ClickAsync();await Assertions.Expect(confirm).ToBeHiddenAsync();}
    private static async Task PickReference(ILocator editor,string name,string caption){ILocator input=editor.Locator($"input[name='{name}']");await input.FillAsync(caption);await input.Locator("..").GetByRole(AriaRole.Option,new(){Name=caption,Exact=true}).ClickAsync();await Assertions.Expect(input).ToHaveValueAsync(caption);}
    private async Task CreateReference(string type,string code,string caption){
        string domain=type is "contact.role" or "address.type" or "legal.entity.type" or "payment.term" or "payment.condition" or "payment.method" or "tax.status" or "bank" or "bank.account.type" or "price.group" or "unit.of.measure" or "packaging.type" or "shipping.method" ? "shared" : "customers";
        await GotoCulture("en",$"/master/reference-data/{domain}/{type}");
        await page.Locator("[data-reference-type]").GetByRole(AriaRole.Button,new(){Name="Add",Exact=true}).ClickAsync();
        ILocator editor=ChildEditor;
        await editor.Locator("[name=reference-code]").FillAsync(code);
        foreach(string culture in new[]{"fa","ar","en"})await editor.Locator($"[name='reference-name-{culture}']").FillAsync(caption);
        await editor.Locator(":scope > footer .btn-primary").ClickAsync();
        await Assertions.Expect(editor).ToBeHiddenAsync();
    }
    private async Task ExerciseLegalContacts(string stamp){
        string code="CONTACT"+stamp,name="Contact"+stamp,updatedName=name+"Updated";
        await page.Locator("header.customer-page-header button.btn-primary").ClickAsync();
        ILocator root=RootCustomerEditor;
        await root.Locator("label").Filter(new(){HasText="Customer Type"}).Locator("select").SelectOptionAsync("LegalEntity");
        await Fill(root,"Customer Number",code);await Fill(root,"Legal Name","Contact owner "+stamp);
        await root.Locator(".customer-editor-actions .btn-primary").ClickAsync();
        await Assertions.Expect(root).ToBeHiddenAsync();await SearchCustomers(code);
        ILocator owner=CustomerRow(code);await CustomerAction(owner,"edit").ClickAsync();
        await root.GetByRole(AriaRole.Tab,new(){Name="Contacts",Exact=true}).ClickAsync();
        ILocator grid=root.Locator("[data-child-kind=contacts]");
        await grid.GetByRole(AriaRole.Button,new(){Name="Add Contact",Exact=true}).ClickAsync();
        await ChildEditor.Locator("[name=firstName]").FillAsync(name);await ChildEditor.Locator("[name=lastName]").FillAsync("E2E");await ChildEditor.Locator("[name=mobile]").FillAsync("09121111111");
        await PickReference(ChildEditor,"contactRoleId","General");await SaveChild();
        ILocator contact=grid.Locator("tbody tr").Filter(new(){HasText=name});
        await Assertions.Expect(contact).ToBeVisibleAsync();
        await CustomerAction(contact,"eye").ClickAsync();await Assertions.Expect(ChildEditor.Locator("[name=firstName]")).ToBeDisabledAsync();await CancelChild();
        await CustomerAction(contact,"edit").ClickAsync();await ChildEditor.Locator("[name=firstName]").FillAsync(updatedName);await SaveChild();
        contact=grid.Locator("tbody tr").Filter(new(){HasText=updatedName});await Assertions.Expect(contact).ToBeVisibleAsync();
        await CustomerAction(contact,"edit").ClickAsync();await ChildEditor.Locator("[name=firstName]").FillAsync("Discarded contact");await CancelChild();await Assertions.Expect(contact).ToContainTextAsync(updatedName);
        await contact.GetByRole(AriaRole.Button,new(){Name="Set Primary",Exact=true}).ClickAsync();await Assertions.Expect(contact).ToContainTextAsync("Primary");
        await CustomerAction(contact,"power").ClickAsync();await Assertions.Expect(contact).ToContainTextAsync("Inactive");
        await CustomerAction(contact,"power").ClickAsync();await Assertions.Expect(contact.Locator(".status-badge").Filter(new(){HasTextRegex=new System.Text.RegularExpressions.Regex("^Active$")})).ToBeVisibleAsync();
        await root.Locator(".customer-editor-actions .btn-secondary").ClickAsync();await page.ReloadAsync(new(){WaitUntil=WaitUntilState.NetworkIdle});await SearchCustomers(code);
        await CustomerAction(CustomerRow(code),"edit").ClickAsync();await root.GetByRole(AriaRole.Tab,new(){Name="Contacts",Exact=true}).ClickAsync();
        contact=root.Locator("[data-child-kind=contacts] tbody tr").Filter(new(){HasText=updatedName});await Assertions.Expect(contact).ToBeVisibleAsync();await Assertions.Expect(contact).ToContainTextAsync("09121111111");
        await CustomerAction(contact,"trash").ClickAsync();await DeleteChild();await Assertions.Expect(contact).ToBeHiddenAsync();
        await root.Locator(".customer-editor-actions .btn-secondary").ClickAsync();
        await CustomerAction(CustomerRow(code),"trash").ClickAsync();await ConfirmDanger();await Assertions.Expect(CustomerRow(code)).ToBeHiddenAsync();
    }

    private async Task GotoCulture(string culture, string path) =>
        await page.GotoAsync($"{baseUrl}/culture?culture={culture}&returnUrl={Uri.EscapeDataString(path)}", new() { WaitUntil = WaitUntilState.NetworkIdle });

    private async Task SwitchCulture(string culture)
    {
        await page.Locator($".language-switch a[hreflang='{culture}']").ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.WaitForTimeoutAsync(1_000);
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("lang", culture);
        await page.WaitForTimeoutAsync(800);
    }

    private ILocator PersonRow(string value) => page.Locator("table.persons-grid tbody tr").Filter(new() { HasText = value }).First;
    private ILocator CustomerRow(string value) => page.Locator("table.customer-grid tbody tr").Filter(new() { HasText = value }).First;
    private static ILocator PersonAction(ILocator scope, string icon) => scope.Locator($".barang-icon-button:has(svg[data-icon='{icon}'])").First;
    private static ILocator CustomerAction(ILocator scope, string icon) => PersonAction(scope, icon);

    private async Task SearchPersons(string value)
    {
        ILocator search = page.Locator(".crud-toolbar .search-box input");
        await search.FillAsync(value);
        await search.PressAsync("End");
        await page.WaitForTimeoutAsync(500);
    }

    private async Task SearchCustomers(string value)
    {
        await page.Locator(".customer-toolbar .customer-search input").FillAsync(value);
        await page.WaitForTimeoutAsync(700);
    }

    private static ILocator Field(ILocator scope, string label) =>
        scope.Locator("label").Filter(new() { HasText = label }).Locator("input").First;

    private static async Task Fill(ILocator scope, string label, string value) => await Field(scope, label).FillAsync(value);
    private async Task FillDate(ILocator scope, string value)
    {
        ILocator date = scope.Locator(".locale-date-input").First;
        string culture = await page.Locator("html").GetAttributeAsync("lang") ?? "fa";
        await Assertions.Expect(date).ToHaveAttributeAsync("data-calendar-culture", culture);
        await Assertions.Expect(date).ToHaveAttributeAsync("data-calendar-ready", "true");
        await date.Locator("input").FillAsync(value);
    }

    private async Task ConfirmDanger()
    {
        ILocator confirm = page.Locator(".barang-modal.modal-small");
        await Assertions.Expect(confirm).ToBeVisibleAsync();
        await confirm.Locator("footer .btn-danger").ClickAsync();
        await Assertions.Expect(confirm).ToBeHiddenAsync();
    }
}
