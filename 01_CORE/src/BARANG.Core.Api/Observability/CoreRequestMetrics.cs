using System.Diagnostics.Metrics;
using System.Diagnostics;

namespace BARANG.Core.Api.Observability;

public static class CoreRequestMetrics
{
    public const string MeterName = "BARANG.Core";
    private static readonly Meter Meter = new(MeterName, "1.0.0");
    private static readonly Counter<long> Requests = Meter.CreateCounter<long>("barang.http.server.requests");
    private static readonly Counter<long> Failures = Meter.CreateCounter<long>("barang.http.server.failures");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("barang.http.server.duration", "ms");

    public static void Record(string method, int statusCode, double elapsedMilliseconds)
    {
        TagList tags = default;
        tags.Add("http.request.method", method);
        tags.Add("http.response.status_code", statusCode);
        Requests.Add(1, tags);
        Duration.Record(elapsedMilliseconds, tags);
        if (statusCode >= 400) Failures.Add(1, tags);
    }
}
