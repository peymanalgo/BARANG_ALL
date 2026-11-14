using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Correlation;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Customers;
using BARANG.Core.Application.Files;
using BARANG.Core.Application.Orders;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Security;
using BARANG.Core.Infrastructure.Context;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Api.Client;

// سرویس‌های ویزارد سفارش B2C:
//  - config/quote عمومی‌اند تا مهمان هم بتواند طرح‌وری بکند؛ سفارش‌سازی همچنان
//    فقط با توکن مشتری (client) انجام می‌شود.
//  - قیمت‌گذاری quote از همان تعرفه‌های موتور قیمت‌گذاری (tariffs) خوانده
//    می‌شود که سفارش نهایی با آن قیمت می‌خورد؛ بنابراین پیش‌نمایش زنده و
//    قیمت نهایی سفارش همیشه سازگارند.
public static class WizardEndpoints
{
    public static IEndpointRouteBuilder MapWizardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder pub = endpoints.MapGroup("/api/v1/public/wizard").RequireRateLimiting("client-read").CacheOutput("public-catalog");
        pub.MapGet("/config", async (string? culture, IConfiguration configuration, CompanyContext companies, ITransactionExecutor tx, CoreDbContext db, IObjectStorage storage, CancellationToken ct)
            => Map(await ConfigAsync(ResolvePublicCompany(configuration), culture ?? "fa", companies, tx, db, storage, ct)));
        pub.MapPost("/quote", async (WizardQuoteRequest request, IConfiguration configuration, CompanyContext companies, ITransactionExecutor tx, CoreDbContext db, CancellationToken ct)
            => Map(await QuoteAsync(ResolvePublicCompany(configuration), request, companies, tx, db, ct)));

        endpoints.MapPost("/api/v1/client/orders/{id:guid}/checkout-gateway-mock", async (Guid id, ClientVersionRequest request, IOrderAdministration orders, CancellationToken ct)
            => Map(await orders.CheckoutWithGatewayMockAsync(id, request.Version, ct), true))
            .RequireAuthorization(SecurityPermissions.ClientOrderWrite).RequireRateLimiting("submit");
        return endpoints;
    }

    private static Guid? ResolvePublicCompany(IConfiguration configuration)
        => configuration.GetValue<string?>("Catalog:PublicCompanyId") is { } raw && Guid.TryParse(raw, out Guid id) && id != Guid.Empty ? id : null;

    // ── config: محصولات/واریانت‌ها با قیمت، رنگ‌ها، اندازه‌های چاپ قابل قیمت‌گذاری و بسته‌بندی‌ها ──
    private static async Task<Result<WizardConfigView>> ConfigAsync(Guid? publicCompany, string culture, CompanyContext companies, ITransactionExecutor tx, CoreDbContext db, IObjectStorage storage, CancellationToken ct)
    {
        if (publicCompany is not Guid company) return Result.Failure<WizardConfigView>(Error.NotFound("wizard.company_not_configured"));
        using IDisposable scope = companies.BeginScope(company);
        return await tx.ExecuteAsync(async t =>
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            List<Domain.Pricing.Tariff> tariffs = await db.Tariffs.AsNoTracking()
                .Where(x => x.CompanyId == company && x.IsActive && x.ValidFrom <= today && (x.ValidTo == null || x.ValidTo >= today))
                .ToListAsync(t);
            Dictionary<Guid, decimal> rawByVariant = tariffs.Where(x => x.TariffType == Domain.Pricing.TariffType.RawProduct && x.ProductVariantId is not null)
                .GroupBy(x => x.ProductVariantId!.Value).ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.PriceGroupId is not null).First().Amount);
            Dictionary<string, decimal> packagingByCode = tariffs.Where(x => x.TariffType == Domain.Pricing.TariffType.Packaging)
                .GroupBy(x => x.Code).ToDictionary(g => g.Key, g => g.First().Amount, StringComparer.OrdinalIgnoreCase);
            decimal shipping = tariffs.FirstOrDefault(x => x.TariffType == Domain.Pricing.TariffType.Shipping && x.Code == "DEFAULT")?.Amount
                ?? tariffs.FirstOrDefault(x => x.TariffType == Domain.Pricing.TariffType.Shipping)?.Amount ?? 0m;
            HashSet<string> pricedPrintSizes = tariffs.Where(x => x.TariffType == Domain.Pricing.TariffType.Print && x.PrintMethodCode is not null)
                .Select(x => x.PrintMethodCode!).ToHashSet(StringComparer.OrdinalIgnoreCase);

            Dictionary<string, string> names = await TranslationsAsync(db, company, ["print.size", "packaging.type", "product.color"], culture, t);
            Dictionary<string, string> descriptions = await DescriptionsAsync(db, company, ["print.size"], culture, t);

            // موجودی انبار «تیشرت خام» برای نمایش به مشتری و جلوگیری از انتخاب بدون موجودی
            Dictionary<Guid, decimal> stockByVariant = await RawTeeStockAsync(db, company, t);
            List<WizardProductView> products = new();
            foreach (var product in await db.Products.AsNoTracking().Where(x => x.CompanyId == company && x.IsActive).OrderByDescending(x => x.ProductCode.StartsWith("RAWTEE-")).ThenBy(x => x.ProductCode).ToListAsync(t))
            {
                List<Domain.Catalog.ProductVariant> variants = await db.ProductVariants.AsNoTracking()
                    .Where(x => x.CompanyId == company && x.ProductId == product.Id && x.IsActive).OrderBy(x => x.Sku).ToListAsync(t);
                decimal sales = await db.ProductSalesPrices.AsNoTracking()
                    .Where(x => x.CompanyId == company && x.ProductId == product.Id && x.IsActive).OrderBy(x => x.Amount).Select(x => x.Amount).FirstOrDefaultAsync(t);
                products.Add(new(product.Id, product.ProductCode, product.Name, product.ProductTypeCode,
                    sales == 0m ? null : sales,
                    variants.Select(v => new WizardVariantView(v.Id, v.Sku, v.DisplayName, v.SizeCode, v.ColorCode,
                        rawByVariant.TryGetValue(v.Id, out decimal raw) ? raw : null,
                        stockByVariant.TryGetValue(v.Id, out decimal stock) ? stock : 0m,
                        string.IsNullOrWhiteSpace(v.ImageStorageKey) ? null : storage.GetPublicObjectUrl(v.ImageStorageKey))).ToList()));
            }

            List<WizardPrintSizeView> printSizes = (await LookupValuesAsync(db, company, "print.size", t))
                .Where(x => pricedPrintSizes.Contains(x))
                .Select(x => new WizardPrintSizeView(x, Name(names, "print.size", x), Description(descriptions, x)))
                .ToList();
            List<WizardPackagingView> packaging = (await LookupValuesAsync(db, company, "packaging.type", t))
                .Select(x => new WizardPackagingView(x, Name(names, "packaging.type", x),
                    packagingByCode.TryGetValue(x, out decimal price) ? price : packagingByCode.TryGetValue("DEFAULT", out decimal fallback) ? fallback : 0m))
                .ToList();
            HashSet<string> colorCodes = products.SelectMany(p => p.Variants).Where(v => !string.IsNullOrWhiteSpace(v.ColorCode)).Select(v => v.ColorCode!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            List<WizardColorView> colors = colorCodes.OrderBy(x => x).Select(x => new WizardColorView(x, Name(names, "product.color", x))).ToList();

            return Result.Success(new WizardConfigView(products, printSizes, packaging, colors, shipping, "IRT"));
        }, ct);
    }

    // موجودی «انبار موجودی تیشرت خام» (کد RAWTEE) به تفکیک واریانت
    private static async Task<Dictionary<Guid, decimal>> RawTeeStockAsync(CoreDbContext db, Guid company, CancellationToken t)
    {
        Domain.Organization.Warehouse? wh = await db.Warehouses.AsNoTracking().SingleOrDefaultAsync(x => x.CompanyId == company && x.Code == "RAWTEE", t);
        if (wh is null) return [];
        return (await db.StockBalances.AsNoTracking().Where(x => x.CompanyId == company && x.WarehouseId == wh.Id).ToListAsync(t)).ToDictionary(x => x.ProductVariantId, x => x.Available);
    }

    // ── quote: قیمت لحظه‌ای پیکربندی ویزارد با همان تعرفه‌های سفارش نهایی ──
    private static async Task<Result<WizardQuoteView>> QuoteAsync(Guid? publicCompany, WizardQuoteRequest request, CompanyContext companies, ITransactionExecutor tx, CoreDbContext db, CancellationToken ct)
    {
        if (publicCompany is not Guid company) return Result.Failure<WizardQuoteView>(Error.NotFound("wizard.company_not_configured"));
        using IDisposable scope = companies.BeginScope(company);
        return await tx.ExecuteAsync(async t =>
        {
            if (request.Quantity < 1 || request.Quantity > 50) return Result.Failure<WizardQuoteView>(Error.Validation("pricing.invalid_data"));
            Domain.Catalog.ProductVariant? variant = await db.ProductVariants.AsNoTracking().SingleOrDefaultAsync(x => x.CompanyId == company && x.Id == request.ProductVariantId, t);
            if (variant is null) return Result.Failure<WizardQuoteView>(Error.NotFound("catalog.variant_not_found"));
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            List<Domain.Pricing.Tariff> tariffs = await db.Tariffs.AsNoTracking()
                .Where(x => x.CompanyId == company && x.IsActive && x.ValidFrom <= today && (x.ValidTo == null || x.ValidTo >= today))
                .ToListAsync(t);
            string? currency = tariffs.FirstOrDefault()?.CurrencyCode ?? "IRT";

            Domain.Pricing.Tariff? raw = tariffs.Where(x => x.TariffType == Domain.Pricing.TariffType.RawProduct && x.ProductVariantId == variant.Id)
                .OrderByDescending(x => x.PriceGroupId is not null).FirstOrDefault();
            decimal rawUnit = raw?.Amount ?? await db.ProductSalesPrices.AsNoTracking()
                .Where(x => x.CompanyId == company && x.ProductId == variant.ProductId && x.IsActive).OrderBy(x => x.Amount).Select(x => x.Amount).FirstOrDefaultAsync(t);
            if (rawUnit <= 0m) return Result.Failure<WizardQuoteView>(Error.BusinessRule("pricing.tariff.missing"));

            string position = string.IsNullOrWhiteSpace(request.PrintPosition) ? "FRONT" : request.PrintPosition.Trim().ToUpperInvariant();
            string size = string.IsNullOrWhiteSpace(request.PrintSizeCode) ? "" : request.PrintSizeCode.Trim().ToUpperInvariant();
            Domain.Pricing.Tariff? print = size.Length > 0
                ? tariffs.Where(x => x.TariffType == Domain.Pricing.TariffType.Print && x.PrintPositionCode == position && x.PrintMethodCode == size).FirstOrDefault()
                    ?? tariffs.Where(x => x.TariffType == Domain.Pricing.TariffType.Print && x.PrintPositionCode == position && x.PrintMethodCode == null).FirstOrDefault()
                : tariffs.Where(x => x.TariffType == Domain.Pricing.TariffType.Print && x.PrintPositionCode == position && x.PrintMethodCode == null).FirstOrDefault();
            decimal printUnit = print?.Amount ?? 0m;

            decimal packaging = 0m;
            if (!string.IsNullOrWhiteSpace(request.PackagingTypeCode))
            {
                string packCode = request.PackagingTypeCode.Trim().ToUpperInvariant();
                packaging = tariffs.Where(x => x.TariffType == Domain.Pricing.TariffType.Packaging && x.Code == packCode).FirstOrDefault()?.Amount
                    ?? tariffs.Where(x => x.TariffType == Domain.Pricing.TariffType.Packaging && x.Code == "DEFAULT").FirstOrDefault()?.Amount ?? 0m;
            }
            decimal shipping = tariffs.Where(x => x.TariffType == Domain.Pricing.TariffType.Shipping && x.Code == "DEFAULT").FirstOrDefault()?.Amount
                ?? tariffs.Where(x => x.TariffType == Domain.Pricing.TariffType.Shipping).FirstOrDefault()?.Amount ?? 0m;

            List<WizardQuoteLine> lines =
            [
                new("product", variant.DisplayName ?? variant.Sku, rawUnit, request.Quantity, Domain.Pricing.Money.Round(rawUnit * request.Quantity)),
                new("print", size.Length > 0 ? $"{position} · {size}" : position, printUnit, request.Quantity, Domain.Pricing.Money.Round(printUnit * request.Quantity)),
                new("packaging", request.PackagingTypeCode, packaging, 1, packaging),
                new("shipping", "DEFAULT", shipping, 1, shipping),
            ];
            return Result.Success(new WizardQuoteView(currency, lines, Domain.Pricing.Money.Round(lines.Sum(x => x.Total))));
        }, ct);
    }

    private static async Task<List<string>> LookupValuesAsync(CoreDbContext db, Guid company, string typeCode, CancellationToken t)
        => (await (from v in db.LookupValues.AsNoTracking()
                   join k in db.LookupTypes.AsNoTracking() on v.LookupTypeId equals k.Id
                   where k.Code == typeCode && v.CompanyId == company && v.IsActive
                   orderby v.DisplayOrder, v.Code
                   select v.Code).ToListAsync(t)).ToList();

    private static async Task<Dictionary<string, string>> TranslationsAsync(CoreDbContext db, Guid company, string[] typeCodes, string culture, CancellationToken t)
        => (await (from v in db.LookupValues.AsNoTracking()
                   join k in db.LookupTypes.AsNoTracking() on v.LookupTypeId equals k.Id
                   join tr in db.LookupValueTranslations.AsNoTracking() on v.Id equals tr.LookupValueId
                   where v.CompanyId == company && v.IsActive && typeCodes.Contains(k.Code)
                   select new { TypeCode = k.Code, v.Code, tr.Culture, tr.DisplayName }).ToListAsync(t))
          .GroupBy(x => x.TypeCode + "|" + x.Code)
          .ToDictionary(g => g.Key, g => Pick(g.Select(x => (x.Culture, x.DisplayName)), culture));

    private static async Task<Dictionary<string, string>> DescriptionsAsync(CoreDbContext db, Guid company, string[] typeCodes, string culture, CancellationToken t)
        => (await (from v in db.LookupValues.AsNoTracking()
                   join k in db.LookupTypes.AsNoTracking() on v.LookupTypeId equals k.Id
                   join tr in db.LookupValueTranslations.AsNoTracking() on v.Id equals tr.LookupValueId
                   where v.CompanyId == company && v.IsActive && typeCodes.Contains(k.Code) && tr.Description != null
                   select new { TypeCode = k.Code, v.Code, tr.Culture, tr.Description }).ToListAsync(t))
          .GroupBy(x => x.TypeCode + "|" + x.Code)
          .ToDictionary(g => g.Key, g => Pick(g.Select(x => (x.Culture, x.Description!)), culture));

    private static string Pick(IEnumerable<(string Culture, string Value)> rows, string culture)
        => rows.FirstOrDefault(x => string.Equals(x.Culture, culture, StringComparison.OrdinalIgnoreCase)).Value
           ?? rows.FirstOrDefault(x => x.Culture == "fa").Value
           ?? rows.FirstOrDefault().Value ?? "";

    private static string Name(Dictionary<string, string> names, string type, string code) => names.TryGetValue(type + "|" + code, out string? value) && value.Length > 0 ? value : code;
    private static string? Description(Dictionary<string, string> descriptions, string code) => descriptions.TryGetValue("print.size|" + code, out string? value) && value.Length > 0 ? value : null;

    private static IResult Map<T>(Result<T> result, bool created = false) => result.IsSuccess
        ? Results.Json(result.Value, statusCode: created ? 201 : 200)
        : Problem(result.Error);
    private static IResult Problem(Error error)
    {
        ProblemDetails p = ApiProblemDetailsMapper.Create(error, "unavailable", null);
        return Results.Problem(p.Detail, p.Instance, p.Status, p.Title, extensions: p.Extensions);
    }
}

public sealed record WizardQuoteRequest(Guid ProductVariantId, string? PrintSizeCode, string? PrintPosition, string? PackagingTypeCode, int Quantity = 1);
public sealed record WizardQuoteLine(string Type, string? Label, decimal UnitAmount, int Quantity, decimal Total);
public sealed record WizardQuoteView(string CurrencyCode, IReadOnlyList<WizardQuoteLine> Lines, decimal Total);
public sealed record WizardVariantView(Guid Id, string Sku, string? DisplayName, string? SizeCode, string? ColorCode, decimal? Price, decimal Stock, string? ImageUrl = null);
public sealed record WizardProductView(Guid Id, string Code, string Name, string ProductTypeCode, decimal? SalesPrice, IReadOnlyList<WizardVariantView> Variants);
public sealed record WizardPrintSizeView(string Code, string Name, string? Description);
public sealed record WizardPackagingView(string Code, string Name, decimal Price);
public sealed record WizardColorView(string Code, string Name);
public sealed record WizardConfigView(IReadOnlyList<WizardProductView> Products, IReadOnlyList<WizardPrintSizeView> PrintSizes, IReadOnlyList<WizardPackagingView> Packaging, IReadOnlyList<WizardColorView> Colors, decimal Shipping, string CurrencyCode);
