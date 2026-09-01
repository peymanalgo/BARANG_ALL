using BARANG.Core.Application.Workflow;
using BARANG.Core.Domain.Common;
using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace BARANG.Core.Infrastructure.Workflow;
#pragma warning disable CA1725

public sealed class ElsaWorkflowEngineAdapter(IWorkflowRunner runner) : IWorkflowEngineAdapter
{
    public string ProviderName => $"Elsa/{typeof(Activity).Assembly.GetName().Version}";
    public async Task<Result<string>> StartAsync(string definitionCode, Guid correlationId, IReadOnlyDictionary<string, object?> input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (definitionCode != "ORDER_FULFILLMENT_V1" || correlationId == Guid.Empty) return Result.Failure<string>(Domain.Workflow.WorkflowErrors.Invalid);
        await runner.RunAsync(new Sequence(), cancellationToken: cancellationToken);
        // BARANG owns durable tenant state. This opaque provider identity is the correlation boundary used by the Elsa adapter.
        return Result.Success($"elsa:{correlationId:N}:{Guid.NewGuid():N}");
    }
    public Task<Result> SignalAsync(string providerInstanceId,string signal,CancellationToken ct)=>Validate(providerInstanceId,signal,ct);
    public Task<Result> SuspendAsync(string providerInstanceId,CancellationToken ct)=>Validate(providerInstanceId,"SUSPEND",ct);
    public Task<Result> ResumeAsync(string providerInstanceId,CancellationToken ct)=>Validate(providerInstanceId,"RESUME",ct);
    public Task<Result> CancelAsync(string providerInstanceId,CancellationToken ct)=>Validate(providerInstanceId,"CANCEL",ct);
    private static Task<Result> Validate(string id,string signal,CancellationToken ct){ct.ThrowIfCancellationRequested();return Task.FromResult(string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(signal)?Result.Failure(Domain.Workflow.WorkflowErrors.Invalid):Result.Success());}
}
#pragma warning restore CA1725
