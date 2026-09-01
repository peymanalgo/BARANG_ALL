using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Workflow;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Security;
using BARANG.Core.Domain.Workflow;

namespace BARANG.Core.Api.Workflow;
public static class WorkflowEndpoints
{
 public static IEndpointRouteBuilder MapWorkflowEndpoints(this IEndpointRouteBuilder e)
 {
  RouteGroupBuilder r=e.MapGroup("/api/v1/workflows");
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
 private static IResult Map<T>(Result<T>r,bool created=false)=>r.IsSuccess?Results.Json(r.Value,statusCode:created?201:200):Results.Problem(ApiProblemDetailsMapper.Create(r.Error,"workflow",null));
}
