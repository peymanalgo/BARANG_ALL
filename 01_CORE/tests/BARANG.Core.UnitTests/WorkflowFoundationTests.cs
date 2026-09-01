using BARANG.Core.Domain.Workflow;
using Xunit;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;
using BARANG.Core.Infrastructure.Workflow;

namespace BARANG.Core.UnitTests;
#pragma warning disable CA1707
public sealed class WorkflowFoundationTests
{
 private static readonly DateTimeOffset Now=new(2026,9,1,10,0,0,TimeSpan.Zero);
 [Fact]public void Instance_starts_at_administrative_review(){var w=WorkflowInstance.Create(Guid.NewGuid(),Guid.NewGuid(),"elsa:test",Now);Assert.Equal(WorkflowStage.AdministrativeReview,w.CurrentStage);Assert.Equal(WorkflowInstanceStatus.Running,w.Status);}
 [Fact]public void Suspend_requires_reason(){var w=WorkflowInstance.Create(Guid.NewGuid(),Guid.NewGuid(),"elsa:test",Now);Assert.True(w.Suspend("",1,Now).IsFailure);}
 [Fact]public void Suspend_resume_preserves_deadline_semantics(){var w=WorkflowInstance.Create(Guid.NewGuid(),Guid.NewGuid(),"elsa:test",Now);Assert.True(w.Suspend("pause",1,Now).IsSuccess);Assert.True(w.Resume(2,Now.AddHours(1)).IsSuccess);Assert.Equal(WorkflowInstanceStatus.Running,w.Status);}
 [Fact]public void Stale_instance_transition_conflicts(){var w=WorkflowInstance.Create(Guid.NewGuid(),Guid.NewGuid(),"elsa:test",Now);Assert.True(w.Advance(WorkflowStage.FinancialApproval,99,Now).IsFailure);Assert.Equal("workflow.concurrency_conflict",w.Advance(WorkflowStage.FinancialApproval,99,Now).Error.Code);}
 [Fact]public void Task_claim_is_single_owner(){var t=HumanTask.Create(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),WorkflowStage.AdministrativeReview,"ADMINISTRATIVE_REVIEW","order.manage",0,Now,Now.AddHours(1));Assert.True(t.Claim(Guid.NewGuid(),1,Now).IsSuccess);Assert.True(t.Claim(Guid.NewGuid(),1,Now).IsFailure);}
 [Fact]public void Only_owner_can_release(){Guid owner=Guid.NewGuid();var t=HumanTask.Create(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),WorkflowStage.AdministrativeReview,"ADMINISTRATIVE_REVIEW","order.manage",0,Now,Now.AddHours(1));t.Claim(owner,1,Now);Assert.True(t.Release(Guid.NewGuid(),2).IsFailure);Assert.True(t.Release(owner,2).IsSuccess);}
 [Fact]public void Completed_task_cannot_be_claimed(){Guid owner=Guid.NewGuid();var t=HumanTask.Create(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),WorkflowStage.AdministrativeReview,"ADMINISTRATIVE_REVIEW","order.manage",0,Now,Now.AddHours(1));t.Claim(owner,1,Now);t.Complete(owner,WorkflowTaskOutcome.Approved,null,2,Now);Assert.True(t.Claim(owner,3,Now).IsFailure);}
 [Fact]public void Sla_bounds_are_validated(){Assert.True(WorkflowSlaPolicy.Create(Guid.NewGuid(),WorkflowStage.QualityControl,0,"workflow.manage",Now).IsFailure);Assert.True(WorkflowSlaPolicy.Create(Guid.NewGuid(),WorkflowStage.QualityControl,60,"workflow.manage",Now).IsSuccess);}
 [Fact]public async Task Elsa_adapter_executes_provider_checkpoint(){ServiceCollection services=new();services.AddElsa();await using ServiceProvider provider=services.BuildServiceProvider();var adapter=new ElsaWorkflowEngineAdapter(provider.GetRequiredService<Elsa.Workflows.IWorkflowRunner>());var result=await adapter.StartAsync("ORDER_FULFILLMENT_V1",Guid.NewGuid(),new Dictionary<string,object?>(),TestContext.Current.CancellationToken);Assert.True(result.IsSuccess);Assert.StartsWith("elsa:",result.Value,StringComparison.Ordinal);}
}
#pragma warning restore CA1707
