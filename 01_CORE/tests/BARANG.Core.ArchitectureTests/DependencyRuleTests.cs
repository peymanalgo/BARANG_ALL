using System.Reflection;
using System.Xml.Linq;
using BARANG.Core.Application;
using BARANG.Core.Domain;
using BARANG.Core.Infrastructure.Persistence;
using Xunit;

namespace BARANG.Core.ArchitectureTests;

public sealed class DependencyRuleTests
{
    private static readonly Assembly Domain = typeof(CoreAssemblyMarker).Assembly;
    private static readonly Assembly Application = typeof(CoreIdentity).Assembly;
    private static readonly Assembly Infrastructure = typeof(CoreDbContext).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;

    [Fact]
    public void ProductionAssembliesRespectLayerDependencies()
    {
        AssertReferencesOnly(Domain);
        AssertReferencesOnly(Application, "BARANG.Core.Domain");
        AssertReferencesOnly(Infrastructure, "BARANG.Core.Application", "BARANG.Core.Domain");
        AssertReferencesOnly(
            Api,
            "BARANG.Core.Application",
            "BARANG.Core.Domain",
            "BARANG.Core.Infrastructure");
    }

    [Fact]
    public void ProjectReferencesRespectLayerDependencies()
    {
        AssertProjectReferences("BARANG.Core.Domain", []);
        AssertProjectReferences("BARANG.Core.Application", ["BARANG.Core.Domain"]);
        AssertProjectReferences(
            "BARANG.Core.Infrastructure",
            ["BARANG.Core.Application", "BARANG.Core.Domain"]);
        AssertProjectReferences(
            "BARANG.Core.Api",
            ["BARANG.Core.Application", "BARANG.Core.Infrastructure"]);
    }

    [Theory]
    [MemberData(nameof(PortableAssemblies))]
    public void DomainAndApplicationRemainProviderAndHttpNeutral(Assembly assembly)
    {
        string[] prohibitedPrefixes =
        [
            "Microsoft.AspNetCore.Http",
            "Microsoft.EntityFrameworkCore",
            "Npgsql",
            "BARANG.Web",
            "BARANG.Mobile"
        ];

        string[] references = assembly.GetReferencedAssemblies().Select(static reference => reference.Name!).ToArray();
        Assert.DoesNotContain(references, reference =>
            prohibitedPrefixes.Any(prefix => reference.StartsWith(prefix, StringComparison.Ordinal)));
    }

    public static TheoryData<Assembly> PortableAssemblies() => new() { Domain, Application };

    private static void AssertReferencesOnly(Assembly assembly, params string[] allowedBarangReferences)
    {
        string[] actual = assembly.GetReferencedAssemblies()
            .Select(static reference => reference.Name!)
            .Where(static name => name.StartsWith("BARANG.", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.All(actual, reference => Assert.Contains(reference, allowedBarangReferences));
    }

    private static void AssertProjectReferences(string projectName, string[] expectedReferences)
    {
        string coreDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        string projectFile = Path.Combine(coreDirectory, "src", projectName, $"{projectName}.csproj");
        XDocument project = XDocument.Load(projectFile);
        string[] actualReferences = project.Descendants("ProjectReference")
            .Select(static element => element.Attribute("Include")?.Value)
            .Where(static value => value is not null)
            .Select(static value => Path.GetFileNameWithoutExtension(value!))
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] expected = expectedReferences.Order(StringComparer.Ordinal).ToArray();

        Assert.Equal(expected, actualReferences);
    }
}
