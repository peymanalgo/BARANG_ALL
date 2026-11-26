using System.Security.Cryptography;
using System.Text;
using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Abstractions.Correlation;
using BARANG.Core.Application.BusinessAnalysis;
using BARANG.Core.Application.Catalog;
using BARANG.Core.Application.Customers;
using BARANG.Core.Application.Files;
using BARANG.Core.Application.Orders;
using BARANG.Core.Application.Lookups;
using BARANG.Core.Application.Organization;
using BARANG.Core.Application.Pricing;
using BARANG.Core.Domain.Common;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Domain.Orders;
using BARANG.Core.Infrastructure.Persistence;
using BARANG.Core.Domain.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Api.Client;

public static class ClientApiEndpoints
{
    public static IEndpointRouteBuilder MapClientApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder root=endpoints.MapGroup("/api/v1/client").RequireRateLimiting("client-read");
        RouteGroupBuilder catalog=root.MapGroup("/catalog").RequireAuthorization(SecurityPermissions.ClientCatalogRead);catalog.MapGet("/products",async(string?search,int?page,int?pageSize,ICatalogAdministration service,CancellationToken ct)=>Map(await PublicProducts(service,search,page??1,pageSize??20,ct)));catalog.MapGet("/products/{id:guid}",async(Guid id,ICatalogAdministration service,CancellationToken ct)=>Map(await service.GetProductAsync(id,ct)));catalog.MapGet("/products/{id:guid}/variants",async(Guid id,ICatalogAdministration service,CancellationToken ct)=>Map(await PublicVariants(service,id,ct)));
        RouteGroupBuilder customers=root.MapGroup("/customers");customers.MapPost("/register",async(CustomerDataCommand command,ICustomerAdministration service,CancellationToken ct)=>Map(await service.CreateAsync(command,ct),true)).RequireAuthorization(SecurityPermissions.ClientCustomerWrite).RequireRateLimiting("client-mutation");customers.MapGet("/by-code/{code}",async(string code,ICustomerAdministration service,CancellationToken ct)=>Map(await service.GetByCodeAsync(code,ct))).RequireAuthorization(SecurityPermissions.ClientCustomerRead);customers.MapGet("/{id:guid}",async(Guid id,ICustomerAdministration service,CancellationToken ct)=>Map(await service.GetAsync(id,ct))).RequireAuthorization(SecurityPermissions.ClientCustomerRead);customers.MapPut("/{id:guid}",async(Guid id,CustomerUpdateCommand command,ICustomerAdministration service,CancellationToken ct)=>Map(await service.UpdateAsync(id,command,ct))).RequireAuthorization(SecurityPermissions.ClientCustomerWrite).RequireRateLimiting("client-mutation");customers.MapGet("/{id:guid}/addresses",async(Guid id,ICustomerAdministration service,CancellationToken ct)=>Map(await service.ListAddressesAsync(id,true,ct))).RequireAuthorization(SecurityPermissions.ClientCustomerRead);customers.MapPost("/{id:guid}/addresses",async(Guid id,BARANG.Core.Application.Customers.AddressCommand command,ICustomerAdministration service,CancellationToken ct)=>Map(await service.AddAddressAsync(id,command,ct),true)).RequireAuthorization(SecurityPermissions.ClientCustomerWrite).RequireRateLimiting("client-mutation");customers.MapPut("/{id:guid}/addresses/{addressId:guid}",async(Guid id,Guid addressId,BARANG.Core.Application.Customers.AddressCommand command,ICustomerAdministration service,CancellationToken ct)=>Map(await service.UpdateAddressAsync(id,addressId,command,ct))).RequireAuthorization(SecurityPermissions.ClientCustomerWrite).RequireRateLimiting("client-mutation");customers.MapPost("/{id:guid}/addresses/{addressId:guid}/remove",async(Guid id,Guid addressId,ClientVersionRequest request,ICustomerAdministration service,CancellationToken ct)=>Map(await service.DeleteAddressAsync(id,addressId,request.Version,ct))).RequireAuthorization(SecurityPermissions.ClientCustomerWrite);customers.MapPost("/{id:guid}/addresses/{addressId:guid}/default",async(Guid id,Guid addressId,ClientVersionRequest request,ICustomerAdministration service,CancellationToken ct)=>Map(await service.SetDefaultAddressAsync(id,addressId,request.Version,ct))).RequireAuthorization(SecurityPermissions.ClientCustomerWrite).RequireRateLimiting("client-mutation");
        RouteGroupBuilder orders=root.MapGroup("/orders");orders.MapPost("/drafts",CreateDraftAsync).RequireAuthorization(SecurityPermissions.ClientOrderWrite).RequireRateLimiting("client-mutation");orders.MapGet("/",ListOrdersAsync).RequireAuthorization(SecurityPermissions.ClientOrderRead);orders.MapGet("/{id:guid}",async(Guid id,IOrderAdministration service,CancellationToken ct)=>Map(await service.GetAsync(id,ct))).RequireAuthorization(SecurityPermissions.ClientOrderRead);orders.MapPut("/{id:guid}",async(Guid id,EditDraftCommand command,IOrderAdministration service,CancellationToken ct)=>Map(await service.EditDraftAsync(id,command,ct))).RequireAuthorization(SecurityPermissions.ClientOrderWrite).RequireRateLimiting("client-mutation");orders.MapPost("/{id:guid}/items",async(Guid id,ItemCommand command,IOrderAdministration service,CancellationToken ct)=>Map(await service.AddItemAsync(id,command,ct),true)).RequireAuthorization(SecurityPermissions.ClientOrderWrite).RequireRateLimiting("client-mutation");orders.MapPut("/{id:guid}/items/{itemId:guid}",async(Guid id,Guid itemId,ItemCommand command,IOrderAdministration service,CancellationToken ct)=>Map(await service.UpdateItemAsync(id,itemId,command,ct))).RequireAuthorization(SecurityPermissions.ClientOrderWrite).RequireRateLimiting("client-mutation");orders.MapPost("/{id:guid}/items/{itemId:guid}/remove",async(Guid id,Guid itemId,ClientVersionRequest request,IOrderAdministration service,CancellationToken ct)=>Map(await service.RemoveItemAsync(id,itemId,request.Version,ct))).RequireAuthorization(SecurityPermissions.ClientOrderWrite);orders.MapPost("/{id:guid}/items/{itemId:guid}/print-operations",async(Guid id,Guid itemId,PrintOperationCommand command,IOrderAdministration service,CancellationToken ct)=>Map(await service.AddPrintAsync(id,itemId,command,ct),true)).RequireAuthorization(SecurityPermissions.ClientOrderWrite);orders.MapPut("/{id:guid}/items/{itemId:guid}/print-operations/{operationId:guid}",async(Guid id,Guid itemId,Guid operationId,PrintOperationCommand command,IOrderAdministration service,CancellationToken ct)=>Map(await service.UpdatePrintAsync(id,itemId,operationId,command,ct))).RequireAuthorization(SecurityPermissions.ClientOrderWrite);orders.MapPost("/{id:guid}/pricing/preview",async(Guid id,ClientVersionRequest request,IPricingAdministration service,CancellationToken ct)=>Map(await service.RecalculateAsync(id,request.Version,null,ct))).RequireAuthorization(SecurityPermissions.ClientPricingPreview);orders.MapPost("/{id:guid}/submit",SubmitAsync).RequireAuthorization(SecurityPermissions.ClientOrderWrite).RequireRateLimiting("submit");orders.MapPost("/{id:guid}/cancel",async(Guid id,CancelOrderCommand command,IOrderAdministration service,CancellationToken ct)=>Map(await service.CancelAsync(id,command,ct))).RequireAuthorization(SecurityPermissions.ClientOrderWrite);orders.MapGet("/{id:guid}/business-profile",async(Guid id,IBusinessProfileAdministration s,CancellationToken ct)=>Map(await s.GetOrderAsync(id,ct))).RequireAuthorization(SecurityPermissions.ClientOrderRead);orders.MapPut("/{id:guid}/business-profile",async(Guid id,ClientBusinessProfileRequest r,IBusinessProfileAdministration s,CancellationToken ct)=>Map(await s.SetOrderAsync(id,new OrderBusinessProfileCommand(r.ShippingMethodCode,r.PackagingTypeCode,r.IncludeInvoiceInPackage,r.OrderTypeCode,r.Notes,r.Version),ct))).RequireAuthorization(SecurityPermissions.ClientOrderWrite).RequireRateLimiting("client-mutation");orders.MapGet("/{id:guid}/validate",async(Guid id,IOrderAdministration service,CancellationToken ct)=>Map(await service.ValidateAsync(id,ct))).RequireAuthorization(SecurityPermissions.ClientOrderRead);orders.MapGet("/form",FormAsync).RequireAuthorization(SecurityPermissions.ClientOrderRead);orders.MapGet("/{id:guid}/tracking",TrackingAsync).RequireAuthorization(SecurityPermissions.ClientTrackingRead);
        root.MapGet("/customers/{customerId:guid}/favorites", async (Guid customerId, string? culture, ITransactionExecutor tx, CoreDbContext db, IObjectStorage storage, CancellationToken ct) =>
        {
            Result<ClientFavoriteDesignView[]> result = await tx.ExecuteAsync(async t =>
            {
                List<(Guid ArtworkId, DateTimeOffset CreatedAtUtc)> favs = await db.CustomerFavoriteDesigns.AsNoTracking()
                    .Where(x => x.CustomerId == customerId).OrderByDescending(x => x.CreatedAtUtc)
                    .Select(x => new ValueTuple<Guid, DateTimeOffset>(x.ArtworkId, x.CreatedAtUtc)).ToListAsync(t);
                Guid[] ids = favs.Select(x => x.ArtworkId).ToArray();
                var artRows = await db.Artworks.AsNoTracking()
                    .Where(x => ids.Contains(x.Id))
                    .Select(x => new { x.Id, x.Title, x.CategoryCode, Version = x.CurrentVersionId }).ToListAsync(t);
                List<Domain.Files.ArtworkTranslation> favTrs = await db.ArtworkTranslations.AsNoTracking().Where(x => ids.Contains(x.ArtworkId)).ToListAsync(t);
                Dictionary<Guid, (string Title, string? CategoryCode, Guid? ArtworkVersion)> arts = artRows.ToDictionary(
                    x => x.Id,
                    x => (x.Title, x.CategoryCode, (Guid?)x.Version));
                Guid[] artworkVersionIds = arts.Values.Where(x => x.ArtworkVersion is not null).Select(x => x.ArtworkVersion!.Value).ToArray();
                Dictionary<Guid, Guid> fvByAv = await db.ArtworkVersions.AsNoTracking().Where(x => artworkVersionIds.Contains(x.Id))
                    .Select(x => new KeyValuePair<Guid, Guid>(x.Id, x.FileVersionId)).ToDictionaryAsync(p => p.Key, p => p.Value, t);
                Guid[] fvIds = fvByAv.Values.ToArray();
                List<(Guid FileVersion, BARANG.Core.Domain.Files.FileDerivativeType Type, string Key)> derivatives = await db.FileDerivatives.AsNoTracking()
                    .Where(d => fvIds.Contains(d.SourceFileVersionId))
                    .Select(d => new ValueTuple<Guid, BARANG.Core.Domain.Files.FileDerivativeType, string>(d.SourceFileVersionId, d.DerivativeType, d.StorageKey)).ToListAsync(t);
                ClientFavoriteDesignView[] rows = favs.Where(f => arts.ContainsKey(f.ArtworkId)).Select(f =>
                {
                    var art = arts[f.ArtworkId];
                    string? thumbnail = null, preview = null;
                    if (art.ArtworkVersion is Guid av && fvByAv.TryGetValue(av, out Guid fv))
                    {
                        thumbnail = derivatives.Where(d => d.FileVersion == fv && d.Type == BARANG.Core.Domain.Files.FileDerivativeType.Thumbnail).Select(d => (string?)d.Key).FirstOrDefault();
                        preview = derivatives.Where(d => d.FileVersion == fv && (d.Type == BARANG.Core.Domain.Files.FileDerivativeType.PublicWeb || d.Type == BARANG.Core.Domain.Files.FileDerivativeType.Web)).Select(d => (string?)d.Key).FirstOrDefault();
                    }
                    string favTitle = BARANG.Core.Api.Files.PublicCatalogEndpoints.LocalizedArtworkText(favTrs, f.ArtworkId, art.Title, culture);
                    return new ClientFavoriteDesignView(f.ArtworkId, favTitle, art.CategoryCode,
                        string.IsNullOrWhiteSpace(thumbnail) ? null : storage.GetPublicObjectUrl(thumbnail),
                        string.IsNullOrWhiteSpace(preview) ? null : storage.GetPublicObjectUrl(preview), f.CreatedAtUtc);
                }).ToArray();
                return Result.Success(rows);
            }, ct);
            return Map(result);
        }).RequireAuthorization(SecurityPermissions.ClientCustomerRead);
        root.MapPut("/customers/{customerId:guid}/favorites/{artworkId:guid}", async (Guid customerId, Guid artworkId, ITransactionExecutor tx, CoreDbContext db, CancellationToken ct) =>
        {
            Result result = await tx.ExecuteAsync(async t =>
            {
                bool customerExists = await db.Customers.AsNoTracking().AnyAsync(x => x.Id == customerId, t);
                bool artworkExists = await db.Artworks.AsNoTracking().AnyAsync(x => x.Id == artworkId, t);
                if (!customerExists || !artworkExists) return Result.Failure(BARANG.Core.Domain.Files.FileErrors.NotFound);
                bool exists = await db.CustomerFavoriteDesigns.AnyAsync(x => x.CustomerId == customerId && x.ArtworkId == artworkId, t);
                if (exists) return Result.Success();
                Guid companyId = await db.Artworks.AsNoTracking().Where(x => x.Id == artworkId).Select(x => x.CompanyId).SingleAsync(t);
                db.Add(BARANG.Core.Domain.Customers.CustomerFavoriteDesign.Create(Guid.NewGuid(), companyId, customerId, artworkId, DateTime.UtcNow));
                return Result.Success();
            }, ct);
            return result.IsSuccess ? Results.NoContent() : new ClientProblemResult(result.Error);
        }).RequireAuthorization(SecurityPermissions.ClientCustomerWrite).RequireRateLimiting("client-mutation");
        root.MapPost("/customers/{customerId:guid}/wallet/topups", async (Guid customerId, HttpRequest http, WalletTopUpRequest request, IWalletAdministration wallet, CancellationToken ct) =>
        {
            if (!TryKey(http, out string key)) return Problem(Error.Validation("api.idempotency_key.required"), http);
            Guid token = DeterministicGuid("topup:" + key);
            Result<WalletTopUpView> existing = await wallet.GetByTokenAsync(token, ct);
            if (existing.IsSuccess) return Results.Json(existing.Value);
            Result<WalletTopUpView> result = await wallet.CreateTopUpAsync(customerId, new CreateTopUpCommand(request.Amount, request.CurrencyCode ?? "IRT"), ct);
            return Map(result, true);
        }).RequireAuthorization(SecurityPermissions.ClientCustomerWrite).RequireRateLimiting("client-mutation");
        root.MapGet("/customers/{customerId:guid}/wallet/topups", async (Guid customerId, string? status, int? page, int? pageSize, IWalletAdministration wallet, CancellationToken ct) => Map(await wallet.ListTopUpsAsync(customerId, status, page ?? 1, pageSize ?? 20, ct))).RequireAuthorization(SecurityPermissions.ClientCustomerRead);
        root.MapPost("/customers/{customerId:guid}/wallet/topups/{topUpId:guid}/cancel", async (Guid customerId, Guid topUpId, ClientVersionRequest request, IWalletAdministration wallet, CancellationToken ct) => Map(await wallet.CancelTopUpAsync(customerId, topUpId, request.Version, ct))).RequireAuthorization(SecurityPermissions.ClientCustomerWrite).RequireRateLimiting("client-mutation");
        root.MapGet("/customers/{customerId:guid}/referral", async (Guid customerId, IReferralAdministration referrals, CancellationToken ct) => Map(await referrals.GetInfoAsync(customerId, ct))).RequireAuthorization(SecurityPermissions.ClientCustomerRead);
        root.MapPost("/customers/{customerId:guid}/referrals/attach", async (Guid customerId, ReferralAttachRequest request, IReferralAdministration referrals, CancellationToken ct) => Map(await referrals.AttachAsync(customerId, request.Code, ct))).RequireAuthorization(SecurityPermissions.ClientCustomerWrite).RequireRateLimiting("client-mutation");
        root.MapPost("/orders/{id:guid}/checkout-wallet", async (Guid id, ClientVersionRequest request, IOrderAdministration orders, CancellationToken ct) => Map(await orders.CheckoutWithWalletAsync(id, request.Version, ct), true)).RequireAuthorization(SecurityPermissions.ClientOrderWrite).RequireRateLimiting("client-mutation");orders.MapPost("/orders/{id:guid}/pay-with-wallet", async (Guid id, ClientVersionRequest request, IWalletAdministration wallet, CancellationToken ct) => Map(await wallet.PayOrderFromWalletAsync(id, request.Version, ct))).RequireAuthorization(SecurityPermissions.ClientOrderWrite).RequireRateLimiting("client-mutation");
        root.MapGet("/orders/{id:guid}/wallet-payment", async (Guid id, ITransactionExecutor tx, CoreDbContext db, CancellationToken ct) =>
        {
            Result<WalletPaymentStatusView> result = await tx.ExecuteAsync(async t =>
            {
                var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, t);
                if (order is null) return Result.Failure<WalletPaymentStatusView>(BARANG.Core.Domain.Files.FileErrors.NotFound);
                var price = await db.OrderPriceSnapshots.AsNoTracking().SingleOrDefaultAsync(x => x.OrderId == id && x.IsCurrent && x.IsFinalized, t);
                decimal paid = await db.Payments.AsNoTracking().Where(x => x.OrderId == id && x.RecordType == BARANG.Core.Domain.Finance.PaymentRecordType.Payment).SumAsync(x => (decimal?)x.Amount, t) ?? 0m;
                decimal reversed = await db.Payments.AsNoTracking().Where(x => x.OrderId == id && x.RecordType == BARANG.Core.Domain.Finance.PaymentRecordType.Reversal).SumAsync(x => (decimal?)x.Amount, t) ?? 0m;
                decimal credit = await db.CustomerWalletTransactions.AsNoTracking().Where(x => x.CustomerId == order.CustomerId && x.Direction == BARANG.Core.Domain.Customers.CustomerWalletDirection.Credit).SumAsync(x => (decimal?)x.Amount, t) ?? 0m;
                decimal debit = await db.CustomerWalletTransactions.AsNoTracking().Where(x => x.CustomerId == order.CustomerId && x.Direction == BARANG.Core.Domain.Customers.CustomerWalletDirection.Debit).SumAsync(x => (decimal?)x.Amount, t) ?? 0m;
                decimal remaining = price is null ? 0m : price.FinalTotal - (paid - reversed);
                return Result.Success(new WalletPaymentStatusView(order.OrderNumber, order.Status.ToString(), price?.CurrencyCode ?? "IRT", price?.FinalTotal ?? 0m, paid - reversed, remaining > 0 ? remaining : 0m, credit - debit, price is not null && remaining <= 0));
            }, ct);
            return Map(result);
        }).RequireAuthorization(SecurityPermissions.ClientOrderRead);
        root.MapDelete("/customers/{customerId:guid}/favorites/{artworkId:guid}", async (Guid customerId, Guid artworkId, ITransactionExecutor tx, CoreDbContext db, CancellationToken ct) =>
        {
            Result result = await tx.ExecuteAsync(async t =>
            {
                var favorite = await db.CustomerFavoriteDesigns.SingleOrDefaultAsync(f => f.CustomerId == customerId && f.ArtworkId == artworkId, t);
                if (favorite is null) return Result.Success();
                db.Remove(favorite);
                return Result.Success();
            }, ct);
            return result.IsSuccess ? Results.NoContent() : new ClientProblemResult(result.Error);
        }).RequireAuthorization(SecurityPermissions.ClientCustomerWrite);
        root.MapGet("/customers/{customerId:guid}/wallet", async (Guid customerId, ITransactionExecutor tx, CoreDbContext db, CancellationToken ct) =>
        {
            Result<ClientWalletView> result = await tx.ExecuteAsync(async t =>
            {
                bool customerExists = await db.Customers.AsNoTracking().AnyAsync(x => x.Id == customerId, t);
                if (!customerExists) return Result.Failure<ClientWalletView>(BARANG.Core.Domain.Files.FileErrors.NotFound);
                decimal credit = await db.CustomerWalletTransactions.AsNoTracking()
                    .Where(x => x.CustomerId == customerId && x.Direction == BARANG.Core.Domain.Customers.CustomerWalletDirection.Credit)
                    .SumAsync(x => x.Amount, t);
                decimal debit = await db.CustomerWalletTransactions.AsNoTracking()
                    .Where(x => x.CustomerId == customerId && x.Direction == BARANG.Core.Domain.Customers.CustomerWalletDirection.Debit)
                    .SumAsync(x => x.Amount, t);
                List<ClientWalletEntryView> rows = await db.CustomerWalletTransactions.AsNoTracking()
                    .Where(x => x.CustomerId == customerId).OrderByDescending(x => x.CreatedAtUtc).Take(50)
                    .Select(x => new ClientWalletEntryView(x.Id, x.Direction == BARANG.Core.Domain.Customers.CustomerWalletDirection.Credit ? "credit" : "debit", x.Amount, x.CurrencyCode, x.ReasonCode, x.ReferenceNote, x.OrderId, x.CreatedAtUtc))
                    .ToListAsync(t);
                string currency = rows.Count > 0 ? rows[0].CurrencyCode : "IRT";
                return Result.Success(new ClientWalletView(credit - debit, currency, rows));
            }, ct);
            return Map(result);
        }).RequireAuthorization(SecurityPermissions.ClientCustomerRead);
        root.MapGet("/customers/{customerId:guid}/feedback", async (Guid customerId, ITransactionExecutor tx, CoreDbContext db, CancellationToken ct) =>
        {
            Result<ClientFeedbackEntryView[]> result = await tx.ExecuteAsync(async t =>
            {
                bool customerExists = await db.Customers.AsNoTracking().AnyAsync(x => x.Id == customerId, t);
                if (!customerExists) return Result.Failure<ClientFeedbackEntryView[]>(BARANG.Core.Domain.Files.FileErrors.NotFound);
                ClientFeedbackEntryView[] rows = await db.CustomerFeedbacks.AsNoTracking()
                    .Where(x => x.CustomerId == customerId).OrderByDescending(x => x.CreatedAtUtc).Take(50)
                    .Select(x => new ClientFeedbackEntryView(x.Id, x.FeedbackTypeCode, x.Subject, x.Body, x.StatusCode, x.CreatedAtUtc))
                    .ToArrayAsync(t);
                return Result.Success(rows);
            }, ct);
            return Map(result);
        }).RequireAuthorization(SecurityPermissions.ClientCustomerRead);
        root.MapPost("/customers/{customerId:guid}/feedback", async (Guid customerId, ClientFeedbackRequest request, ITransactionExecutor tx, CoreDbContext db, CancellationToken ct) =>
        {
            Result<Guid> result = await tx.ExecuteAsync(async t =>
            {
                Guid? companyId = await db.Customers.AsNoTracking().Where(x => x.Id == customerId).Select(x => (Guid?)x.CompanyId).SingleOrDefaultAsync(t);
                if (companyId is null) return Result.Failure<Guid>(BARANG.Core.Domain.Files.FileErrors.NotFound);
                Result<BARANG.Core.Domain.Customers.CustomerFeedback> created = BARANG.Core.Domain.Customers.CustomerFeedback.Create(Guid.NewGuid(), companyId.Value, customerId, request.FeedbackTypeCode, request.Subject, request.Body, DateTime.UtcNow);
                if (created.IsFailure) return Result.Failure<Guid>(created.Error);
                db.Add(created.Value);
                return Result.Success(created.Value.Id);
            }, ct);
            return result.IsSuccess ? Results.Created($"/client/customers/{customerId}/feedback", new ClientFeedbackCreatedView(result.Value)) : new ClientProblemResult(result.Error);
        }).RequireAuthorization(SecurityPermissions.ClientCustomerWrite).RequireRateLimiting("client-mutation");
        root.MapPost("/customers/{customerId:guid}/custom-designs", async (Guid customerId, HttpRequest http, ITransactionExecutor tx, CoreDbContext db, IArtworkFileAdministration files, IObjectStorage storage, CancellationToken ct) =>
        {
            if (!TryKey(http, out string key)) return Problem(Error.Validation("api.idempotency_key.required"), http);
            if (!http.HasFormContentType) return Problem(Error.Validation("file.invalid"), http);
            IFormCollection form = await http.ReadFormAsync(ct);
            IFormFile? file = form.Files.GetFile("file");
            string title = form["title"].ToString();
            if (file is null) return Problem(Error.Validation("file.invalid"), http);
            await using Stream stream = file.OpenReadStream();
            Result<UploadView> upload = await files.UploadAsync(new(null, file.FileName, file.ContentType, file.Length, stream, form["sha256"]), ct);
            if (upload.IsFailure) return new ClientProblemResult(upload.Error);
            Guid fileVersionId = upload.Value.FileVersionId;
            Result<ClientCustomDesignView> result = await tx.ExecuteAsync(async t =>
            {
                Guid? companyId = await db.Customers.AsNoTracking().Where(x => x.Id == customerId).Select(x => (Guid?)x.CompanyId).SingleOrDefaultAsync(t);
                if (companyId is null) return Result.Failure<ClientCustomDesignView>(BARANG.Core.Domain.Files.FileErrors.NotFound);
                Result<BARANG.Core.Domain.Customers.CustomerCustomDesign> created = BARANG.Core.Domain.Customers.CustomerCustomDesign.Create(Guid.NewGuid(), companyId.Value, customerId, title, fileVersionId, DateTime.UtcNow);
                if (created.IsFailure) return Result.Failure<ClientCustomDesignView>(created.Error);
                db.Add(created.Value);
                string? thumbnail = await db.FileDerivatives.AsNoTracking().Where(d => d.SourceFileVersionId == fileVersionId && d.DerivativeType == BARANG.Core.Domain.Files.FileDerivativeType.Thumbnail).Select(d => (string?)d.StorageKey).FirstOrDefaultAsync(t);
                string? preview = await db.FileDerivatives.AsNoTracking().Where(d => d.SourceFileVersionId == fileVersionId && (d.DerivativeType == BARANG.Core.Domain.Files.FileDerivativeType.PublicWeb || d.DerivativeType == BARANG.Core.Domain.Files.FileDerivativeType.Web)).Select(d => (string?)d.StorageKey).FirstOrDefaultAsync(t);
                return Result.Success(new ClientCustomDesignView(created.Value.Id, created.Value.Title, fileVersionId,
                    string.IsNullOrWhiteSpace(thumbnail) ? null : storage.GetPublicObjectUrl(thumbnail),
                    string.IsNullOrWhiteSpace(preview) ? null : storage.GetPublicObjectUrl(preview), created.Value.CreatedAtUtc));
            }, ct);
            return result.IsSuccess ? Results.Created($"/client/customers/{customerId}/custom-designs", result.Value) : new ClientProblemResult(result.Error);
        }).RequireAuthorization(SecurityPermissions.ClientCustomerWrite).RequireRateLimiting("client-mutation").DisableAntiforgery();
        root.MapGet("/customers/{customerId:guid}/custom-designs", async (Guid customerId, ITransactionExecutor tx, CoreDbContext db, IObjectStorage storage, CancellationToken ct) =>
        {
            Result<ClientCustomDesignView[]> result = await tx.ExecuteAsync(async t =>
            {
                bool customerExists = await db.Customers.AsNoTracking().AnyAsync(x => x.Id == customerId, t);
                if (!customerExists) return Result.Failure<ClientCustomDesignView[]>(BARANG.Core.Domain.Files.FileErrors.NotFound);
                List<(Guid Id, string Title, Guid FileVersionId, DateTimeOffset CreatedAtUtc)> rows = await db.CustomerCustomDesigns.AsNoTracking()
                    .Where(x => x.CustomerId == customerId).OrderByDescending(x => x.CreatedAtUtc).Take(100)
                    .Select(x => new ValueTuple<Guid, string, Guid, DateTimeOffset>(x.Id, x.Title, x.FileVersionId, x.CreatedAtUtc)).ToListAsync(t);
                Guid[] fvIds = rows.Select(x => x.FileVersionId).ToArray();
                List<(Guid FileVersion, BARANG.Core.Domain.Files.FileDerivativeType Type, string Key)> derivatives = await db.FileDerivatives.AsNoTracking()
                    .Where(d => fvIds.Contains(d.SourceFileVersionId))
                    .Select(d => new ValueTuple<Guid, BARANG.Core.Domain.Files.FileDerivativeType, string>(d.SourceFileVersionId, d.DerivativeType, d.StorageKey)).ToListAsync(t);
                return Result.Success(rows.Select(r =>
                {
                    string? thumbnail = derivatives.Where(d => d.FileVersion == r.FileVersionId && d.Type == BARANG.Core.Domain.Files.FileDerivativeType.Thumbnail).Select(d => (string?)d.Key).FirstOrDefault();
                    string? preview = derivatives.Where(d => d.FileVersion == r.FileVersionId && (d.Type == BARANG.Core.Domain.Files.FileDerivativeType.PublicWeb || d.Type == BARANG.Core.Domain.Files.FileDerivativeType.Web)).Select(d => (string?)d.Key).FirstOrDefault();
                    return new ClientCustomDesignView(r.Id, r.Title, r.FileVersionId,
                        string.IsNullOrWhiteSpace(thumbnail) ? null : storage.GetPublicObjectUrl(thumbnail),
                        string.IsNullOrWhiteSpace(preview) ? null : storage.GetPublicObjectUrl(preview), r.CreatedAtUtc);
                }).ToArray());
            }, ct);
            return Map(result);
        }).RequireAuthorization(SecurityPermissions.ClientCustomerRead);
        root.MapDelete("/customers/{customerId:guid}/custom-designs/{designId:guid}", async (Guid customerId, Guid designId, ITransactionExecutor tx, CoreDbContext db, CancellationToken ct) =>
        {
            Result result = await tx.ExecuteAsync(async t =>
            {
                var design = await db.CustomerCustomDesigns.SingleOrDefaultAsync(x => x.CustomerId == customerId && x.Id == designId, t);
                if (design is null) return Result.Success();
                db.Remove(design);
                return Result.Success();
            }, ct);
            return result.IsSuccess ? Results.NoContent() : new ClientProblemResult(result.Error);
        }).RequireAuthorization(SecurityPermissions.ClientCustomerWrite);
                root.MapPost("/files/upload",UploadAsync).RequireAuthorization(SecurityPermissions.ClientFileUpload).RequireRateLimiting("upload").DisableAntiforgery();root.MapGet("/branches",async(IOrganizationAdministration service,CancellationToken ct)=>{Result<IReadOnlyList<BranchView>>branches=await service.ListBranchesAsync(ct);return branches.IsSuccess?Results.Json(branches.Value.Where(x=>x.IsActive).ToArray()):new ClientProblemResult(branches.Error);}).RequireAuthorization(SecurityPermissions.ClientOrderRead);root.MapGet("/lookups/{typeCode}/options",async(string typeCode,string?culture,ILookupAdministration service,CancellationToken ct)=>ClientLookupTypes.Contains(typeCode)?Map(await service.GetOptionsAsync(typeCode,string.IsNullOrWhiteSpace(culture)?"fa":culture,true,ct)):Problem(Error.NotFound("lookup.type_not_found"),null)).RequireAuthorization(SecurityPermissions.ClientCatalogRead);return endpoints;
    }
    private static async Task<Result<IReadOnlyList<PublicProduct>> >PublicProducts(ICatalogAdministration service,string?search,int page,int pageSize,CancellationToken ct){var result=await service.SearchProductsAsync(new(search,true,Math.Max(1,page),Math.Clamp(pageSize,1,100)),ct);return result.IsFailure?Result.Failure<IReadOnlyList<PublicProduct>>(result.Error):Result.Success<IReadOnlyList<PublicProduct>>(result.Value.Items.Select(x=>new PublicProduct(x.Id,x.ProductCode,x.Name,x.ProductTypeCode,x.UnitOfMeasureCode,x.IsActive)).ToArray());}
    private static async Task<Result<IReadOnlyList<PublicVariant>>>PublicVariants(ICatalogAdministration service,Guid id,CancellationToken ct){var result=await service.ListVariantsAsync(id,true,ct);return result.IsFailure?Result.Failure<IReadOnlyList<PublicVariant>>(result.Error):Result.Success<IReadOnlyList<PublicVariant>>(result.Value.Select(x=>new PublicVariant(x.Id,x.ProductId,x.Sku,x.DisplayName,x.SizeCode,x.ColorCode,x.IsUsable)).ToArray());}
    private static async Task<IResult>CreateDraftAsync(HttpRequest http,CreateDraftCommand command,IOrderAdministration service,CancellationToken ct){if(!TryKey(http,out string key))return Problem(Error.Validation("api.idempotency_key.required"),http);string external=string.IsNullOrWhiteSpace(command.ExternalReference)?$"idem:{key}":command.ExternalReference;CreateDraftCommand effective=command with{ExternalReference=external};Result<OrderView>result=await service.CreateDraftAsync(effective,ct);if(result.IsFailure){Result<Page<OrderSummaryView>>found=await service.SearchAsync(new(null,null,null,null,null,effective.Source,null,null,null,null,external),ct);if(found.IsSuccess&&found.Value.Items.Count==1)return Map(await service.GetAsync(found.Value.Items[0].Id,ct));}return Map(result,true);}
    private static async Task<IResult>SubmitAsync(Guid id,HttpRequest http,ClientVersionRequest request,IOrderAdministration service,CancellationToken ct){if(!TryKey(http,out _))return Problem(Error.Validation("api.idempotency_key.required"),http);return Map(await service.SubmitAsync(id,request.Version,ct));}
    private static async Task<IResult>ListOrdersAsync(Guid? customerId,string? status,string? search,int? page,int? pageSize,IOrderAdministration service,CancellationToken ct){OrderStatus? parsed=status is null?null:Enum.TryParse<OrderStatus>(status,ignoreCase:true,out OrderStatus value)?value:null;Result<Page<OrderSummaryView>>result=await service.SearchAsync(new(search,customerId,null,parsed,null,null,null,null,null,null,null,page??1,Math.Clamp(pageSize??20,1,100)),ct);return Map(result);}
    private static async Task<IResult>UploadAsync(HttpRequest http,IArtworkFileAdministration service,CancellationToken ct){if(!TryKey(http,out string key)||!http.HasFormContentType)return Problem(Error.Validation("api.idempotency_key.required"),http);IFormCollection form=await http.ReadFormAsync(ct);IFormFile?file=form.Files.GetFile("file");if(file is null)return Problem(Error.Validation("file.invalid"),http);Guid fileId=DeterministicGuid(key);await using Stream stream=file.OpenReadStream();Result<UploadView>result=await service.UploadAsync(new(fileId,file.FileName,file.ContentType,file.Length,stream,form["sha256"]),ct);if(result.IsFailure){Result<StoredFileView>existing=await service.GetFileAsync(fileId,ct);if(existing.IsSuccess)return Map(existing);}return Map(result,true);}
    private static async Task<IResult>TrackingAsync(Guid id,IOrderAdministration orders,BARANG.Core.Infrastructure.Persistence.CoreDbContext db,CancellationToken ct){Result<OrderView>order=await orders.GetAsync(id,ct);if(order.IsFailure)return Map(order);var shipments=await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(db.Shipments.AsNoTracking().Where(x=>x.OrderId==id).OrderBy(x=>x.CreatedAtUtc).Select(x=>new PublicShipment(x.ShipmentNumber,x.CarrierCode,x.ShippingMethodCode,x.TrackingCode,x.Status.ToString(),x.ShipmentDateUtc,x.DeliveredAtUtc)),ct);Result<IReadOnlyList<OrderTimelineView>>timeline=await orders.TimelineAsync(id,ct);string[]allowed=["Submitted","Cancelled","StatusChanged"];return Results.Ok(new PublicTracking(order.Value.OrderNumber,order.Value.Status.ToString(),shipments,timeline.IsSuccess?timeline.Value.Where(x=>allowed.Contains(x.EventType)).Select(x=>new PublicTrackingEvent(x.EventType,x.OccurredAtUtc)).ToArray():[]));}
    private static bool TryKey(HttpRequest request,out string key){key=request.Headers["Idempotency-Key"].ToString();return key.Length is>=8 and<=128&&key.All(x=>char.IsAsciiLetterOrDigit(x)||x is'-'or'_'or'.'or':');}private static Guid DeterministicGuid(string key){byte[]hash=SHA256.HashData(Encoding.UTF8.GetBytes(key));return new Guid(hash[..16]);}
    private static readonly HashSet<string> ClientLookupTypes=new(StringComparer.Ordinal){"shipping.method","shipping.carrier","packaging.type","customer.address.type","order.cancellation.reason","print.method","wallet.reason","feedback.type","feedback.status"};
    private static readonly string[] FormPrintPositions=["FRONT","BACK","SLEEVE","OTHER"];private static readonly string[] FormCustomerTypes=["individual","legalEntity"];private static readonly string[] FormAddressTypes=["shipping","billing","warehouse","office"];
    private static IResult FormAsync()=>Results.Json(new ClientOrderForm(FormPrintPositions,FormCustomerTypes,FormAddressTypes));
    private static IResult Map(Result result)=>result.IsSuccess?Results.NoContent():new ClientProblemResult(result.Error);private static IResult Map<T>(Result<T>result,bool created=false)=>result.IsSuccess?Results.Json(result.Value,statusCode:created?201:200):new ClientProblemResult(result.Error);private static ClientProblemResult Problem(Error error,HttpRequest?request)=>new(error,request?.Path);
    private sealed class ClientProblemResult(Error error,PathString?path=null):IResult
    {
        public Task ExecuteAsync(HttpContext context){ICorrelationContext?correlation=context.RequestServices.GetService<ICorrelationContext>();ProblemDetails p=ApiProblemDetailsMapper.Create(error,correlation?.CorrelationId??context.TraceIdentifier,path??context.Request.Path);return Results.Problem(p.Detail,p.Instance,p.Status,p.Title,extensions:p.Extensions).ExecuteAsync(context);}
    }
}
public sealed record PublicProduct(Guid Id,string Code,string Name,string ProductTypeCode,string UomCode,bool AvailableToOrder);public sealed record PublicVariant(Guid Id,Guid ProductId,string Sku,string?DisplayName,string?SizeCode,string?ColorCode,bool AvailableToOrder);public sealed record ClientVersionRequest(long Version);public sealed record PublicShipment(string ShipmentNumber,string CarrierCode,string ShippingMethodCode,string?TrackingCode,string Status,DateTimeOffset?ShippedAtUtc,DateTimeOffset?DeliveredAtUtc);public sealed record PublicTrackingEvent(string Type,DateTimeOffset OccurredAtUtc);public sealed record PublicTracking(string?OrderNumber,string OrderStatus,IReadOnlyList<PublicShipment>Shipments,IReadOnlyList<PublicTrackingEvent>Events);public sealed record ClientBusinessProfileRequest(string?ShippingMethodCode,string?PackagingTypeCode,bool IncludeInvoiceInPackage,string?OrderTypeCode,string?Notes,long Version);public sealed record ClientFavoriteDesignView(Guid ArtworkId, string Title, string? CategoryCode, string? ThumbnailUrl, string? PreviewUrl, DateTimeOffset CreatedAtUtc);public sealed record ClientWalletView(decimal Balance, string CurrencyCode, IReadOnlyList<ClientWalletEntryView> Transactions);public sealed record ClientWalletEntryView(Guid Id, string Direction, decimal Amount, string CurrencyCode, string ReasonCode, string? ReferenceNote, Guid? OrderId, DateTimeOffset CreatedAtUtc);public sealed record ClientFeedbackEntryView(Guid Id, string FeedbackTypeCode, string Subject, string Body, string StatusCode, DateTimeOffset CreatedAtUtc);public sealed record ClientFeedbackRequest(string FeedbackTypeCode, string Subject, string Body);public sealed record ClientFeedbackCreatedView(Guid Id);public sealed record ClientCustomDesignView(Guid Id, string Title, Guid FileVersionId, string? ThumbnailUrl, string? PreviewUrl, DateTimeOffset CreatedAtUtc);public sealed record ReferralAttachRequest(string Code);public sealed record WalletTopUpRequest(decimal Amount, string? CurrencyCode);public sealed record WalletPaymentStatusView(string? OrderNumber, string OrderStatus, string CurrencyCode, decimal Total, decimal Paid, decimal Remaining, decimal WalletBalance, bool FullyPaid);
public sealed record ClientOrderForm(IReadOnlyList<string>PrintPositions,IReadOnlyList<string>CustomerTypes,IReadOnlyList<string>AddressTypes);
