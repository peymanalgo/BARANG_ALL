using BARANG.Core.Domain.Production;using BARANG.Core.Domain.Quality;using Xunit;
namespace BARANG.Core.UnitTests;
#pragma warning disable CA1707
public sealed class QualityFoundationTests
{
 private static readonly Guid Company=Guid.NewGuid(),Order=Guid.NewGuid(),Item=Guid.NewGuid(),Job=Guid.NewGuid(),User=Guid.NewGuid();private static readonly DateTimeOffset Now=new(2026,9,1,12,0,0,TimeSpan.Zero);
 [Fact]public void Template_normalizes_code_and_is_concurrency_protected(){var x=QCChecklistTemplate.Create(Company," garment-final ","Garment final",ProductionJobType.Print,null,Now).Value;Assert.Equal("GARMENT-FINAL",x.Code);Assert.Equal(QualityErrors.Concurrency.Code,x.SetActive(false,2,Now).Error.Code);}
 [Fact]public void Criterion_rejects_invalid_numeric_range(){Assert.True(QCChecklistCriterion.Create(Company,Guid.NewGuid(),"WIDTH","Width",QCCriterionType.Numeric,true,1,10,2,null).IsFailure);}
 [Fact]public void Result_type_must_match_snapshot_criterion(){var c=QCChecklistCriterion.Create(Company,Guid.NewGuid(),"COLOR","Color",QCCriterionType.Choice,true,1,null,null,"RED,BLUE").Value;Assert.True(QCCriteriaResult.Create(Company,Guid.NewGuid(),c,true,null,null,null,User,Now).IsFailure);Assert.True(QCCriteriaResult.Create(Company,Guid.NewGuid(),c,null,null,null,"red",User,Now).IsSuccess);}
 [Fact]public void Approve_finalizes_and_prevents_later_transition(){QCInspection x=Inspection();Assert.True(x.Approve(1,Now).IsSuccess);Assert.Equal(QCOutcome.Approved,x.Outcome);Assert.True(x.Suspend("late",2,Now).IsFailure);}
 [Fact]public void Reject_requires_reason(){QCInspection x=Inspection();Assert.Equal(QualityErrors.ReasonRequired.Code,x.Reject("",1,Now).Error.Code);}
 [Fact]public void Suspend_and_management_review_are_controlled(){QCInspection x=Inspection();Assert.True(x.Suspend("await management",1,Now).IsSuccess);Assert.True(x.Review("manager decision",2,Now).IsSuccess);Assert.True(x.Approve(3,Now).IsSuccess);}
 [Fact]public void Stale_inspection_transition_conflicts(){Assert.Equal(QualityErrors.Concurrency.Code,Inspection().Approve(2,Now).Error.Code);}
 [Fact]public void Defect_requires_positive_quantity(){Assert.True(QCDefect.Create(Company,Guid.NewGuid(),null,Guid.NewGuid(),0,DefectSeverity.Major,null,null,User,Now).IsFailure);}
 [Fact]public void Reproduction_requires_reason_quantity_and_depth(){Assert.True(ReproductionRequest.Create(Company,Guid.NewGuid(),Order,Item,Job,Job,ReproductionStage.Print,0,"bad",User,1,Now).IsFailure);Assert.True(ReproductionRequest.Create(Company,Guid.NewGuid(),Order,Item,Job,Job,ReproductionStage.Print,1,"",User,1,Now).IsFailure);}
 [Fact]public void Reproduction_marking_is_idempotency_guarded(){var x=ReproductionRequest.Create(Company,Guid.NewGuid(),Order,Item,Job,Job,ReproductionStage.FullItem,2,"repeat",User,1,Now).Value;Assert.True(x.MarkJobsCreated(1,Now).IsSuccess);Assert.True(x.MarkJobsCreated(2,Now).IsFailure);}
 [Fact]public void Reproduction_job_preserves_source_chain(){Guid request=Guid.NewGuid(),root=Guid.NewGuid();var j=ProductionJob.CreateReproduction(Company,Order,Item,null,ProductionJobType.HeatPress,request,Job,root,3,null,null,Now).Value;Assert.Equal(request,j.ReproductionRequestId);Assert.Equal(Job,j.SourceProductionJobId);Assert.Equal(root,j.RootProductionJobId);Assert.Equal(3,j.ReproductionDepth);}
 private static QCInspection Inspection()=>QCInspection.Create(Company,Order,Item,Job,Guid.NewGuid(),1,User,null,null,Now);
}
#pragma warning restore CA1707
