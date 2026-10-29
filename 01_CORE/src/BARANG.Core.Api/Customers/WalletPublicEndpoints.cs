using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Customers;
using BARANG.Core.Domain.Common;
using BARANG.Core.Infrastructure.Context;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace BARANG.Core.Api.Customers;

// Mock payment gateway endpoints for wallet top-ups. The gateway itself is a
// test double: it reads a top-up by its payment token and reports success or
// failure. Real gateway integration replaces only these two routes.
public static class WalletPublicEndpoints
{
    public static void MapWalletPublicEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder gateway = endpoints.MapGroup("/api/v1/public/wallet");
        gateway.MapGet("/topups/{token:guid}", async (Guid token, IConfiguration configuration, CompanyContext companies, IWalletAdministration wallet, CancellationToken ct) =>
        {
            Guid? publicCompany = ResolvePublicCompany(configuration);
            if (publicCompany is null) return Results.StatusCode(503);
            using IDisposable scope = companies.BeginScope(publicCompany.Value);
            Result<WalletTopUpView> result = await wallet.GetByTokenAsync(token, ct);
            return result.IsSuccess
                ? Results.Json(new { number = result.Value.Number, amount = result.Value.Amount, currencyCode = result.Value.CurrencyCode, status = result.Value.Status, gatewayCode = result.Value.GatewayCode, createdAtUtc = result.Value.CreatedAtUtc })
                : Problem(result.Error);
        });

        gateway.MapPost("/topups/{token:guid}/mock-callback", async (Guid token, MockGatewayCallbackRequest r, IConfiguration configuration, CompanyContext companies, IWalletAdministration wallet, CancellationToken ct) =>
        {
            Guid? publicCompany = ResolvePublicCompany(configuration);
            if (publicCompany is null) return Results.StatusCode(503);
            using IDisposable scope = companies.BeginScope(publicCompany.Value);
            Result<WalletTopUpView> result = await wallet.CompleteByTokenAsync(token, r.Success, r.GatewayReference, ct);
            return result.IsSuccess
                ? Results.Json(new { number = result.Value.Number, status = result.Value.Status, amount = result.Value.Amount, paidAtUtc = result.Value.PaidAtUtc })
                : Problem(result.Error);
        }).RequireRateLimiting("client-mutation");
    }

    private static Guid? ResolvePublicCompany(IConfiguration configuration)
    {
        string? configured = configuration.GetValue<string?>("Catalog:PublicCompanyId");
        return string.IsNullOrWhiteSpace(configured) || !Guid.TryParse(configured, out Guid publicCompany) || publicCompany == Guid.Empty ? null : publicCompany;
    }

    private static IResult Problem(Error error)
    {
        ProblemDetails problem = ApiProblemDetailsMapper.Create(error, "wallet", null);
        return Results.Problem(problem.Detail, problem.Instance, problem.Status, problem.Title, extensions: problem.Extensions);
    }
}

public sealed record MockGatewayCallbackRequest(bool Success, string? GatewayReference);
