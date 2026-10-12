using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Workflow;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Security;
using BARANG.Core.Domain.Workflow;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Text.Json;

namespace BARANG.Core.Api.Workflow;
public static class WorkflowEndpoints
{
 public static IEndpointRouteBuilder MapWorkflowEndpoints(this IEndpointRouteBuilder e)
 {
  RouteGroupBuilder r=e.MapGroup("/api/v1/workflows");
  r.MapGet("/definition",async(Guid? versionId,CoreDbContext db,ICompanyContext company,CancellationToken ct)=>await ReadDefinition(db,company.CompanyId,versionId,ct)).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapGet("/definition/positions",async(string? search,int? limit,CoreDbContext db,CancellationToken ct)=>Results.Json(await db.Positions.AsNoTracking().Where(x=>string.IsNullOrWhiteSpace(search)||EF.Functions.ILike(x.Code,"%"+search+"%")||EF.Functions.ILike(x.Title,"%"+search+"%")).OrderBy(x=>x.Title).Take(Math.Clamp(limit??20,1,100)).Select(x=>new{id=x.Id,code=x.Code,title=x.Title}).ToListAsync(ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapPost("/definition/validate",(JsonDocument graph)=>Results.Json(ValidateGraph(graph))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapPost("/orders/{orderId:guid}",async(Guid orderId,IWorkflowAdministration s,CancellationToken ct)=>Map(await s.StartAsync(new(orderId),ct),true)).RequireAuthorization(SecurityPermissions.WorkflowManage);
  r.MapGet("/{id:guid}",async(Guid id,IWorkflowAdministration s,CancellationToken ct)=>Map(await s.GetAsync(id,ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapGet("/{id:guid}/history",async(Guid id,IWorkflowAdministration s,CancellationToken ct)=>Map(await s.HistoryAsync(id,ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapPost("/{id:guid}/suspend",async(Guid id,WorkflowReasonCommand c,IWorkflowAdministration s,CancellationToken ct)=>Map(await s.SuspendAsync(id,c,ct))).RequireAuthorization(SecurityPermissions.WorkflowManage);
  r.MapPost("/{id:guid}/resume",async(Guid id,WorkflowVersionCommand c,IWorkflowAdministration s,CancellationToken ct)=>Map(await s.ResumeAsync(id,c,ct))).RequireAuthorization(SecurityPermissions.WorkflowManage);
  r.MapPost("/{id:guid}/cancel",async(Guid id,WorkflowReasonCommand c,IWorkflowAdministration s,CancellationToken ct)=>Map(await s.CancelAsync(id,c,ct))).RequireAuthorization(SecurityPermissions.WorkflowManage);
  r.MapGet("/tasks/mine",async(HumanTaskStatus?status,int?page,int?pageSize,IWorkflowAdministration s,CancellationToken ct)=>Map(await s.MyTasksAsync(status,page??1,pageSize??20,ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapGet("/tasks/roles",async(WorkflowStage?stage,HumanTaskStatus?status,int?page,int?pageSize,IWorkflowAdministration s,CancellationToken ct)=>Map(await s.RoleTasksAsync(stage,status,page??1,pageSize??20,ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapPost("/tasks/{id:guid}/claim",async(Guid id,WorkflowVersionCommand c,IWorkflowAdministration s,CancellationToken ct)=>Map(await s.ClaimAsync(id,c.Version,ct))).RequireAuthorization(SecurityPermissions.WorkflowTaskClaim);
  r.MapPost("/tasks/{id:guid}/release",async(Guid id,WorkflowVersionCommand c,IWorkflowAdministration s,CancellationToken ct)=>Map(await s.ReleaseAsync(id,c.Version,ct))).RequireAuthorization(SecurityPermissions.WorkflowTaskClaim);
  r.MapPost("/tasks/{id:guid}/complete",async(Guid id,CompleteWorkflowTaskCommand c,IWorkflowAdministration s,CancellationToken ct)=>Map(await s.CompleteAsync(id,c,ct))).RequireAuthorization(SecurityPermissions.WorkflowTaskComplete);
  r.MapPut("/sla",async(UpsertSlaCommand c,IWorkflowAdministration s,CancellationToken ct)=>Map(await s.UpsertSlaAsync(c,ct))).RequireAuthorization(SecurityPermissions.WorkflowManage);
  r.MapPost("/escalations/process",async(IWorkflowAdministration s,CancellationToken ct)=>Map(await s.EscalateOverdueAsync(ct))).RequireAuthorization(SecurityPermissions.WorkflowManage);
  r.MapPost("/reconcile",async(IWorkflowAdministration s,CancellationToken ct)=>Map(await s.ReconcileAsync(ct))).RequireAuthorization(SecurityPermissions.WorkflowManage);
  return e;
 }
 private static List<object> ValidateGraph(JsonDocument graph)
 {
  JsonElement root=graph.RootElement;List<object> issues=[];if(!root.TryGetProperty("nodes",out JsonElement nodes)||nodes.ValueKind!=JsonValueKind.Array){issues.Add(new{nodeKey="",messageKey="ExactlyOneStart"});return issues;}
  JsonElement[] rows=nodes.EnumerateArray().ToArray();int starts=rows.Count(x=>IsType(x,0,"Start"));if(starts!=1)issues.Add(new{nodeKey="",messageKey="ExactlyOneStart"});if(!rows.Any(x=>IsType(x,1,"End")))issues.Add(new{nodeKey="",messageKey="AtLeastOneEnd"});HashSet<string> keys=new(StringComparer.OrdinalIgnoreCase);foreach(JsonElement row in rows){string key=row.TryGetProperty("key",out JsonElement k)?k.GetString()??"":string.Empty;if(!keys.Add(key))issues.Add(new{nodeKey=key,messageKey="DuplicateNodeKey"});if(IsType(row,2,"UserTask")&&(!row.TryGetProperty("positionId",out JsonElement position)||position.ValueKind==JsonValueKind.Null||string.IsNullOrWhiteSpace(position.ToString())))issues.Add(new{nodeKey=key,messageKey="TaskPositionRequired"});}return issues;
 }
 private static bool IsType(JsonElement row,int number,string name)=>row.TryGetProperty("type",out JsonElement value)&&(value.ValueKind==JsonValueKind.Number?value.GetInt32()==number:string.Equals(value.GetString(),name,StringComparison.OrdinalIgnoreCase));
 private static async Task<IResult> ReadDefinition(CoreDbContext db,Guid? companyId,Guid? versionId,CancellationToken ct)
 {
  if(companyId is not Guid company||company==Guid.Empty)return Results.Problem("Company context is required",statusCode:400);
  await using DbConnection connection=db.Database.GetDbConnection();await connection.OpenAsync(ct);
  static void Add(DbCommand c,string n,object v){DbParameter p=c.CreateParameter();p.ParameterName=n;p.Value=v;c.Parameters.Add(p);}
  await using DbCommand contextCommand=connection.CreateCommand();contextCommand.CommandText="select set_config('barang.company_id',cast(@company as text),false)";Add(contextCommand,"company",company);await contextCommand.ExecuteScalarAsync(ct);
  await using DbCommand d=connection.CreateCommand();d.CommandText="select id,code,name_json,active_version_id from barang.workflow_definitions where company_id=@company and code='BARANG.ORDER'";Add(d,"company",company);
  await using DbDataReader dr=await d.ExecuteReaderAsync(ct);if(!await dr.ReadAsync(ct))return Results.NotFound();Guid definitionId=dr.GetGuid(0);string code=dr.GetString(1);using JsonDocument definitionName=JsonDocument.Parse(dr.GetString(2));Guid? active=dr.IsDBNull(3)?null:dr.GetGuid(3);await dr.DisposeAsync();Guid selected=versionId??active??Guid.Empty;if(selected==Guid.Empty)return Results.Problem("No active workflow definition",statusCode:422);
  await using DbCommand v=connection.CreateCommand();v.CommandText="select version_number,status,created_at_utc,version from barang.workflow_definition_versions where company_id=@company and definition_id=@definition and id=@id";Add(v,"company",company);Add(v,"definition",definitionId);Add(v,"id",selected);await using DbDataReader vr=await v.ExecuteReaderAsync(ct);if(!await vr.ReadAsync(ct))return Results.NotFound();int number=vr.GetInt32(0);string status=vr.GetString(1);DateTimeOffset created=vr.GetFieldValue<DateTimeOffset>(2);long rowVersion=vr.GetInt64(3);await vr.DisposeAsync();
  List<object> nodes=[];await using DbCommand n=connection.CreateCommand();n.CommandText="select id,node_key,type,title_json,description_json,position_id,x,y,version from barang.workflow_definition_nodes where company_id=@company and definition_version_id=@version order by x,y,node_key";Add(n,"company",company);Add(n,"version",selected);await using DbDataReader nr=await n.ExecuteReaderAsync(ct);while(await nr.ReadAsync(ct)){using JsonDocument title=JsonDocument.Parse(nr.GetString(3));using JsonDocument description=JsonDocument.Parse(nr.GetString(4));nodes.Add(new{id=nr.GetGuid(0),key=nr.GetString(1),type=nr.GetString(2),title=title.RootElement.Clone(),description=description.RootElement.Clone(),positionId=nr.IsDBNull(5)?(Guid?)null:nr.GetGuid(5),x=nr.GetDecimal(6),y=nr.GetDecimal(7),version=nr.GetInt64(8)});}await nr.DisposeAsync();
  List<object> transitions=[];await using DbCommand t=connection.CreateCommand();t.CommandText="select f.node_key,to_node.node_key,tr.condition_json,tr.is_default,tr.sort_order,tr.version from barang.workflow_definition_transitions tr join barang.workflow_definition_nodes f on f.id=tr.from_node_id join barang.workflow_definition_nodes to_node on to_node.id=tr.to_node_id where tr.company_id=@company and tr.definition_version_id=@version order by tr.sort_order,tr.id";Add(t,"company",company);Add(t,"version",selected);await using DbDataReader tr=await t.ExecuteReaderAsync(ct);while(await tr.ReadAsync(ct)){using JsonDocument condition=JsonDocument.Parse(tr.GetString(2));JsonElement c=condition.RootElement;transitions.Add(new{fromKey=tr.GetString(0),toKey=tr.GetString(1),fieldCode=c.TryGetProperty("fieldCode",out JsonElement field)?field.GetString():null,@operator=Optional(c,"operator"),value=Optional(c,"value"),isDefault=tr.GetBoolean(3),sortOrder=tr.GetInt32(4),outcomeCode=c.TryGetProperty("outcome",out JsonElement outcome)?outcome.GetString():null,sourceAnchor=c.TryGetProperty("sourceAnchor",out JsonElement source)?source.GetString():"Auto",targetAnchor=c.TryGetProperty("targetAnchor",out JsonElement target)?target.GetString():"Auto",title=Optional(c,"title"),version=tr.GetInt64(5)});}await tr.DisposeAsync();
  List<object> history=[];await using DbCommand h=connection.CreateCommand();h.CommandText="select id,version_number,status,created_at_utc from barang.workflow_definition_versions where company_id=@company and definition_id=@definition order by version_number desc";Add(h,"company",company);Add(h,"definition",definitionId);await using DbDataReader hr=await h.ExecuteReaderAsync(ct);while(await hr.ReadAsync(ct))history.Add(new{id=hr.GetGuid(0),number=hr.GetInt32(1),status=hr.GetString(2),createdAtUtc=hr.GetFieldValue<DateTimeOffset>(3),isReadOnly=hr.GetGuid(0)!=selected||hr.GetString(2)!="Draft"});
  return Results.Json(new{current=new{id=selected,number,status,createdAtUtc=created,isReadOnly=status!="Draft"||selected!=active,nodes,transitions,version=rowVersion},history,definition=new{id=definitionId,code,name=definitionName.RootElement.Clone()}});
 }
 private static JsonElement? Optional(JsonElement root,string name)=>root.TryGetProperty(name,out JsonElement value)?value.Clone():null;
 private static IResult Map<T>(Result<T>r,bool created=false)=>r.IsSuccess?Results.Json(r.Value,statusCode:created?201:200):Results.Problem(ApiProblemDetailsMapper.Create(r.Error,"workflow",null));
}
