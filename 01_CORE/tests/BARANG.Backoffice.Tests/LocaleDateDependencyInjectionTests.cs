using BARANG.Backoffice.Components;
using BARANG.Backoffice.Components.Pages;
using BARANG.Backoffice.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Microsoft.AspNetCore.Routing;

namespace BARANG.Backoffice.Tests;

public sealed class LocaleDateDependencyInjectionTests
{
    [Fact]
    public void RealBackofficeProviderResolvesOneDateServiceForBothContracts()
    {
        using WebApplicationFactory<Program> application = new();
        using IServiceScope scope = application.Services.CreateScope();

        LocaleDateService concrete = scope.ServiceProvider.GetRequiredService<LocaleDateService>();
        ILocaleDateService abstraction = scope.ServiceProvider.GetRequiredService<ILocaleDateService>();

        Assert.Same(concrete, abstraction);
    }

    [Theory]
    [InlineData(typeof(OrganizationalChartTree))]
    [InlineData(typeof(Persons))]
    public void RealBackofficeProviderSatisfiesEveryInjectedComponentDependency(Type componentType)
    {
        using WebApplicationFactory<Program> application = new();
        using IServiceScope scope = application.Services.CreateScope();

        PropertyInfo[] injectedProperties = componentType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(property => property.GetCustomAttributes(typeof(InjectAttribute), true).Length != 0)
            .ToArray();

        Assert.NotEmpty(injectedProperties);
        foreach (PropertyInfo property in injectedProperties)
            Assert.NotNull(scope.ServiceProvider.GetService(property.PropertyType));
    }

    [Fact]
    public async Task RealBackofficeStartupMapsInteractiveServerCircuitAndFrameworkAsset()
    {
        using WebApplicationFactory<Program> application = new();
        using HttpClient client = application.CreateClient();
        using HttpResponseMessage asset = await client.GetAsync("/_framework/blazor.web.js", TestContext.Current.CancellationToken);
        IEnumerable<EndpointDataSource> sources = application.Services.GetServices<EndpointDataSource>();

        Assert.True(asset.IsSuccessStatusCode);
        Assert.Contains(sources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>(), endpoint => endpoint.RoutePattern.RawText?.Contains("_blazor", StringComparison.OrdinalIgnoreCase) == true);
    }
}
