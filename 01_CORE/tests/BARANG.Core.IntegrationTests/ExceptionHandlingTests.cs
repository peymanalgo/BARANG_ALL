using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BARANG.Core.Api.Errors;
using BARANG.Core.Api.Middleware;
using BARANG.Core.Application.Abstractions.Correlation;
using BARANG.Core.Application.Exceptions;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Exceptions;
using BARANG.Core.Infrastructure.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BARANG.Core.IntegrationTests;

public sealed class ExceptionHandlingTests
{
    [Theory]
    [InlineData("validation", HttpStatusCode.BadRequest, "core.input.invalid")]
    [InlineData("authentication", HttpStatusCode.Unauthorized, "core.authentication.required")]
    [InlineData("authorization", HttpStatusCode.Forbidden, "core.access.denied")]
    [InlineData("concurrency", HttpStatusCode.Conflict, "core.concurrency.conflict")]
    public async Task ControlledFailureReturnsMappedProblemDetails(
        string scenario,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        await using WebApplication application = await CreateApplicationAsync(
            scenario,
            TestContext.Current.CancellationToken);
        using HttpClient client = application.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Correlation-ID", "exception-test-123");

        using HttpResponseMessage response = await client.GetAsync(
            "/failure",
            TestContext.Current.CancellationToken);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(expectedCode, problem.GetProperty("code").GetString());
        Assert.Equal("exception-test-123", problem.GetProperty("correlationId").GetString());
        Assert.False(problem.TryGetProperty("stackTrace", out _));
    }

    [Theory]
    [InlineData("unexpected")]
    [InlineData("infrastructure")]
    public async Task TechnicalFailureReturnsSafeGenericProblem(string scenario)
    {
        await using WebApplication application = await CreateApplicationAsync(
            scenario,
            TestContext.Current.CancellationToken);
        using HttpClient client = application.GetTestClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/failure",
            TestContext.Current.CancellationToken);
        string content = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        using JsonDocument document = JsonDocument.Parse(content);
        JsonElement problem = document.RootElement;

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("database-password", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack", content, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(problem.GetProperty("correlationId").GetString());
    }

    private static async Task<WebApplication> CreateApplicationAsync(
        string scenario,
        CancellationToken cancellationToken)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddSingleton<CorrelationContext>();
        builder.Services.AddSingleton<ICorrelationContext>(static provider =>
            provider.GetRequiredService<CorrelationContext>());

        WebApplication application = builder.Build();
        application.UseMiddleware<CorrelationIdMiddleware>();
        application.UseExceptionHandler();
        application.Run(_ => throw CreateException(scenario));
        await application.StartAsync(cancellationToken);
        return application;
    }

    private static Exception CreateException(string scenario) => scenario switch
    {
        "validation" => new TestCoreException(Error.Validation("core.input.invalid")),
        "authentication" => new TestCoreException(
            Error.Authentication("core.authentication.required")),
        "authorization" => new TestCoreException(Error.Authorization("core.access.denied")),
        "concurrency" => new ConcurrencyException(),
        "infrastructure" => new InfrastructureException(
            "core.storage.failed",
            "database-password must remain private",
            new InvalidOperationException("database-password")),
        _ => new InvalidOperationException("database-password and stack details")
    };

    private sealed class TestCoreException(Error error) : CoreException(error);
}
