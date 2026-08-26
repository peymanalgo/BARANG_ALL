using System.Text.Json;
using BARANG.Core.Application.Auditing;
using BARANG.Core.Infrastructure.Auditing;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class AuditFoundationTests
{
    [Fact]
    public void SensitiveMetadataIsRedactedRecursivelyAndCaseInsensitively()
    {
        Dictionary<string, object?> metadata = new()
        {
            ["Password"] = "plain-secret",
            ["safe"] = "visible",
            ["nested"] = new Dictionary<string, object?>
            {
                ["access_token"] = "bearer-value",
                ["Authorization"] = "Bearer credential",
                ["apiKey"] = "machine-secret",
                ["passwordHash"] = "hash-value"
            }
        };

        string json = AuditRedactor.Serialize(metadata);

        Assert.DoesNotContain("plain-secret", json, StringComparison.Ordinal);
        Assert.DoesNotContain("bearer-value", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Bearer credential", json, StringComparison.Ordinal);
        Assert.DoesNotContain("machine-secret", json, StringComparison.Ordinal);
        Assert.DoesNotContain("hash-value", json, StringComparison.Ordinal);
        Assert.Contains("visible", json, StringComparison.Ordinal);
        Assert.Equal(5, json.Split("[REDACTED]", StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void ChangeFormatRepresentsNullOldAndNewValues()
    {
        string json = AuditRedactor.Serialize(new Dictionary<string, object?>
        {
            ["displayName"] = new Dictionary<string, object?> { ["old"] = null, ["new"] = "New" }
        });
        using JsonDocument document = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("displayName").GetProperty("old").ValueKind);
        Assert.Equal("New", document.RootElement.GetProperty("displayName").GetProperty("new").GetString());
    }

    [Fact]
    public void TaxonomyUsesStableTechnicalNames()
    {
        Assert.Equal("authentication.login.succeeded", AuditActions.LoginSucceeded);
        Assert.Equal("security.user_role.assigned", AuditActions.UserRoleAssigned);
        Assert.Equal("system.operation.failed", AuditActions.OperationFailed);
    }
}
