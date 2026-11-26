using Microsoft.Extensions.Localization;

namespace BARANG.Backoffice.Localization;

public sealed class Order360FieldLabelResolver(
    IStringLocalizer<Order360FieldsStrings> fields,
    IStringLocalizer<AppStrings> app)
{
    public string Resolve(string key)
    {
        LocalizedString value = fields[key];
        if (!value.ResourceNotFound && !string.IsNullOrWhiteSpace(value.Value))
        {
            return value.Value;
        }

        value = app[key];
        return !value.ResourceNotFound && !string.IsNullOrWhiteSpace(value.Value)
            ? value.Value
            : fields["Unknown"].Value;
    }
}
