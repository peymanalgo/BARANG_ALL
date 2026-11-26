using Microsoft.Playwright;

string baseUrl=Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL")??"http://localhost:5100";
string browserPath=Environment.GetEnvironmentVariable("BARANG_E2E_BROWSER_PATH")??@"C:\Program Files\Google\Chrome\Application\chrome.exe";
string username=Environment.GetEnvironmentVariable("BARANG_E2E_USERNAME")??"admin";
string password=Environment.GetEnvironmentVariable("BARANG_E2E_PASSWORD")??"BarangDev@2026";
using IPlaywright pw=await Playwright.CreateAsync();
await using IBrowser browser=await pw.Chromium.LaunchAsync(new(){Headless=true,ExecutablePath=browserPath});
await using IBrowserContext context=await browser.NewContextAsync(new(){Locale="en-US",ViewportSize=new(){Width=1440,Height=1000}});
IPage page=await context.NewPageAsync();
await page.GotoAsync($"{baseUrl}/login",new(){WaitUntil=WaitUntilState.NetworkIdle});
await page.Locator("#username").FillAsync(username);await page.Locator("#password").FillAsync(password);await page.Locator("button[type=submit]").ClickAsync();await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
if(new Uri(page.Url).AbsolutePath=="/company"){ILocator choice=page.Locator(".company-list form button[type=submit]");if(await choice.CountAsync()==0)throw new InvalidOperationException("No company choice available");await choice.First.ClickAsync();await page.WaitForLoadStateAsync(LoadState.NetworkIdle);}
await page.GotoAsync($"{baseUrl}/workspace/logistics/returns",new(){WaitUntil=WaitUntilState.NetworkIdle});
if(await page.Locator(".page-heading h1").CountAsync()!=1)throw new InvalidOperationException($"Logistics return page heading missing url={page.Url} body={await page.Locator("body").InnerTextAsync()}");
if(await page.Locator("button").CountAsync()<2)throw new InvalidOperationException("Return actions missing");
int inputCount=await page.Locator("input").CountAsync();if(inputCount<7)throw new InvalidOperationException($"Return form fields missing: {inputCount}; url={page.Url}; text={await page.Locator("body").InnerTextAsync()}");
if(await page.Locator(".inline-error").CountAsync()!=0)throw new InvalidOperationException("Return page displayed an error");
Directory.CreateDirectory(".artifacts/step11-browser");await page.ScreenshotAsync(new(){Path=".artifacts/step11-browser/logistics-return-workspace.png",FullPage=true});
IReadOnlyList<string> buttons=await page.Locator("button").AllTextContentsAsync();
Console.WriteLine($"PASS logistics-return-ui url={page.Url} inputs={inputCount} buttons={string.Join("|",buttons.Select(x=>x.Trim()).Where(x=>x.Length>0))}");
string? returnShipment=Environment.GetEnvironmentVariable("BARANG_E2E_RETURN_SHIPMENT_ID");
if(!string.IsNullOrWhiteSpace(returnShipment))
{
    string returnItem=Environment.GetEnvironmentVariable("BARANG_E2E_RETURN_ITEM_ID")??throw new InvalidOperationException("BARANG_E2E_RETURN_ITEM_ID is required");
    string returnQuantity=Environment.GetEnvironmentVariable("BARANG_E2E_RETURN_QUANTITY")??"1";
    string returnKey=Environment.GetEnvironmentVariable("BARANG_E2E_RETURN_KEY")??$"UI-RETURN-{Guid.NewGuid():N}";
    string[] values={returnShipment,returnItem,returnQuantity,"PCS","CUSTOMER_RETURN","UI acceptance",returnKey};
    ILocator formInputs = page.Locator("input:not([type=hidden])");
    if(await formInputs.CountAsync() < values.Length) throw new InvalidOperationException($"Return form visible fields missing: {await formInputs.CountAsync()}; url={page.Url}");
    for(int i=0;i<values.Length;i++)await formInputs.Nth(i).FillAsync(values[i]);
    await page.GetByRole(AriaRole.Button,new(){Name="ذخیره",Exact=true}).ClickAsync();
    bool expectedFailure=string.Equals(Environment.GetEnvironmentVariable("BARANG_E2E_RETURN_EXPECTED"),"failure",StringComparison.OrdinalIgnoreCase);
    ILocator outcome=expectedFailure?page.Locator(".inline-error"):page.Locator(".inline-success");
    await outcome.WaitForAsync(new(){State=WaitForSelectorState.Visible,Timeout=10000});
    Console.WriteLine($"PASS return-form-action expected={(expectedFailure?"rejection":"success")} shipment={returnShipment} item={returnItem}");
}
string? orderId=Environment.GetEnvironmentVariable("BARANG_E2E_ORDER_ID");
if(!string.IsNullOrWhiteSpace(orderId))
{
    await page.GotoAsync($"{baseUrl}/orders/{orderId}/360",new(){WaitUntil=WaitUntilState.NetworkIdle});
    if(await page.Locator("#order360-title").CountAsync()!=1)throw new InvalidOperationException($"Order 360 missing url={page.Url} body={await page.Locator("body").InnerTextAsync()}");
    await page.GetByRole(AriaRole.Button,new(){Name="مشاهده کامل جزئیات سفارش"}).ClickAsync();
    try{await page.Locator(".order360-specification-modal").WaitForAsync(new(){State=WaitForSelectorState.Visible,Timeout=10000});}catch{await page.ScreenshotAsync(new(){Path=".artifacts/step11-browser/order360-return-failure.png",FullPage=true});throw new InvalidOperationException($"Order 360 specification modal missing body={await page.Locator("body").InnerTextAsync()}");}
    string body=await page.Locator("body").InnerTextAsync();
    if(!body.Contains("مرجوع",StringComparison.OrdinalIgnoreCase)&&!body.Contains("Returns",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Order 360 return section missing");
    await page.ScreenshotAsync(new(){Path=".artifacts/step11-browser/order360-return-status.png",FullPage=true});
    Console.WriteLine($"PASS order360-return-status url={page.Url} order={orderId}");
}
