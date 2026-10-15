using Microsoft.Playwright;
using Xunit;

namespace BARANG.Backoffice.E2ETests;

public sealed class OrdersE2ETests : IAsyncLifetime
{
    private IPlaywright playwright = null!; private IBrowser browser = null!; private IBrowserContext context = null!; private IPage page = null!;
    private readonly string baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL") ?? "http://localhost:5100";
    public async ValueTask InitializeAsync(){playwright=await Playwright.CreateAsync();browser=await playwright.Chromium.LaunchAsync(new(){Headless=true,ExecutablePath=Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH")??@"C:\Program Files\Google\Chrome\Application\chrome.exe"});context=await browser.NewContextAsync(new(){Locale="en-US",ViewportSize=new(){Width=1440,Height=1000}});page=await context.NewPageAsync();await page.GotoAsync(baseUrl+"/login",new(){WaitUntil=WaitUntilState.DOMContentLoaded});await page.WaitForTimeoutAsync(1000);await page.Locator("#username").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME")??"admin");await page.Locator("#password").FillAsync(Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD")??"BarangDev@2026");Task loginNavigation=page.WaitForURLAsync(url=>new Uri(url).AbsolutePath!="/login",new(){WaitUntil=WaitUntilState.Commit});await page.Locator("button[type=submit]").ClickAsync();try{await loginNavigation;}catch(Exception ex){string body=await page.Locator("body").InnerTextAsync();throw new InvalidOperationException($"Backoffice login did not navigate. Url={page.Url}; Body={body[..Math.Min(body.Length,1000)]}",ex);}if(new Uri(page.Url).AbsolutePath=="/company"){await page.Locator(".company-list form button[type=submit]").First.ClickAsync();await page.WaitForURLAsync(url=>new Uri(url).AbsolutePath!="/company",new(){WaitUntil=WaitUntilState.Commit});}}
    [Fact]
    public async Task OrdersWorkspaceIsCanonicalAndExposesOperationalActions(){
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Fworkspace%2Forders",new(){WaitUntil=WaitUntilState.DOMContentLoaded});
        await page.GotoAsync(baseUrl+"/workspace/orders",new(){WaitUntil=WaitUntilState.DOMContentLoaded});
        var grid=page.Locator("[data-orders-grid]"); await Assertions.Expect(grid).ToBeVisibleAsync();
        await Assertions.Expect(grid.Locator(".crud-search input")).ToHaveCountAsync(1); await Assertions.Expect(grid.Locator(".crud-toolbar button")).ToHaveCountAsync(1); await Assertions.Expect(grid).Not.ToContainTextAsync("Refresh");
        await Assertions.Expect(page.GetByRole(AriaRole.Link,new(){Name="New order",Exact=true})).ToBeVisibleAsync();
        var first=grid.Locator("tbody tr").First; await Assertions.Expect(first.GetByRole(AriaRole.Button,new(){Name="Order 360",Exact=true})).ToBeVisibleAsync(); await Assertions.Expect(first.GetByRole(AriaRole.Button,new(){Name="View",Exact=true})).ToHaveCountAsync(0);
        await first.GetByRole(AriaRole.Button,new(){Name="Order 360",Exact=true}).ClickAsync(); await page.WaitForURLAsync("**/360", new(){WaitUntil=WaitUntilState.Commit}); await Assertions.Expect(page.Locator(".order360-shell")).ToBeVisibleAsync(); await Assertions.Expect(page.Locator(".order360-head .eyebrow")).ToBeVisibleAsync(); await Assertions.Expect(page.GetByRole(AriaRole.Button,new(){Name="Edit",Exact=true})).ToHaveCountAsync(0); await page.GetByRole(AriaRole.Button,new(){Name="Customer",Exact=true}).First.ClickAsync(); await Assertions.Expect(page.Locator(".order360-detail h2")).ToContainTextAsync("Customer"); await page.GetByRole(AriaRole.Button,new(){Name="Print operations",Exact=true}).First.ClickAsync(); await Assertions.Expect(page.Locator(".order360-detail h2")).ToContainTextAsync("Print operations"); await page.GetByRole(AriaRole.Button,new(){Name="Historical price snapshot",Exact=true}).First.ClickAsync(); await Assertions.Expect(page.Locator(".order360-detail h2")).ToContainTextAsync("Historical price snapshot"); await page.GetByRole(AriaRole.Button,new(){Name="Close",Exact=true}).ClickAsync(); await Assertions.Expect(page.Locator("[data-orders-grid]")).ToBeVisibleAsync();
    }
    [Fact]
    public async Task OrderStepOneRendersHeaderWizardAndLocalizedDateControl(){
        await page.GotoAsync(baseUrl+"/culture?culture=fa&returnUrl=%2Forders%2Fnew",new(){WaitUntil=WaitUntilState.DOMContentLoaded});
        await page.GotoAsync(baseUrl+"/orders/new",new(){WaitUntil=WaitUntilState.DOMContentLoaded});
        await Assertions.Expect(page.Locator(".wizard-stepper")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".wizard-stepper button")).ToHaveCountAsync(7);
        await Assertions.Expect(page.Locator("input[type=date]")).ToHaveCountAsync(0);
        await Assertions.Expect(page.GetByText("ذخیره پیش‌نویس",new(){Exact=true})).ToHaveCountAsync(0);
        await SelectFirstOption("order-branch");
        string branchCaption=await page.Locator("input[name='order-branch']").InputValueAsync();
        await SelectFirstOption("order-customer");
        string customerCaption=await page.Locator("input[name='order-customer']").InputValueAsync();
        string branchId=await page.Locator("input[name='order-branch']").GetAttributeAsync("data-selected-value")??"";
        string customerId=await page.Locator("input[name='order-customer']").GetAttributeAsync("data-selected-value")??"";
        try { await page.GetByRole(AriaRole.Button,new(){Name="بعدی",Exact=true}).ClickAsync(); }
        catch(Exception ex) { throw new Xunit.Sdk.XunitException($"Step 1 Next click failed; branchId={branchId}; customerId={customerId}; url={page.Url}; body={await page.Locator("body").InnerTextAsync()}",ex); }
        try { await Assertions.Expect(page.Locator(".order-items-step")).ToBeVisibleAsync(); }
        catch(Exception ex) { throw new Xunit.Sdk.XunitException($"Step 1 Next was rejected; branchId={branchId}; customerId={customerId}; url={page.Url}; body={await page.Locator("body").InnerTextAsync()}",ex); }
        await page.ReloadAsync(new(){WaitUntil=WaitUntilState.DOMContentLoaded});
        await page.WaitForTimeoutAsync(1000);
        await Assertions.Expect(page.Locator(".order-items-step")).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator("input[name='order-branch']")).ToHaveValueAsync(branchCaption);
        await Assertions.Expect(page.Locator("input[name='order-customer']")).ToHaveValueAsync(customerCaption);
        await Assertions.Expect(page.GetByText("ذخیره پیش‌نویس",new(){Exact=true})).ToHaveCountAsync(0);
        string reloadedBranchId=await page.Locator("input[name='order-branch']").GetAttributeAsync("data-selected-value")??"";
        string reloadedCustomerId=await page.Locator("input[name='order-customer']").GetAttributeAsync("data-selected-value")??"";
        await page.GetByRole(AriaRole.Button,new(){Name="بعدی",Exact=true}).ClickAsync();
        try { await Assertions.Expect(page.Locator(".order-items-step")).ToBeVisibleAsync(); }
        catch(Exception ex) { throw new Xunit.Sdk.XunitException($"Reloaded Step 1 Next was rejected; branchId={reloadedBranchId}; customerId={reloadedCustomerId}; url={page.Url}; body={await page.Locator("body").InnerTextAsync()}",ex); }
    }
    [Fact]
    public async Task OrderStepTwoSupportsMultiItemDraftEditingInOneWizardSession(){
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Forders%2Fnew",new(){WaitUntil=WaitUntilState.DOMContentLoaded});
        await page.GotoAsync(baseUrl+"/orders/new",new(){WaitUntil=WaitUntilState.DOMContentLoaded});
        await SelectFirstOption("order-branch");
        await SelectFirstOption("order-customer");
        await page.GetByText("Next",new(){Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator(".order-items-step")).ToBeVisibleAsync();
        await page.Locator("input[name='order-item-product']").ClickAsync();
        await page.Locator(".order-items-step [role=option]").First.ClickAsync();
        await page.Locator("input[name='order-item-sku']").ClickAsync();
        await page.Locator(".order-items-step [role=option]").First.ClickAsync();
        await page.Locator("input[name='order-item-quantity']").FillAsync("2");
        await page.GetByText("Add item",new(){Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator(".order-items-step tbody tr")).ToHaveCountAsync(1);
        await page.Locator("input[name='order-item-product']").ClickAsync();
        await page.Locator(".order-items-step [role=option]").First.ClickAsync();
        await page.Locator("input[name='order-item-sku']").ClickAsync();
        await page.Locator(".order-items-step [role=option]").First.ClickAsync();
        await page.Locator("input[name='order-item-quantity']").FillAsync("3");
        await page.GetByText("Add item",new(){Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator(".order-items-step tbody tr")).ToHaveCountAsync(2);
        await Assertions.Expect(page.Locator(".order-items-step tbody tr").First.GetByRole(AriaRole.Button,new(){Name="View",Exact=true})).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".order-items-step tbody tr").First.GetByRole(AriaRole.Button,new(){Name="Edit",Exact=true})).ToBeVisibleAsync();
        await page.GetByText("Back",new(){Exact=true}).Last.ClickAsync();
        await Assertions.Expect(page.Locator("input[name='order-branch']")).ToBeVisibleAsync();
        await page.GetByText("Next",new(){Exact=true}).First.ClickAsync();
        await Assertions.Expect(page.Locator(".order-items-step tbody tr")).ToHaveCountAsync(2);
    }

    [Fact]
    public async Task OrderStepThreeSupportsPrintOperationCrudInSameWizardSession(){
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Forders%2Fnew",new(){WaitUntil=WaitUntilState.DOMContentLoaded});
        await page.GotoAsync(baseUrl+"/orders/new",new(){WaitUntil=WaitUntilState.DOMContentLoaded});
        await SelectFirstOption("order-branch");
        await SelectFirstOption("order-customer");
        await page.GetByText("Next",new(){Exact=true}).ClickAsync();
        await page.Locator("input[name='order-item-product']").ClickAsync();
        await page.Locator(".order-items-step [role=option]").First.ClickAsync();
        await page.Locator("input[name='order-item-sku']").ClickAsync();
        await page.Locator(".order-items-step [role=option]").First.ClickAsync();
        await page.Locator("input[name='order-item-quantity']").FillAsync("2");
        await page.GetByText("Add item",new(){Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator(".order-items-step tbody tr")).ToHaveCountAsync(1);
        await page.Locator(".order-items-step").GetByRole(AriaRole.Button,new(){Name="Next",Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator(".order-print-step-redesign")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".order-print-step-redesign .print-item-card")).ToHaveCountAsync(1);
        await page.GetByLabel("Width * cm").FillAsync("10");
        await page.GetByLabel("Height * cm").FillAsync("12");
        await page.Locator("select[name='print-method-step2']").SelectOptionAsync(new SelectOptionValue { Index = 1 });
        await page.Locator("input[type=file]").SetInputFilesAsync(new FilePayload
        {
            Name = "customer-artwork.pdf",
            MimeType = "application/pdf",
            Buffer = System.Text.Encoding.ASCII.GetBytes($"%PDF-1.4\n% BARANG {Guid.NewGuid():N}\n1 0 obj<</Type/Catalog>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF\n")
        });
        await Assertions.Expect(page.Locator(".order-print-editor")).ToContainTextAsync("customer-artwork.pdf");
        await page.Locator("[data-testid='print-add-button']").ClickAsync();
        await Assertions.Expect(page.Locator(".order-print-step-redesign tbody tr")).ToHaveCountAsync(1);
        await page.GetByText("Back",new(){Exact=true}).Last.ClickAsync();
        await Assertions.Expect(page.Locator(".order-items-step tbody tr")).ToHaveCountAsync(1);
        await page.Locator(".order-items-step > .form-actions .primary-link").Last.ClickAsync();
        await Assertions.Expect(page.Locator(".order-print-step-redesign tbody tr")).ToHaveCountAsync(1);
    }
    [Fact]
    public async Task OrderStepFourPersistsSelectedServicesAndPackagingInSameWizardSession(){
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Forders%2Fnew",new(){WaitUntil=WaitUntilState.DOMContentLoaded});
        await page.GotoAsync(baseUrl+"/orders/new",new(){WaitUntil=WaitUntilState.DOMContentLoaded});
        await SelectFirstOption("order-branch");
        await SelectFirstOption("order-customer");
        await page.GetByText("Next",new(){Exact=true}).ClickAsync();
        await page.Locator("input[name='order-item-product']").ClickAsync();
        await page.Locator(".order-items-step [role=option]").First.ClickAsync();
        await page.Locator("input[name='order-item-sku']").ClickAsync();
        await page.Locator(".order-items-step [role=option]").First.ClickAsync();
        await page.Locator("input[name='order-item-quantity']").FillAsync("2");
        await page.GetByText("Add item",new(){Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator(".order-items-step tbody tr")).ToHaveCountAsync(1);
        await page.Locator(".order-items-step").GetByRole(AriaRole.Button,new(){Name="Next",Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator(".order-print-step-redesign")).ToBeVisibleAsync();
        await page.Locator(".order-print-step-redesign").GetByRole(AriaRole.Button,new(){Name="Next",Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator(".order-services-step")).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Heading,new(){Name="Additional Services & Packaging",Exact=true})).ToBeVisibleAsync();

        ILocator serviceInput=page.Locator("input[name='order-step4-service']");
        await serviceInput.ClickAsync();
        ILocator serviceOptions=page.Locator(".order-services-step [role=listbox] [role=option]");
        if(await serviceOptions.CountAsync()>0){
            await serviceOptions.First.ClickAsync();
            ILocator itemSelect=page.Locator("select[name='order-step4-item']");
            if(!await itemSelect.IsDisabledAsync()) await itemSelect.SelectOptionAsync(new SelectOptionValue{Index=1});
            await page.GetByRole(AriaRole.Button,new(){Name="Add",Exact=true}).ClickAsync();
            await Assertions.Expect(page.Locator(".order-services-step tbody tr")).ToHaveCountAsync(1);
            await page.GetByRole(AriaRole.Button,new(){Name="Delete",Exact=true}).ClickAsync();
            await Assertions.Expect(page.Locator(".order-services-step tbody tr")).ToHaveCountAsync(0);
        }else{
            await Assertions.Expect(page.GetByText("No additional services selected.",new(){Exact=true})).ToBeVisibleAsync();
        }

        ILocator packaging=page.Locator("input[name='order-step4-packaging']");
        await packaging.ClickAsync();
        ILocator packagingOptions=page.Locator(".order-services-step [role=listbox] [role=option]");
        if(await packagingOptions.CountAsync()>1) await packagingOptions.Nth(1).ClickAsync();
        await page.Locator(".order-services-step").GetByRole(AriaRole.Button,new(){Name="Save draft",Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator(".order-services-step")).ToBeVisibleAsync();
        await page.GetByText("Back",new(){Exact=true}).ClickAsync();
        await page.Locator(".order-print-step-redesign").GetByRole(AriaRole.Button,new(){Name="Next",Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator(".order-services-step")).ToBeVisibleAsync();
    }
    [Fact]
    public async Task OrderStepFivePersistsDeliverySnapshotAndShippingMethod(){
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Forders%2Fnew",new(){WaitUntil=WaitUntilState.DOMContentLoaded});
        await page.GotoAsync(baseUrl+"/orders/new",new(){WaitUntil=WaitUntilState.DOMContentLoaded});
        await SelectFirstOption("order-branch");
        await SelectFirstOption("order-customer");
        await page.GetByText("Next",new(){Exact=true}).ClickAsync();
        await page.Locator("input[name='order-item-product']").ClickAsync(); await page.Locator(".order-items-step [role=option]").First.ClickAsync();
        await page.Locator("input[name='order-item-sku']").ClickAsync(); await page.Locator(".order-items-step [role=option]").First.ClickAsync();
        await page.Locator("input[name='order-item-quantity']").FillAsync("1"); await page.GetByText("Add item",new(){Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator(".order-items-step tbody tr")).ToHaveCountAsync(1);
        await page.Locator(".order-items-step").GetByRole(AriaRole.Button,new(){Name="Next",Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator(".order-print-step-redesign")).ToBeVisibleAsync();
        await page.Locator(".order-print-step-redesign").GetByRole(AriaRole.Button,new(){Name="Next",Exact=true}).ClickAsync();
        await page.Locator(".order-services-step").GetByRole(AriaRole.Button,new(){Name="Next",Exact=true}).ClickAsync();
        ILocator step=page.Locator(".order-shipping-step"); await Assertions.Expect(step).ToBeVisibleAsync();
        await Assertions.Expect(step.GetByRole(AriaRole.Heading,new(){Name="Delivery & Shipping",Exact=true})).ToBeVisibleAsync();
        ILocator addresses=step.Locator("button[name='order-step5-address']");
        if(await addresses.CountAsync()>0) await addresses.First.ClickAsync();
        else { await step.Locator("button[name='order-step5-specific-address']").ClickAsync(); await step.Locator("input[name='order-step5-recipient']").FillAsync("Chrome Receiver"); await step.Locator("input[name='order-step5-country']").FillAsync("IR"); await step.Locator("textarea[name='order-step5-address']").FillAsync("Deployed acceptance address"); }
        ILocator method=step.Locator("input[name='order-step5-shipping-method']"); await method.ClickAsync();
        ILocator methodOptions=step.Locator("[role=listbox] [role=option]"); await Assertions.Expect(methodOptions).Not.ToHaveCountAsync(0); await methodOptions.First.ClickAsync();
        await step.GetByRole(AriaRole.Button,new(){Name="Save draft",Exact=true}).ClickAsync();
        await Assertions.Expect(step).ToBeVisibleAsync(); await Assertions.Expect(step.Locator("input[name='order-step5-recipient']")).Not.ToHaveValueAsync("");
        await step.GetByRole(AriaRole.Button,new(){Name="Back",Exact=true}).ClickAsync(); await Assertions.Expect(page.Locator(".order-services-step")).ToBeVisibleAsync();
        await page.Locator(".order-services-step").GetByRole(AriaRole.Button,new(){Name="Next",Exact=true}).ClickAsync(); await Assertions.Expect(step).ToBeVisibleAsync();
        await page.ReloadAsync(new(){WaitUntil=WaitUntilState.DOMContentLoaded});
        await Assertions.Expect(page.Locator(".order-shipping-step")).ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator(".wizard-stepper")).ToBeVisibleAsync();
    }
    [Fact]
    public async Task OrderStepSixCalculatesAuthoritativePricingAndSupportsRecalculation(){
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Forders%2Fnew",new(){WaitUntil=WaitUntilState.DOMContentLoaded});
        await page.GotoAsync(baseUrl+"/orders/new",new(){WaitUntil=WaitUntilState.DOMContentLoaded});
        await SelectFirstOption("order-branch");
        await SelectFirstOption("order-customer");
        await page.GetByText("Next",new(){Exact=true}).ClickAsync();
        await page.Locator("input[name='order-item-product']").ClickAsync(); await page.Locator(".order-items-step [role=option]").First.ClickAsync();
        await page.Locator("input[name='order-item-sku']").ClickAsync(); await page.Locator(".order-items-step [role=option]").First.ClickAsync();
        await page.Locator("input[name='order-item-quantity']").FillAsync("2"); await page.GetByText("Add item",new(){Exact=true}).ClickAsync();
        await page.Locator(".order-items-step").GetByRole(AriaRole.Button,new(){Name="Next",Exact=true}).ClickAsync();
        await page.Locator(".order-print-step-redesign").GetByRole(AriaRole.Button,new(){Name="Next",Exact=true}).ClickAsync();
        await Assertions.Expect(page.Locator(".order-services-step")).ToBeVisibleAsync();
        await page.Locator(".order-services-step").GetByRole(AriaRole.Button,new(){Name="Next",Exact=true}).ClickAsync();
        ILocator step=page.Locator(".order-shipping-step"); await Assertions.Expect(step).ToBeVisibleAsync();
        ILocator addresses=step.Locator("button[name='order-step5-address']");
        if(await addresses.CountAsync()>0) await addresses.First.ClickAsync();
        else { await step.Locator("button[name='order-step5-specific-address']").ClickAsync(); await step.Locator("input[name='order-step5-recipient']").FillAsync("Pricing Receiver"); await step.Locator("input[name='order-step5-country']").FillAsync("IR"); await step.Locator("textarea[name='order-step5-address']").FillAsync("Pricing acceptance address"); }
        ILocator method=step.GetByRole(AriaRole.Combobox,new(){Name="Shipping method *",Exact=true}); await method.FillAsync("standard"); ILocator options=step.Locator(".searchable-select-list [role=option]"); await Assertions.Expect(step.Locator(".searchable-select-list")).ToBeVisibleAsync(); Assert.True(await options.CountAsync()>0); await options.First.ClickAsync();
        await step.GetByRole(AriaRole.Button,new(){Name="Next",Exact=true}).ClickAsync();
        ILocator pricing=page.Locator(".order-pricing-step"); await Assertions.Expect(pricing).ToBeVisibleAsync(); await Assertions.Expect(pricing.GetByRole(AriaRole.Heading,new(){Name="Order Pricing",Exact=true})).ToBeVisibleAsync();
        await Assertions.Expect(pricing.Locator(".pricing-final")).ToBeVisibleAsync(); await Assertions.Expect(pricing.GetByRole(AriaRole.Button,new(){Name="Recalculate",Exact=true})).ToBeVisibleAsync();
        await pricing.GetByRole(AriaRole.Button,new(){Name="Recalculate",Exact=true}).ClickAsync(); await Assertions.Expect(pricing.Locator(".loading-state")).ToHaveCountAsync(0); await Assertions.Expect(pricing.Locator(".pricing-summary-grid")).ToBeVisibleAsync();
    }
    [Fact]
    public async Task OrderStepSevenReviewsAndFinalizesWithoutStartingWorkflow(){
        await page.GotoAsync(baseUrl+"/culture?culture=en&returnUrl=%2Forders%2Fnew",new(){WaitUntil=WaitUntilState.DOMContentLoaded}); await page.GotoAsync(baseUrl+"/orders/new",new(){WaitUntil=WaitUntilState.DOMContentLoaded});
        await SelectFirstOption("order-branch"); await SelectFirstOption("order-customer"); await page.GetByText("Next",new(){Exact=true}).ClickAsync();
        await page.Locator("input[name='order-item-product']").ClickAsync(); await page.Locator(".order-items-step [role=option]").First.ClickAsync(); await page.Locator("input[name='order-item-sku']").ClickAsync(); await page.Locator(".order-items-step [role=option]").First.ClickAsync(); await page.Locator("input[name='order-item-quantity']").FillAsync("1"); await page.GetByText("Add item",new(){Exact=true}).ClickAsync(); await page.Locator(".order-items-step").GetByRole(AriaRole.Button,new(){Name="Next",Exact=true}).ClickAsync();
        await page.Locator(".order-print-step-redesign").GetByRole(AriaRole.Button,new(){Name="Next",Exact=true}).ClickAsync(); await page.Locator(".order-services-step").GetByRole(AriaRole.Button,new(){Name="Next",Exact=true}).ClickAsync(); ILocator shipping=page.Locator(".order-shipping-step");
        ILocator addresses=shipping.Locator("button[name='order-step5-address']"); if(await addresses.CountAsync()>0) await addresses.First.ClickAsync(); else { await shipping.Locator("button[name='order-step5-specific-address']").ClickAsync(); await shipping.Locator("input[name='order-step5-recipient']").FillAsync("Final Receiver"); await shipping.Locator("input[name='order-step5-country']").FillAsync("IR"); await shipping.Locator("textarea[name='order-step5-address']").FillAsync("Finalization acceptance address"); }
        ILocator method=shipping.GetByRole(AriaRole.Combobox,new(){Name="Shipping method *",Exact=true}); await method.FillAsync("standard"); ILocator options=shipping.Locator(".searchable-select-list [role=option]"); await Assertions.Expect(options).Not.ToHaveCountAsync(0); await options.First.ClickAsync(); await shipping.GetByRole(AriaRole.Button,new(){Name="Next",Exact=true}).ClickAsync();
        ILocator pricing=page.Locator(".order-pricing-step"); await Assertions.Expect(pricing.Locator(".pricing-summary-grid")).ToBeVisibleAsync(); await pricing.GetByRole(AriaRole.Button,new(){Name="Next",Exact=true}).ClickAsync();
        ILocator review=page.Locator(".order-review-step"); await Assertions.Expect(review).ToBeVisibleAsync(); await Assertions.Expect(review.GetByRole(AriaRole.Heading,new(){Name="Review & Finalize",Exact=true})).ToBeVisibleAsync(); await Assertions.Expect(review.GetByText("This draft does not have a final order number yet.",new(){Exact=true})).ToBeVisibleAsync();
        await review.GetByRole(AriaRole.Button,new(){Name="Finalize order",Exact=true}).ClickAsync(); await Assertions.Expect(review.GetByRole(AriaRole.Button,new(){Name="Confirm finalization",Exact=true})).ToBeVisibleAsync(); await review.GetByRole(AriaRole.Button,new(){Name="Confirm finalization",Exact=true}).ClickAsync(); await page.WaitForURLAsync(url=>url.Contains("/360")); await Assertions.Expect(page.GetByText("ORD-",new(){Exact=false}).First).ToBeVisibleAsync();
    }
    private async Task SelectFirstOption(string name){var input=page.Locator($"input[name='{name}']");await input.ClickAsync();var options=input.Locator("xpath=..").Locator("[role=option]");try{await Assertions.Expect(options).Not.ToHaveCountAsync(0);}catch(Exception ex){throw new Xunit.Sdk.XunitException($"No options for {name}; input={await input.EvaluateAsync<string>("el=>el.outerHTML")}; host={await input.Locator("xpath=..").EvaluateAsync<string>("el=>el.outerHTML")}; url={page.Url}",ex);}await options.First.ClickAsync();await Assertions.Expect(input).Not.ToHaveValueAsync("");}
    public async ValueTask DisposeAsync(){await context.DisposeAsync();await browser.DisposeAsync();playwright.Dispose();}
}
