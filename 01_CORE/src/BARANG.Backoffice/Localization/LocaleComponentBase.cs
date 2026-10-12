using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace BARANG.Backoffice.Localization;

// A changing culture cascade rerenders existing instances, not a keyed/recreated page.
public abstract class LocaleComponentBase : ComponentBase
{
    [CascadingParameter(Name = "BarangCulture")] public string? ActiveUiCulture { get; set; }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        LocaleRenderCulture.Apply(parameters);
        return base.SetParametersAsync(parameters);
    }
}

public abstract class LocaleLayoutBase : LayoutComponentBase
{
    [CascadingParameter(Name = "BarangCulture")] public string? ActiveUiCulture { get; set; }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        LocaleRenderCulture.Apply(parameters);
        return base.SetParametersAsync(parameters);
    }
}

internal static class LocaleRenderCulture
{
    internal static void Apply(ParameterView parameters)
    {
        if (!parameters.TryGetValue("ActiveUiCulture", out string? code) || code is null) return;
        CultureInfo culture = CultureInfo.GetCultureInfo(LocalePolicy.Resolve(code).Code);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
