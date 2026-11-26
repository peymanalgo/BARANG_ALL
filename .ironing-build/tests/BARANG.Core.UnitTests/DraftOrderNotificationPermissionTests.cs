using BARANG.Core.Domain.Security;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class DraftOrderNotificationPermissionTests
{
    [Fact] public void DraftOrderPermissionIsRegisteredInCatalog() { SecurityPermissionDefinition definition = Assert.Single(SecurityPermissions.Catalog, x => x.Code == SecurityPermissions.NotificationDraftOrderRead); Assert.Equal("notification", definition.Module); Assert.Equal("notification.draftorder", definition.FeaturePath); Assert.Equal(PermissionCapability.View, definition.Capability); }
    [Fact] public void DraftOrderPermissionIsLastCatalogEntry() => Assert.Equal(SecurityPermissions.NotificationDraftOrderRead, SecurityPermissions.Catalog[^1].Code);
    [Fact] public void DraftOrderPermissionCodeIsValid() => Assert.True(PermissionCode.IsValid(SecurityPermissions.NotificationDraftOrderRead));
    [Fact] public void CatalogCodesAreUnique() => Assert.Equal(SecurityPermissions.Catalog.Count, SecurityPermissions.Catalog.Select(x => x.Code).Distinct(StringComparer.Ordinal).Count());
}
