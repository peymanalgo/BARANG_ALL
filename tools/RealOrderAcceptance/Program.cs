using Microsoft.Playwright;

const string baseUrl = "http://localhost:5100";
const string company = "42f0e459-6095-45bb-aa13-f38126d91c9d";
string userPassword = Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD") ?? "BarangDev@2026";
string userName = Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME") ?? "admin";
using IPlaywright pw = await Playwright.CreateAsync();
await using IBrowser browser = await pw.Chromium.LaunchAsync(new() { Headless = true, ExecutablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe" });
await using IBrowserContext context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 1000 }, Locale = "en-US" });
IPage page = await context.NewPageAsync();
string? processOrder = Environment.GetEnvironmentVariable("BARANG_PROCESS_ORDER_ID");
if (Guid.TryParse(processOrder, out _)) { await ProcessCartable(page, processOrder!, browser); return; }
if (Environment.GetEnvironmentVariable("BARANG_DELIVER_TO_DESIGN") == "1") { await DeliverToDesign(page); return; }
if (Environment.GetEnvironmentVariable("BARANG_VERIFY_DESIGN") == "1") { await VerifyDesign(page); return; }
if (Environment.GetEnvironmentVariable("BARANG_VERIFY_NEXT") == "1") { await VerifyNext(page); return; }
if (Environment.GetEnvironmentVariable("BARANG_REJECT_WAREHOUSE") == "1") { await RejectWarehouse(page); return; }
if (Environment.GetEnvironmentVariable("BARANG_VERIFY_REFERRALS") == "1") { await VerifyReferrals(page); return; }
if (Environment.GetEnvironmentVariable("BARANG_AUDIT_FLOW") == "1") { await AuditFlow(page); return; }
if (Environment.GetEnvironmentVariable("BARANG_COMPLETE_REMAINING") == "1") { await CompleteRemaining(page); return; }
if (Environment.GetEnvironmentVariable("BARANG_VERIFY_FIRST") == "1") { await VerifyFirstTask(page); return; }
if (Environment.GetEnvironmentVariable("BARANG_SMOKE") == "1")
{
    await Login(page, userName, userPassword);
    var smokeOrder = Environment.GetEnvironmentVariable("BARANG_SMOKE_ORDER_ID") ?? "5ce2153f-4f99-43e3-9aa3-1807eda89156";
    await page.GotoAsync($"{baseUrl}/orders/{smokeOrder}/edit", new() { WaitUntil = WaitUntilState.NetworkIdle });
    Console.WriteLine("CHROME_EDIT_BUTTONS " + string.Join(" | ", await page.GetByRole(AriaRole.Button).AllTextContentsAsync()));
    await page.GotoAsync($"{baseUrl}/cartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
    Console.WriteLine("CHROME_CARTABLE_URL " + page.Url);
    Console.WriteLine("CHROME_NAV " + (await page.Locator("nav.sidebar-navigation").InnerTextAsync()).ReplaceLineEndings(" | "));
    Console.WriteLine("CHROME_CARTABLE_TABS " + string.Join(" | ", await page.Locator(".cartable-tabs button").AllTextContentsAsync()));
    Console.WriteLine("CHROME_CARTABLE_SUMMARY_CARDS " + await page.Locator(".cartable-summary").CountAsync());
    var referralsTab = page.Locator(".cartable-tabs button").Nth(1);
    if (await referralsTab.CountAsync() > 0) { await referralsTab.ClickAsync(); await page.WaitForTimeoutAsync(300); Console.WriteLine("CHROME_REFERRALS " + string.Join(" | ", await page.Locator(".cartable-tabs button").AllTextContentsAsync())); Console.WriteLine("CHROME_REFERRAL_ROWS " + string.Join(" || ", await page.Locator(".cartable-standard-grid tbody tr").AllTextContentsAsync())); }
    Console.WriteLine("CHROME_CARTABLE_GRID_ONLY " + await page.Locator(".cartable-standard-grid").CountAsync() + "|detail=" + await page.Locator(".cartable-detail-page").CountAsync() + "|reader=" + await page.Locator(".cartable-reader").CountAsync());
    var referralEye = page.Locator(".cartable-standard-grid tbody tr").First.Locator("button").First;
    if (await referralEye.CountAsync() > 0) { await referralEye.ClickAsync(); await page.WaitForTimeoutAsync(300); Console.WriteLine("CHROME_DETAIL_ROUTE " + page.Url + "|actions=" + await page.Locator(".cartable-actions-panel").CountAsync() + "|messages=" + await page.Locator(".cartable-messages-panel").CountAsync()); }
    await page.GotoAsync($"{baseUrl}/workspace/orders", new() { WaitUntil = WaitUntilState.NetworkIdle });
    var gridOrderNumber = Environment.GetEnvironmentVariable("BARANG_GRID_ORDER_NUMBER") ?? "ORD-2026-0000000025";
    var orderRow = page.Locator("[data-orders-grid] tbody tr").Filter(new() { HasText = gridOrderNumber }).First;
    Console.WriteLine("CHROME_ORDER_GRID_STATUS " + (await orderRow.CountAsync() > 0 ? await orderRow.InnerTextAsync() : "not-visible"));
    foreach (var culture in new[] { "en", "ar" }) { await page.GotoAsync($"{baseUrl}/culture?culture={culture}&returnUrl=%2Fcartable", new() { WaitUntil = WaitUntilState.NetworkIdle }); await page.GotoAsync($"{baseUrl}/cartable", new() { WaitUntil = WaitUntilState.NetworkIdle }); Console.WriteLine($"CHROME_{culture.ToUpperInvariant()}_CARTABLE " + string.Join(" | ", await page.Locator(".cartable-tabs button").AllTextContentsAsync())); }
    await page.GotoAsync($"{baseUrl}/orders/{smokeOrder}/360", new() { WaitUntil = WaitUntilState.NetworkIdle });
    var timelineTab = page.GetByRole(AriaRole.Button, new() { Name = "خط زمانی", Exact = true });
    if (await timelineTab.CountAsync() > 0) { await timelineTab.ClickAsync(); await page.WaitForTimeoutAsync(300); }
    Console.WriteLine("CHROME_360 " + await page.Locator("body").InnerTextAsync());
    return;
}
await Login(page, userName, userPassword);
string? existingOrder = Environment.GetEnvironmentVariable("BARANG_ACCEPTANCE_ORDER_ID");
if (Guid.TryParse(existingOrder, out _))
{
    await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Forders%2F{existingOrder}%2Fedit", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.GotoAsync($"{baseUrl}/orders/{existingOrder}/edit", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).First.ClickAsync();
    await page.Locator(".order-items-step").GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
}
else
{
    await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Forders%2Fnew", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.GotoAsync($"{baseUrl}/orders/new", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await ChooseFirst(page, "input[name='order-branch']");
    await ChooseFirst(page, "input[name='order-customer']");
    await page.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).First.ClickAsync();
    await ChooseFirst(page, "input[name='order-item-product']");
    await ChooseFirst(page, "input[name='order-item-sku']");
    await page.Locator("input[name='order-item-quantity']").FillAsync("1");
    await page.GetByRole(AriaRole.Button, new() { Name = "Add item", Exact = true }).ClickAsync();
    await page.WaitForTimeoutAsync(600);
    await page.Locator(".order-items-step").GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
    await page.WaitForTimeoutAsync(800);
}
if (await page.Locator(".order-item-context").CountAsync() > 0 && await page.Locator("input[name='print-width']").CountAsync() == 0) await page.Locator(".order-item-context").First.ClickAsync();
await page.Locator("input[name='print-width']").FillAsync("10");
await page.Locator("input[name='print-height']").FillAsync("12");
await page.Locator("input[type='file']").SetInputFilesAsync(new FilePayload { Name = "acceptance-artwork.png", MimeType = "image/png", Buffer = new byte[] { 137,80,78,71,13,10,26,10,0,0,0,0 }.Concat(Guid.NewGuid().ToByteArray()).ToArray() });
await page.Locator(".print-operation-editor").GetByRole(AriaRole.Button, new() { Name = "Preview", Exact = true }).ClickAsync();
await page.GetByRole(AriaRole.Button, new() { Name = "Close", Exact = true }).Last.ClickAsync();
await page.Locator(".print-operation-editor .primary-link").ClickAsync();
await page.Locator(".order-print-step").GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
await page.Locator(".order-services-step").GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
var shipping = page.Locator(".order-shipping-step");
await shipping.Locator("button[name='order-step5-specific-address']").ClickAsync();
await shipping.Locator("input[name='order-step5-recipient']").FillAsync("Acceptance Receiver");
await shipping.Locator("input[name='order-step5-country']").FillAsync("IR");
await shipping.Locator("input[name='order-step5-province']").FillAsync("Tehran");
await shipping.Locator("input[name='order-step5-city']").FillAsync("Tehran");
await shipping.Locator("textarea[name='order-step5-address']").FillAsync("Tehran, Acceptance Avenue 1");
await shipping.Locator("input[name='order-step5-shipping-method']").ClickAsync();
var shippingOptions = shipping.Locator("[role='option']");
for (var wait = 0; wait < 20 && await shippingOptions.CountAsync() == 0; wait++) await page.WaitForTimeoutAsync(250);
Console.WriteLine("SHIPPING_OPTIONS " + await shippingOptions.CountAsync());
await shippingOptions.Last.ClickAsync();
await page.WaitForTimeoutAsync(400);
Console.WriteLine("SHIPPING_VALUES " + await shipping.Locator("input[name='order-step5-recipient']").InputValueAsync() + "|" + await shipping.Locator("input[name='order-step5-country']").InputValueAsync() + "|" + await shipping.Locator("textarea[name='order-step5-address']").InputValueAsync() + "|" + await shipping.Locator("input[name='order-step5-shipping-method']").InputValueAsync());
await shipping.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
await page.WaitForTimeoutAsync(1000);
if (await page.Locator(".order-pricing-step").CountAsync() == 0) { Console.WriteLine("SHIPPING_BLOCKED " + await shipping.InnerTextAsync()); return; }
var pricing = page.Locator(".order-pricing-step");
await pricing.GetByRole(AriaRole.Button, new() { Name = "Recalculate", Exact = true }).ClickAsync();
await page.WaitForTimeoutAsync(1200);
await Assertions.Expect(pricing.Locator(".pricing-final strong")).Not.ToHaveTextAsync("0.00");
await pricing.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true }).ClickAsync();
var review = page.Locator(".order-review-step");
await review.GetByRole(AriaRole.Button, new() { Name = "Finalize order", Exact = true }).ClickAsync();
await review.GetByRole(AriaRole.Button, new() { Name = "Confirm finalization", Exact = true }).ClickAsync();
await page.WaitForURLAsync("**/360", new() { WaitUntil = WaitUntilState.NetworkIdle });
Console.WriteLine("ORDER_CREATED " + await page.Locator(".order360-head .eyebrow").InnerTextAsync());

static async Task ChooseFirst(IPage page, string selector)
{
    await page.Locator(selector).ClickAsync();
    await page.Locator("[role='option']").First.ClickAsync();
}
static async Task Login(IPage page, string user, string password)
{
    await page.GotoAsync("http://localhost:5100/login", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.Locator("#username").FillAsync(user); await page.Locator("#password").FillAsync(password);
    await page.Locator("button[type=submit]").ClickAsync(); await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    if (new Uri(page.Url).AbsolutePath == "/company") await page.Locator($".company-list form input[name='companyId'][value='{company}']").Locator(".. ").GetByRole(AriaRole.Button).ClickAsync();
}

static async Task ProcessCartable(IPage page, string orderId, IBrowser browser)
{
    bool rejected = false, audited = false;
    for (int round = 0; round < 40; round++)
    {
        bool acted = false;
        for (int index = 1; index <= 11; index++)
        {
            await page.Context.ClearCookiesAsync();
            await Login(page, $"User{index}", "Admin@123");
            await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Fcartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.GotoAsync($"{baseUrl}/cartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
            var rows = page.Locator(".cartable-mail-row");
            var targetNumber = Environment.GetEnvironmentVariable("BARANG_ACCEPTANCE_ORDER_NUMBER");
            var row = rows.Filter(new() { HasText = string.IsNullOrWhiteSpace(targetNumber) ? "ORD-" : targetNumber }).First;
            if (await row.CountAsync() == 0) continue;
            await row.ClickAsync();
            await page.Locator(".cartable-reader-inner").WaitForAsync();
            var claim = page.GetByRole(AriaRole.Button, new() { Name = "Claim", Exact = true });
            if (await claim.CountAsync() > 0) { await claim.ClickAsync(); await page.WaitForTimeoutAsync(500); }
            var outcomeButtons = page.Locator(".cartable-outcomes button");
            if (await outcomeButtons.CountAsync() == 0) continue;
            string stage = await page.Locator(".cartable-order-topline p").InnerTextAsync();
            string action = "Approve";
            if (!rejected && !stage.Contains("Admin", StringComparison.OrdinalIgnoreCase) && await page.GetByRole(AriaRole.Button, new() { Name = "Reject", Exact = true }).CountAsync() > 0) { action = "Reject"; }
            else if (!audited && !stage.Contains("Admin", StringComparison.OrdinalIgnoreCase) && await page.GetByRole(AriaRole.Button, new() { Name = "Audit reject", Exact = true }).CountAsync() > 0) { action = "Audit Reject"; }
            var selected = action == "Audit Reject"
                ? page.GetByRole(AriaRole.Button, new() { Name = "Audit reject", Exact = true })
                : page.GetByRole(AriaRole.Button, new() { Name = action, Exact = true });
            if (await selected.CountAsync() == 0) selected = outcomeButtons.First;
            if (action != "Approve") await page.Locator("textarea").FillAsync(action == "Reject" ? "Acceptance return for pricing verification" : "Acceptance audit review required");
            await selected.ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Submit decision", Exact = true }).ClickAsync();
            await page.WaitForTimeoutAsync(700);
            if (action == "Reject") rejected = true;
            if (action == "Audit Reject") audited = true;
            Console.WriteLine($"CARTABLE_ACTION User{index} {stage} {action}"); acted = true; break;
        }
        if (!acted) break;
    }
}

static async Task DeliverToDesign(IPage page)
{
    string orderNumber = Environment.GetEnvironmentVariable("BARANG_DELIVER_ORDER_NUMBER") ?? throw new InvalidOperationException("BARANG_DELIVER_ORDER_NUMBER is required");
    foreach (var actor in new[] { (User: "User4", Stage: "ADMIN"), (User: "User11", Stage: "FINANCE"), (User: "User7", Stage: "PRODUCTION") })
    {
        await page.Context.ClearCookiesAsync();
        await Login(page, actor.User, "Admin@123");
        await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Fcartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GotoAsync($"{baseUrl}/cartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
        var row = page.Locator(".cartable-standard-grid tbody tr").Filter(new() { HasText = orderNumber }).First;
        await row.WaitForAsync();
        Console.WriteLine($"PRE_DESIGN_RECEIVED {actor.User} {actor.Stage} {await row.InnerTextAsync()}");
        await row.Locator("button").First.ClickAsync();
        await page.WaitForURLAsync("**/cartable/task/*", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(1200);
        await page.Locator(".cartable-detail-page").WaitForAsync();
        var claim = page.GetByRole(AriaRole.Button, new() { Name = "Claim", Exact = true });
        if (await claim.CountAsync() > 0) { await claim.ClickAsync(); await page.WaitForTimeoutAsync(500); }
        var approve = page.GetByRole(AriaRole.Button, new() { Name = "Approve", Exact = true });
        if (await approve.CountAsync() == 0) { Console.WriteLine($"DETAIL_BUTTONS {string.Join(" | ", await page.GetByRole(AriaRole.Button).AllTextContentsAsync())}"); Console.WriteLine("DETAIL_BODY " + (await page.Locator("body").InnerTextAsync()).ReplaceLineEndings(" | ")); }
        await approve.ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Confirm", Exact = true }).ClickAsync();
        await page.WaitForTimeoutAsync(900);
        Console.WriteLine($"PRE_DESIGN_APPROVED {actor.User} {actor.Stage}");
    }

    await page.Context.ClearCookiesAsync();
    await Login(page, "User1", "Admin@123");
    await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Fcartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.GotoAsync($"{baseUrl}/cartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
    var designRow = page.Locator(".cartable-standard-grid tbody tr").Filter(new() { HasText = orderNumber }).First;
    await designRow.WaitForAsync();
    Console.WriteLine($"USER1_DESIGN_RECEIVED {await designRow.InnerTextAsync()}|eye={await designRow.Locator("button").CountAsync()}");
}

static async Task VerifyDesign(IPage page)
{
    string orderNumber = Environment.GetEnvironmentVariable("BARANG_DELIVER_ORDER_NUMBER") ?? throw new InvalidOperationException("BARANG_DELIVER_ORDER_NUMBER is required");
    await Login(page, "User1", "Admin@123");
    await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Fcartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.GotoAsync($"{baseUrl}/cartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.WaitForTimeoutAsync(1000);
    Console.WriteLine("USER1_TABS " + string.Join(" | ", await page.Locator(".cartable-tabs button").AllTextContentsAsync()));
    var row = page.Locator(".cartable-standard-grid tbody tr").Filter(new() { HasText = orderNumber }).First;
    await row.WaitForAsync();
    Console.WriteLine($"USER1_RECEIVED_FINAL {await row.InnerTextAsync()}|eye={await row.Locator("button").CountAsync()}|detail={await page.Locator(".cartable-detail-page").CountAsync()}");
}

static async Task VerifyNext(IPage page)
{
    string orderNumber = Environment.GetEnvironmentVariable("BARANG_DELIVER_ORDER_NUMBER") ?? throw new InvalidOperationException("BARANG_DELIVER_ORDER_NUMBER is required");
    string username = Environment.GetEnvironmentVariable("BARANG_NEXT_USERNAME") ?? "User5";
    await Login(page, username, "Admin@123");
    await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Fcartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.GotoAsync($"{baseUrl}/cartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.WaitForTimeoutAsync(1000);
    Console.WriteLine("NEXT_TABS " + string.Join(" | ", await page.Locator(".cartable-tabs button").AllTextContentsAsync()));
    var row = page.Locator(".cartable-standard-grid tbody tr").Filter(new() { HasText = orderNumber }).First;
    await row.WaitForAsync();
    Console.WriteLine($"NEXT_RECEIVED {await row.InnerTextAsync()}|eye={await row.Locator("button").CountAsync()}|detail={await page.Locator(".cartable-detail-page").CountAsync()}");
    if (Environment.GetEnvironmentVariable("BARANG_OPEN_EYE") == "1")
    {
        await row.Locator("button").First.ClickAsync();
        await page.WaitForURLAsync("**/cartable/task/*", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator(".cartable-detail-page").WaitForAsync();
        Console.WriteLine("NEXT_DETAIL actions=" + await page.Locator(".cartable-actions-panel").CountAsync() + "|360=" + await page.Locator(".cartable-360-panel .order360-shell").CountAsync() + "|messages=" + await page.Locator(".cartable-messages-panel").CountAsync());
        Console.WriteLine("NEXT_MESSAGES " + (await page.Locator(".message-history").InnerTextAsync()).ReplaceLineEndings(" | "));
    }
}

static async Task VerifyReferrals(IPage page)
{
    string orderNumber = Environment.GetEnvironmentVariable("BARANG_DELIVER_ORDER_NUMBER") ?? throw new InvalidOperationException("BARANG_DELIVER_ORDER_NUMBER is required");
    string username = Environment.GetEnvironmentVariable("BARANG_NEXT_USERNAME") ?? "User5";
    await Login(page, username, "Admin@123");
    await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Fcartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.GotoAsync($"{baseUrl}/cartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.WaitForTimeoutAsync(1000);
    Console.WriteLine("REFERRAL_TABS " + string.Join(" | ", await page.Locator(".cartable-tabs button").AllTextContentsAsync()));
    await page.Locator(".cartable-tabs button").Nth(1).ClickAsync();
    await page.WaitForTimeoutAsync(300);
    var row = page.Locator(".cartable-standard-grid tbody tr").Filter(new() { HasText = orderNumber }).First;
    await row.WaitForAsync();
    Console.WriteLine("REFERRAL_ROW " + await row.InnerTextAsync() + "|eye=" + await row.Locator("button").CountAsync());
}

static async Task AuditFlow(IPage page)
{
    string orderNumber = Environment.GetEnvironmentVariable("BARANG_DELIVER_ORDER_NUMBER") ?? throw new InvalidOperationException("BARANG_DELIVER_ORDER_NUMBER is required");

    await Login(page, "User1", "Admin@123");
    await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Fcartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.GotoAsync($"{baseUrl}/cartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
    var designRow = page.Locator(".cartable-standard-grid tbody tr").Filter(new() { HasText = orderNumber }).First;
    await designRow.WaitForAsync();
    Console.WriteLine("RETURNED_DESIGN_ROW " + await designRow.InnerTextAsync());
    await designRow.Locator("button").First.ClickAsync();
    await page.WaitForURLAsync("**/cartable/task/*", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.Locator(".cartable-detail-page").WaitForAsync();
    Console.WriteLine("RETURNED_DESIGN_HISTORY " + (await page.Locator(".message-history").InnerTextAsync()).ReplaceLineEndings(" | "));
    var claim = page.GetByRole(AriaRole.Button, new() { Name = "Claim", Exact = true });
    if (await claim.CountAsync() > 0) { await claim.ClickAsync(); await page.WaitForTimeoutAsync(500); }
    await page.GetByRole(AriaRole.Button, new() { Name = "Approve", Exact = true }).ClickAsync();
    await page.GetByRole(AriaRole.Button, new() { Name = "Confirm", Exact = true }).ClickAsync();
    await page.WaitForTimeoutAsync(900);
    Console.WriteLine("RETURNED_DESIGN_APPROVED");

    await page.Context.ClearCookiesAsync();
    await Login(page, "User5", "Admin@123");
    await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Fcartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.GotoAsync($"{baseUrl}/cartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
    var warehouseRow = page.Locator(".cartable-standard-grid tbody tr").Filter(new() { HasText = orderNumber }).First;
    await warehouseRow.WaitForAsync();
    Console.WriteLine("AUDIT_STAGE_ROW " + await warehouseRow.InnerTextAsync());
    await warehouseRow.Locator("button").First.ClickAsync();
    await page.WaitForURLAsync("**/cartable/task/*", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.Locator(".cartable-detail-page").WaitForAsync();
    Console.WriteLine("AUDIT_STAGE_BUTTONS " + string.Join(" | ", await page.GetByRole(AriaRole.Button).AllTextContentsAsync()));
    Console.WriteLine("AUDIT_STAGE_HISTORY " + (await page.Locator(".message-history").InnerTextAsync()).ReplaceLineEndings(" | "));
    var stageClaim = page.GetByRole(AriaRole.Button, new() { Name = "Claim", Exact = true });
    if (await stageClaim.CountAsync() > 0) { await stageClaim.ClickAsync(); await page.WaitForTimeoutAsync(500); }
    await page.Locator(".message-compose textarea").FillAsync("برای بررسی بیشتر به ممیزی ارجاع شد.");
    var addMessage = page.GetByRole(AriaRole.Button, new() { Name = "Add message", Exact = true });
    if (await addMessage.CountAsync() == 0) addMessage = page.GetByRole(AriaRole.Button, new() { Name = "Add Message", Exact = true });
    await addMessage.ClickAsync();
    await page.WaitForTimeoutAsync(400);
    await page.Locator(".cartable-actions-panel textarea").FillAsync("نیاز به بررسی ممیزی دارد.");
    await page.GetByRole(AriaRole.Button, new() { Name = "Audit Reject", Exact = true }).ClickAsync();
    await page.GetByRole(AriaRole.Button, new() { Name = "Confirm", Exact = true }).ClickAsync();
    await page.WaitForTimeoutAsync(1000);
    Console.WriteLine("AUDIT_REJECTED url=" + page.Url);
}

static async Task CompleteRemaining(IPage page)
{
    string orderNumber = Environment.GetEnvironmentVariable("BARANG_DELIVER_ORDER_NUMBER") ?? throw new InvalidOperationException("BARANG_DELIVER_ORDER_NUMBER is required");
    bool auditMessageAdded = false;
    for (int round = 0; round < 30; round++)
    {
        bool acted = false;
        for (int index = 1; index <= 15; index++)
        {
            string username = $"User{index}";
            await page.Context.ClearCookiesAsync();
            await Login(page, username, "Admin@123");
            await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Fcartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.GotoAsync($"{baseUrl}/cartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
            var row = page.Locator(".cartable-standard-grid tbody tr").Filter(new() { HasText = orderNumber }).First;
            if (await row.CountAsync() == 0) continue;
            Console.WriteLine($"REMAINING_RECEIVED {username} {await row.InnerTextAsync()}");
            await row.Locator("button").First.ClickAsync();
            await page.WaitForURLAsync("**/cartable/task/*", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.Locator(".cartable-detail-page").WaitForAsync();
            var stage = (await page.Locator(".cartable-detail-header p").InnerTextAsync()).Split('·')[0].Trim();
            var claim = page.GetByRole(AriaRole.Button, new() { Name = "Claim", Exact = true });
            if (await claim.CountAsync() > 0) { await claim.ClickAsync(); await page.WaitForTimeoutAsync(500); }
            if (!auditMessageAdded && username == "User3" && stage.Contains("Audit", StringComparison.OrdinalIgnoreCase))
            {
                await page.Locator(".message-compose textarea").FillAsync("ممیزی انجام شد و سفارش قابل ادامه است.");
                var addMessage = page.GetByRole(AriaRole.Button, new() { Name = "Add message", Exact = true });
                if (await addMessage.CountAsync() == 0) addMessage = page.GetByRole(AriaRole.Button, new() { Name = "Add Message", Exact = true });
                await addMessage.ClickAsync();
                await page.WaitForTimeoutAsync(400);
                auditMessageAdded = true;
            }
            var outcomes = page.Locator(".cartable-outcomes button");
            var labels = await outcomes.AllTextContentsAsync();
            Console.WriteLine($"REMAINING_OUTCOMES {username} {stage} {string.Join(" | ", labels)}");
            if (labels.Count == 0) throw new InvalidOperationException($"No outcomes available for {username} {stage}");
            ILocator selected = page.GetByRole(AriaRole.Button, new() { Name = "Approve", Exact = true });
            if (await selected.CountAsync() == 0)
            {
                selected = outcomes.Filter(new() { HasText = "Approve" }).First;
                if (await selected.CountAsync() == 0) throw new InvalidOperationException($"No positive outcome available for {username} {stage}: {string.Join(" | ", labels)}");
            }
            await selected.ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Confirm", Exact = true }).ClickAsync();
            await page.WaitForTimeoutAsync(900);
            Console.WriteLine($"REMAINING_COMPLETED {username} {stage}");
            acted = true;
            break;
        }
        if (!acted) break;
    }
}

static async Task RejectWarehouse(IPage page)
{
    string orderNumber = Environment.GetEnvironmentVariable("BARANG_DELIVER_ORDER_NUMBER") ?? throw new InvalidOperationException("BARANG_DELIVER_ORDER_NUMBER is required");
    await Login(page, "User5", "Admin@123");
    await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Fcartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.GotoAsync($"{baseUrl}/cartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
    var row = page.Locator(".cartable-standard-grid tbody tr").Filter(new() { HasText = orderNumber }).First;
    await row.WaitForAsync();
    Console.WriteLine("WAREHOUSE_ROW " + await row.InnerTextAsync());
    await row.Locator("button").First.ClickAsync();
    await page.WaitForURLAsync("**/cartable/task/*", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.Locator(".cartable-detail-page").WaitForAsync();
    await page.Locator(".cartable-360-panel .order360-shell").WaitForAsync();
    var claim = page.GetByRole(AriaRole.Button, new() { Name = "Claim", Exact = true });
    if (await claim.CountAsync() > 0) { await claim.ClickAsync(); await page.WaitForTimeoutAsync(500); }
    var messageBox = page.Locator(".message-compose textarea");
    await messageBox.FillAsync("نیاز به بازبینی مرحله طراحی دارد.");
    Console.WriteLine("WAREHOUSE_BUTTONS " + string.Join(" | ", await page.GetByRole(AriaRole.Button).AllTextContentsAsync()));
    var addMessage = page.GetByRole(AriaRole.Button, new() { Name = "Add message", Exact = true });
    if (await addMessage.CountAsync() == 0) addMessage = page.GetByRole(AriaRole.Button, new() { Name = "Add Message", Exact = true });
    await addMessage.ClickAsync();
    await page.WaitForTimeoutAsync(400);
    Console.WriteLine("WAREHOUSE_MESSAGE " + (await page.Locator(".message-history").InnerTextAsync()).ReplaceLineEndings(" | "));
    await page.Locator(".cartable-actions-panel textarea").FillAsync("اطلاعات طراحی نیاز به بازبینی دارد.");
    await page.GetByRole(AriaRole.Button, new() { Name = "Reject", Exact = true }).ClickAsync();
    await page.GetByRole(AriaRole.Button, new() { Name = "Confirm", Exact = true }).ClickAsync();
    await page.WaitForTimeoutAsync(1000);
    Console.WriteLine("WAREHOUSE_REJECTED url=" + page.Url);
}

static async Task VerifyFirstTask(IPage page)
{
    string orderNumber = Environment.GetEnvironmentVariable("BARANG_VERIFY_ORDER_NUMBER") ?? throw new InvalidOperationException("BARANG_VERIFY_ORDER_NUMBER is required");
    await Login(page, "User4", "Admin@123");
    await page.GotoAsync($"{baseUrl}/culture?culture=en&returnUrl=%2Fcartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.GotoAsync($"{baseUrl}/cartable", new() { WaitUntil = WaitUntilState.NetworkIdle });
    Console.WriteLine("FIRST_TABS " + string.Join(" | ", await page.Locator(".cartable-tabs button").AllTextContentsAsync()));
    var row = page.Locator(".cartable-standard-grid tbody tr").Filter(new() { HasText = orderNumber }).First;
    await row.WaitForAsync();
    Console.WriteLine("FIRST_RECEIVED " + await row.InnerTextAsync());
    await row.Locator("button").First.ClickAsync();
    await page.WaitForURLAsync("**/cartable/task/*", new() { WaitUntil = WaitUntilState.NetworkIdle });
    await page.Locator(".cartable-detail-page").WaitForAsync();
    await page.Locator(".cartable-360-panel .order360-shell").WaitForAsync();
    Console.WriteLine("FIRST_DETAIL actions=" + await page.Locator(".cartable-actions-panel").CountAsync() + "|360=" + await page.Locator(".cartable-360-panel .order360-shell").CountAsync() + "|messages=" + await page.Locator(".cartable-messages-panel").CountAsync() + "|claim=" + await page.GetByRole(AriaRole.Button, new() { Name = "Claim", Exact = true }).CountAsync());
}
