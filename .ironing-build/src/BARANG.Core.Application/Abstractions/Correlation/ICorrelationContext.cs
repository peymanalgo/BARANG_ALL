namespace BARANG.Core.Application.Abstractions.Correlation;

public interface ICorrelationContext
{
    string? CorrelationId { get; }
}
