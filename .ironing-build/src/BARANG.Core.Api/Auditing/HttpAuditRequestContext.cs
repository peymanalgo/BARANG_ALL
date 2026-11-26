using BARANG.Core.Application.Auditing;

namespace BARANG.Core.Api.Auditing;

public sealed class HttpAuditRequestContext(IHttpContextAccessor accessor) : IAuditRequestContext
{
    private HttpContext? Context => accessor.HttpContext;
    public string? RequestId => Context?.TraceIdentifier;
    public string? IpAddress => Context?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent
    {
        get
        {
            string? value = Context?.Request.Headers.UserAgent.ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value[..Math.Min(value.Length, 512)];
        }
    }
}
