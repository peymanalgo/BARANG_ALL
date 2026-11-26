using System.Globalization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace BARANG.Backoffice.Localization;

public sealed class UiCultureState
{
    public string Code { get; private set; } = LocalePolicy.Resolve(CultureInfo.CurrentUICulture.Name).Code;
    public event Func<Task>? Changed;

    public void Apply()
    {
        CultureInfo culture = CultureInfo.GetCultureInfo(Code);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public async Task ChangeAsync(string code)
    {
        code = LocalePolicy.Resolve(code).Code;
        if (Code == code) return;
        Code = code;
        Apply();
        if (Changed is not null)
            foreach (Func<Task> handler in Changed.GetInvocationList().Cast<Func<Task>>())
                await handler();
    }
}

// Scope culture to this circuit's inbound activity. Never change process-wide defaults.
public sealed class UiCultureCircuitHandler(UiCultureState culture) : CircuitHandler
{
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next) => async context =>
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo previousUi = CultureInfo.CurrentUICulture;
        culture.Apply();
        try { await next(context); }
        finally { CultureInfo.CurrentCulture = previous; CultureInfo.CurrentUICulture = previousUi; }
    };
}
