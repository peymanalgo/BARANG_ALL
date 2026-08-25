using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Organization;

public static class OrganizationErrors
{
    public static Error InvalidCode => Error.Validation("organization.code.invalid");
    public static Error InvalidName => Error.Validation("organization.name.invalid");
    public static Error InvalidSetting => Error.Validation("organization.setting.invalid");
    public static Error CrossCompanyReference => Error.NotFound("organization.resource.not_found");
    public static Error HierarchyCycle => Error.BusinessRule("organization.unit.hierarchy_cycle");
    public static Error NotFound => Error.NotFound("organization.resource.not_found");
    public static Error DuplicateCode => Error.Conflict("organization.code.duplicate");
    public static Error Concurrency => Error.Concurrency("organization.concurrency_conflict");
}
