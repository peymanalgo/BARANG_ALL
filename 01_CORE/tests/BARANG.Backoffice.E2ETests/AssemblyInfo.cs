using Xunit;
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace BARANG.Backoffice.E2ETests;

internal static class E2EEnvironmentGuard
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void ValidateTarget()
    {
        var baseUrl = Environment.GetEnvironmentVariable("BARANG_E2E_BASE_URL");
        var sharedDevelopmentDbAllowed = string.Equals(
            Environment.GetEnvironmentVariable("BARANG_E2E_ALLOW_SHARED_DEV_DB"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(baseUrl) || IsLocalhost(baseUrl))
        {
            if (!sharedDevelopmentDbAllowed)
            {
                throw new InvalidOperationException(
                    "Backoffice E2E tests require an isolated deployed environment. " +
                    "Set BARANG_E2E_BASE_URL to that environment, or explicitly set " +
                    "BARANG_E2E_ALLOW_SHARED_DEV_DB=true for an intentional development run.");
            }
        }
    }

    private static bool IsLocalhost(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase));
    }
}
