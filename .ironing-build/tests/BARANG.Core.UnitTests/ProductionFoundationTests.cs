using BARANG.Core.Domain.Production;
using Xunit;
namespace BARANG.Core.UnitTests;
#pragma warning disable CA1707
public sealed class ProductionFoundationTests
{
 private static readonly Guid Company=Guid.NewGuid(),Order=Guid.NewGuid(),Item=Guid.NewGuid(),Operator=Guid.NewGuid(),Equipment=Guid.NewGuid();private static readonly DateTimeOffset Now=new(2026,9,1,12,0,0,TimeSpan.Zero);
 [Fact]public void Print_job_requires_operation(){Assert.True(ProductionJob.Create(Company,Order,Item,null,ProductionJobType.Print,null,null,Now).IsFailure);}
 [Fact]public void Generation_key_is_deterministic(){Guid op=Guid.NewGuid();Assert.Equal(ProductionText.Key(Item,ProductionJobType.Print,op),ProductionJob.Create(Company,Order,Item,op,ProductionJobType.Print,null,null,Now).Value.GenerationKey);}
 [Fact]public void Planning_validates_window_and_concurrency(){ProductionJob j=Job();Assert.True(j.Plan(ProductionPriority.High,Now.AddHours(2),Now.AddHours(1),1,1,Now).IsFailure);Assert.Equal(ProductionErrors.Concurrency.Code,j.Plan(ProductionPriority.High,Now,Now.AddHours(1),1,2,Now).Error.Code);}
 [Fact]public void Start_requires_ready_and_operator(){ProductionJob j=Job();Assert.True(j.Start(1,Now).IsFailure);j.Plan(ProductionPriority.Normal,Now,Now.AddHours(1),1,1,Now);Assert.True(j.Start(2,Now).IsFailure);}
 [Fact]public void Design_job_can_start_without_equipment(){ProductionJob j=Job();j.Plan(ProductionPriority.Normal,Now,Now.AddHours(1),1,1,Now);j.AssignOperator(Operator,2,Now);Assert.True(j.Start(3,Now).IsSuccess);}
 [Fact]public void Print_job_requires_equipment(){ProductionJob j=ProductionJob.Create(Company,Order,Item,Guid.NewGuid(),ProductionJobType.Print,null,null,Now).Value;j.Plan(ProductionPriority.Normal,Now,Now.AddHours(1),1,1,Now);j.AssignOperator(Operator,2,Now);Assert.True(j.Start(3,Now).IsFailure);j.AssignEquipment(Equipment,3,Now);Assert.True(j.Start(4,Now).IsSuccess);}
 [Fact]public void Suspend_requires_reason(){ProductionJob j=Started();Assert.Equal(ProductionErrors.ReasonRequired.Code,j.Suspend("",4,Now).Error.Code);}
 [Fact]public void Suspend_resume_preserves_started_time(){ProductionJob j=Started();DateTimeOffset?started=j.StartedAtUtc;j.Suspend("material wait",4,Now.AddMinutes(1));Assert.True(j.Resume(5,Now.AddMinutes(2)).IsSuccess);Assert.Equal(started,j.StartedAtUtc);}
 [Fact]public void Complete_only_from_in_progress(){ProductionJob j=Job();Assert.True(j.Complete(1,null,1,Now).IsFailure);}
 [Fact]public void Failure_requires_valid_state_and_reason(){ProductionJob j=Job();Assert.True(j.Fail("",1,Now).IsFailure);j.Plan(ProductionPriority.Normal,Now,Now.AddHours(1),1,1,Now);Assert.True(j.Fail("design rejected",2,Now).IsSuccess);Assert.Equal(ProductionJobStatus.Failed,j.Status);}
 [Fact]public void Completed_job_is_immutable_through_lifecycle(){ProductionJob j=Started();j.Complete(5,"done",4,Now);Assert.True(j.Suspend("late",5,Now).IsFailure);Assert.Equal(5,j.QuantityCompleted);}
 private static ProductionJob Job()=>ProductionJob.Create(Company,Order,Item,null,ProductionJobType.DesignPreparation,null,null,Now).Value;private static ProductionJob Started(){ProductionJob j=Job();j.Plan(ProductionPriority.Normal,Now,Now.AddHours(1),1,1,Now);j.AssignOperator(Operator,2,Now);j.Start(3,Now);return j;}
}
#pragma warning restore CA1707
