using BARANG.Core.Domain.Common;

namespace BARANG.Core.Domain.Organization;

public sealed class OrganizationalAssignment
{
    private OrganizationalAssignment() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid PersonId { get; private set; }
    public Guid OrganizationalUnitId { get; private set; }
    public Guid? ManagerAssignmentId { get; private set; }
    public string PositionCode { get; private set; } = string.Empty;
    public string PositionTitle { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public static Result<OrganizationalAssignment> Create(Guid id,Guid companyId,Guid personId,Guid unitId,Guid? managerAssignmentId,string positionCode,string positionTitle,bool isPrimary,DateOnly? start,DateOnly? end,DateTimeOffset now)
    { string code=positionCode.Trim().ToUpperInvariant(); string title=positionTitle.Trim(); if(id==Guid.Empty||companyId==Guid.Empty||personId==Guid.Empty||unitId==Guid.Empty||code.Length is <1 or >64||title.Length is <1 or >200||end<start)return Result.Failure<OrganizationalAssignment>(OrganizationErrors.InvalidAssignment); if(managerAssignmentId==id)return Result.Failure<OrganizationalAssignment>(OrganizationErrors.ReportingCycle); return Result.Success(new OrganizationalAssignment{Id=id,CompanyId=companyId,PersonId=personId,OrganizationalUnitId=unitId,ManagerAssignmentId=managerAssignmentId,PositionCode=code,PositionTitle=title,IsPrimary=isPrimary,StartDate=start,EndDate=end,IsActive=true,CreatedAtUtc=now,UpdatedAtUtc=now,Version=1}); }
    public void SetNotes(string? notes){Notes=string.IsNullOrWhiteSpace(notes)?null:notes.Trim();}
    public Result Change(Guid unitId,Guid? managerAssignmentId,string positionCode,string positionTitle,bool primary,DateOnly? start,DateOnly? end,DateTimeOffset now){if(managerAssignmentId==Id)return Result.Failure(OrganizationErrors.ReportingCycle);string code=positionCode.Trim().ToUpperInvariant();string title=positionTitle.Trim();if(unitId==Guid.Empty||code.Length is <1 or >64||title.Length is <1 or >200||end<start)return Result.Failure(OrganizationErrors.InvalidAssignment);OrganizationalUnitId=unitId;ManagerAssignmentId=managerAssignmentId;PositionCode=code;PositionTitle=title;IsPrimary=primary;StartDate=start;EndDate=end;Touch(now);return Result.Success();}
    public void Deactivate(DateTimeOffset now){if(IsActive){IsActive=false;Touch(now);}}
    public void Activate(DateTimeOffset now){if(!IsActive){IsActive=true;Touch(now);}}
    private void Touch(DateTimeOffset now){UpdatedAtUtc=now;Version++;}
}




