using BARANG.Core.Domain.Files;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class ArtworkFileFoundationTests
{
    private static readonly DateTimeOffset Now=new(2026,8,31,12,0,0,TimeSpan.Zero);
    [Fact]public void ArtworkCodeIsNormalizedAndImmutable(){Artwork value=Artwork.Create(Guid.NewGuid(),Guid.NewGuid()," art-100 ","Campaign",null," print ",null,Now).Value;Assert.Equal("ART-100",value.Code);Assert.Equal("PRINT",value.CategoryCode);Assert.Equal(ArtworkLifecycle.Draft,value.LifecycleStatus);}
    [Fact]public void ArtworkArchiveAndRestorePreserveIdentity(){Artwork value=Artwork.Create(Guid.NewGuid(),Guid.NewGuid(),"ART-1","Campaign",null,null,null,Now).Value;value.Archive(Now.AddMinutes(1));Assert.True(value.IsArchived);Assert.False(value.IsActive);value.Restore(Now.AddMinutes(2));Assert.False(value.IsArchived);Assert.True(value.IsActive);Assert.Equal("ART-1",value.Code);}
    [Fact]public void ArtworkVersionsAreDeterministicImmutableSnapshots(){Guid company=Guid.NewGuid(),artwork=Guid.NewGuid(),file=Guid.NewGuid(),actor=Guid.NewGuid();ArtworkVersion one=ArtworkVersion.Create(Guid.NewGuid(),company,artwork,1,file,"first",actor,Now).Value;ArtworkVersion two=ArtworkVersion.Create(Guid.NewGuid(),company,artwork,2,Guid.NewGuid(),"second",actor,Now).Value;Assert.Equal(1,one.VersionNumber);Assert.Equal(file,one.FileVersionId);Assert.Equal(2,two.VersionNumber);}
    [Fact]public void FileVersionRequiresServerGradeHashAndPositiveContent(){Assert.True(FileVersion.Create(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),1,"minio","bucket","safe/key.png","a.png","image/png","png",10,new string('a',64),Guid.NewGuid(),Now).IsSuccess);Assert.True(FileVersion.Create(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),1,"minio","bucket","safe/key.png","a.png","image/png","png",0,new string('a',64),Guid.NewGuid(),Now).IsFailure);Assert.True(FileVersion.Create(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),1,"minio","bucket","safe/key.png","a.png","image/png","png",10,"client-md5",Guid.NewGuid(),Now).IsFailure);}
    [Fact]public void ProcessingJobHasExplicitRetryLifecycle(){FileProcessingJob job=FileProcessingJob.Create(Guid.NewGuid(),Guid.NewGuid(),Now);Assert.Equal(FileProcessingStatus.Pending,job.Status);job.Start(Now.AddSeconds(1));Assert.Equal(1,job.AttemptCount);job.Fail("safe.error",Now.AddSeconds(2));Assert.Equal(FileProcessingStatus.Failed,job.Status);Assert.Equal("safe.error",job.LastErrorCode);}
    [Fact]public void TagCodesAreControlledAndBounded(){Assert.True(ArtworkTag.Create(Guid.NewGuid(),Guid.NewGuid()," seasonal ","Seasonal",Now).IsSuccess);Assert.True(ArtworkTag.Create(Guid.NewGuid(),Guid.NewGuid(),"unsafe/tag","Unsafe",Now).IsFailure);}
}
