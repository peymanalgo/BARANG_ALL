using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Workflow;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Security;
using BARANG.Core.Domain.Workflow;
using BARANG.Core.Domain.Orders;

namespace BARANG.Core.Api.Workflow;
public static class WorkflowEndpoints
{
 public static IEndpointRouteBuilder MapWorkflowEndpoints(this IEndpointRouteBuilder e)
 {
  RouteGroupBuilder r=e.MapGroup("/api/v1/workflows");
  r.MapGet("/definition",async(Guid?versionId,IWorkflowDesignerAdministration s,CancellationToken ct)=>Map(await s.GetAsync(versionId,ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapPut("/definition/draft",async(WorkflowGraphCommand c,IWorkflowDesignerAdministration s,CancellationToken ct)=>Map(await s.SaveDraftAsync(c,ct))).RequireAuthorization(SecurityPermissions.WorkflowManage);
  r.MapGet("/definition/validate/{versionId:guid}",async(Guid versionId,IWorkflowDesignerAdministration s,CancellationToken ct)=>Map(await s.ValidateAsync(versionId,ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapPost("/definition/validate",async(WorkflowGraphCommand c,IWorkflowDesignerAdministration s,CancellationToken ct)=>Map(await s.ValidateAsync(c,ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapPost("/definition/publish/{versionId:guid}",async(Guid versionId,IWorkflowDesignerAdministration s,CancellationToken ct)=>Map(await s.PublishAsync(versionId,ct))).RequireAuthorization(SecurityPermissions.WorkflowManage);
  r.MapGet("/definition/positions",async(string?search,int?limit,IWorkflowDesignerAdministration s,CancellationToken ct)=>Map(await s.PositionsAsync(search,limit??20,ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapGet("/definition/condition-fields",(IWorkflowDesignerAdministration s)=>Results.Ok(s.ConditionFields)).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapPost("/definition/runtime/start",async(StartDefinitionWorkflowCommand c,IWorkflowDefinitionRuntime s,CancellationToken ct)=>Map(await s.StartAsync(c,ct),true)).RequireAuthorization(SecurityPermissions.WorkflowManage);
  r.MapPost("/definition/runtime/tasks/{id:guid}/complete",async(Guid id,CompleteDefinitionWorkflowTaskCommand c,IWorkflowDefinitionRuntime s,CancellationToken ct)=>Map(await s.CompleteTaskAsync(id,c.ResultJson,c.Attachments,c.Version,ct))).RequireAuthorization(SecurityPermissions.WorkflowTaskComplete);
  r.MapGet("/definition/runtime/orders/{orderId:guid}/decision-attachments",async(Guid orderId,IWorkflowDefinitionRuntime s,CancellationToken ct)=>Map(await s.DecisionAttachmentsAsync(orderId,ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapGet("/definition/runtime/decision-attachments/{id:guid}/download",async(Guid id,IWorkflowDefinitionRuntime s,CancellationToken ct)=>{var result=await s.DownloadDecisionAttachmentAsync(id,ct);return result.IsFailure?Map(result):Results.Stream(result.Value.Content,result.Value.MediaType,result.Value.FileName,enableRangeProcessing:false);}).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapPost("/definition/runtime/tasks/{id:guid}/claim",async(Guid id,ClaimDefinitionWorkflowTaskCommand c,IWorkflowDefinitionRuntime s,CancellationToken ct)=>Map(await s.ClaimTaskAsync(id,c.Version,ct))).RequireAuthorization(SecurityPermissions.WorkflowTaskComplete);
  r.MapPost("/definition/runtime/tasks/{id:guid}/release",async(Guid id,ClaimDefinitionWorkflowTaskCommand c,IWorkflowDefinitionRuntime s,CancellationToken ct)=>Map(await s.ReleaseTaskAsync(id,c.Version,ct))).RequireAuthorization(SecurityPermissions.WorkflowTaskComplete);
  r.MapGet("/definition/runtime/tasks/{id:guid}/forward-targets",async(Guid id,string?search,IWorkflowDefinitionRuntime s,CancellationToken ct)=>Map(await s.ForwardTargetsAsync(id,search,ct))).RequireAuthorization(SecurityPermissions.WorkflowTaskComplete);
  r.MapPost("/definition/runtime/tasks/{id:guid}/forward",async(Guid id,ControlledForwardCommand c,IWorkflowDefinitionRuntime s,CancellationToken ct)=>Map(await s.ForwardTaskAsync(id,c,ct))).RequireAuthorization(SecurityPermissions.WorkflowTaskComplete);
  r.MapGet("/definition/runtime/substitutions",async(string?search,IWorkflowDefinitionRuntime s,CancellationToken ct)=>Map(await s.ListSubstitutionsAsync(search,ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapGet("/definition/runtime/substitutions/candidates",async(string?search,IWorkflowDefinitionRuntime s,CancellationToken ct)=>Map(await s.SubstitutionCandidatesAsync(search,ct))).RequireAuthorization(SecurityPermissions.SecurityUserManage);
  r.MapPost("/definition/runtime/substitutions",async(CreateWorkflowSubstitutionCommand c,IWorkflowDefinitionRuntime s,CancellationToken ct)=>Map(await s.CreateSubstitutionAsync(c,ct),true)).RequireAuthorization(SecurityPermissions.SecurityUserManage);
  r.MapPost("/definition/runtime/substitutions/{id:guid}/revoke",async(Guid id,long version,IWorkflowDefinitionRuntime s,CancellationToken ct)=>Map(await s.RevokeSubstitutionAsync(id,version,ct))).RequireAuthorization(SecurityPermissions.SecurityUserManage);
  r.MapGet("/definition/runtime/{instanceId:guid}/tasks",async(Guid instanceId,IWorkflowDefinitionRuntime s,CancellationToken ct)=>Map(await s.TasksAsync(instanceId,ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapGet("/definition/runtime/tasks/eligible",async(string?search,int?page,int?pageSize,Guid?taskId,string?stageKey,Guid?customerId,DateOnly?fromDate,DateOnly?toDate,string?status,string?sort,bool?descending,bool?starredOnly,string?aging,string?priority,string?followUp,IWorkflowDefinitionRuntime s,CancellationToken ct)=>Map(await s.EligibleTasksAsync(new CartableTaskQuery(search,page??1,pageSize??20,taskId,stageKey,customerId,fromDate,toDate,null,status,sort,descending??true,starredOnly??false,aging,priority,followUp),ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapGet("/definition/runtime/tasks/referrals",async(string?search,int?page,int?pageSize,Guid?taskId,string?stageKey,Guid?customerId,DateOnly?fromDate,DateOnly?toDate,string?outcome,string?status,string?sort,bool?descending,bool?starredOnly,string?priority,string?followUp,IWorkflowDefinitionRuntime s,CancellationToken ct)=>Map(await s.MyReferralsAsync(new CartableTaskQuery(search,page??1,pageSize??20,taskId,stageKey,customerId,fromDate,toDate,outcome,status,sort,descending??true,starredOnly??false,null,priority,followUp),ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapGet("/definition/runtime/tasks/{id:guid}/quick-preview",async(Guid id,IWorkflowDefinitionRuntime s,CancellationToken ct)=>Map(await s.QuickPreviewAsync(id,ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapPost("/definition/runtime/tasks/{taskId:guid}/personal-state",async(Guid taskId,CartablePersonalStateCommand c,IWorkflowDefinitionRuntime s,CancellationToken ct)=>Map(await s.SetPersonalStateAsync(taskId,c,ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapGet("/definition/runtime/tasks/{id:guid}/context",async(Guid id,IWorkflowDefinitionRuntime s,CancellationToken ct)=>Map(await s.WorkflowContextAsync(id,ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapGet("/definition/runtime/workload-summary",async(IWorkflowDefinitionRuntime s,CancellationToken ct)=>Map(await s.WorkloadSummaryAsync(ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapGet("/definition/runtime/workbench",async(string?search,int?page,int?pageSize,Guid?taskId,string?stageKey,Guid?customerId,DateOnly?fromDate,DateOnly?toDate,string?outcome,string?status,string?sort,bool?descending,bool?starredOnly,string?aging,string?priority,string?followUp,bool?historical,IWorkflowDefinitionRuntime s,CancellationToken ct)=>Map(await s.WorkbenchAsync(new CartableWorkbenchQuery(new CartableTaskQuery(search,page??1,pageSize??20,taskId,stageKey,customerId,fromDate,toDate,outcome,status,sort,descending??true,starredOnly??false,aging,priority,followUp),historical??false),ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapGet("/definition/runtime/workbench/{id:guid}",async(Guid id,bool?historical,IWorkflowDefinitionRuntime s,CancellationToken ct)=>Map(await s.WorkbenchDetailAsync(id,historical??false,ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapGet("/definition/runtime/workbench/{id:guid}/history",async(Guid id,IWorkflowDefinitionRuntime s,CancellationToken ct)=>Map(await s.WorkbenchHistoryAsync(id,ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapGet("/definition/runtime/saved-filters",async(CartableSavedFilterTab tab, ICartableSavedFilterAdministration s,CancellationToken ct)=>Map(await s.ListAsync(tab,ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapPost("/definition/runtime/saved-filters",async(SaveCartableFilterCommand c, ICartableSavedFilterAdministration s,CancellationToken ct)=>Map(await s.CreateAsync(c,ct),true)).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapPut("/definition/runtime/saved-filters/{id:guid}",async(Guid id,UpdateCartableFilterCommand c, ICartableSavedFilterAdministration s,CancellationToken ct)=>Map(await s.UpdateAsync(id,c,ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
  r.MapDelete("/definition/runtime/saved-filters/{id:guid}",async(Guid id,long version,ICartableSavedFilterAdministration s,CancellationToken ct)=>Map(await s.DeleteAsync(id,version,ct))).RequireAuthorization(SecurityPermissions.WorkflowRead);
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
 private static IResult Map(Result r)=>r.IsSuccess?Results.NoContent():Results.Problem(ApiProblemDetailsMapper.Create(r.Error,"workflow",null));
 private static IResult Map<T>(Result<T>r,bool created=false)=>r.IsSuccess?Results.Json(r.Value,statusCode:created?201:200):Results.Problem(ApiProblemDetailsMapper.Create(r.Error,"workflow",null));
}
