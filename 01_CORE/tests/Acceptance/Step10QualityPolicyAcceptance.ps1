[CmdletBinding()]
param(
    [string]$BaseUrl = $(if ($env:BARANG_ACCEPTANCE_API) { $env:BARANG_ACCEPTANCE_API } else { 'http://127.0.0.1:5869' }),
    [Guid]$CompanyId = $(if ($env:BARANG_ACCEPTANCE_COMPANY_ID) { [Guid]$env:BARANG_ACCEPTANCE_COMPANY_ID } else { throw 'BARANG_ACCEPTANCE_COMPANY_ID is required.' }),
    [string]$Username = $(if ($env:BARANG_ACCEPTANCE_USERNAME) { $env:BARANG_ACCEPTANCE_USERNAME } else { throw 'BARANG_ACCEPTANCE_USERNAME is required.' }),
    [string]$Password = $(if ($env:BARANG_ACCEPTANCE_PASSWORD) { $env:BARANG_ACCEPTANCE_PASSWORD } else { throw 'BARANG_ACCEPTANCE_PASSWORD is required.' })
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function Api([string]$Method,[string]$Path,$Body=$null) {
    $p=@{Method=$Method;Uri=($BaseUrl.TrimEnd('/')+$Path);Headers=$script:Headers;ContentType='application/json'}
    if($null -ne $Body){$p.Body=$Body|ConvertTo-Json -Depth 20 -Compress}
    try{return Invoke-RestMethod @p}catch{throw "${Method} ${Path}: $($_.ErrorDetails.Message)"}
}
$login=Invoke-RestMethod -Method Post -Uri ($BaseUrl.TrimEnd('/')+'/api/v1/auth/login') -ContentType 'application/json' -Body (@{username=$Username;password=$Password}|ConvertTo-Json)
$script:Headers=@{Authorization="Bearer $($login.accessToken)";'X-Company-Id'=$CompanyId}
Api Post '/api/v1/auth/me/company' @{companyId=$CompanyId}|Out-Null
$me=Api Get '/api/v1/auth/me'
$fixture = & "$PSScriptRoot/Step10QualityFixture.ps1" -BaseUrl $BaseUrl -CompanyId $CompanyId -Username $Username -Password $Password -PrepareOnly | ConvertFrom-Json
$inspection=Api Get "/api/v1/quality/inspections/$($fixture.inspectionId)"
$quantity=[decimal]$inspection.disposition.inspectableQuantity
$accepted=[math]::Floor($quantity/2)
$replacement=$quantity-$accepted
if($accepted -le 0 -or $replacement -le 0){throw "Fixture quantity must support mixed disposition; actual=$quantity"}
Api Put "/api/v1/quality/inspections/$($fixture.inspectionId)/disposition" @{acceptedQuantity=$accepted;rejectedQuantity=0;reproductionRequiredQuantity=$replacement;acceptedReason='Accepted portion';rejectedReason=$null;reproductionReason='Replacement required';version=$inspection.disposition.version}|Out-Null
$allTemplates=@(Api Get '/api/v1/quality/templates?limit=100')
if($allTemplates.Count -eq 1 -and $allTemplates[0] -is [array]){$allTemplates=@($allTemplates[0])}
$template=$null
foreach($candidate in $allTemplates){if(([string]$candidate.id).ToLowerInvariant() -eq ([string]$fixture.templateId).ToLowerInvariant()){$template=$candidate;break}}
if($null -eq $template){throw "Template prerequisite missing for $($fixture.templateId); returned $($allTemplates.Count) templates."}
$criterion=@($template.criteria)|Select-Object -First 1
if($null -eq $criterion -or $null -eq $criterion.id){throw "Template criterion prerequisite missing for $($fixture.templateId)."}
Api Post "/api/v1/quality/inspections/$($fixture.inspectionId)/results" @{criterionId=$criterion.id;passFailValue=$true;numericValue=$null;textValue=$null;choiceCode=$null}|Out-Null
$inspection=Api Get "/api/v1/quality/inspections/$($fixture.inspectionId)"
$rejected=Api Post "/api/v1/quality/inspections/$($fixture.inspectionId)/reject" @{reason='Replacement required';version=$inspection.version}
$request=Api Post "/api/v1/quality/inspections/$($fixture.inspectionId)/reproduction" @{requestedStage=0;quantity=$replacement;reason='Approved reproduction request'}
$jobs=Api Post "/api/v1/quality/reproductions/$($request.id)/jobs" @{version=$request.version}
$replacementJobId=@($jobs)[0]
$replacementJob=Api Get "/api/v1/production/jobs/$replacementJobId"
Api Put "/api/v1/production/jobs/$replacementJobId/plan" @{priority=0;plannedStartUtc=$null;plannedEndUtc=$null;queuePosition=1;version=$replacementJob.version}|Out-Null
$replacementJob=Api Get "/api/v1/production/jobs/$replacementJobId"
Api Put "/api/v1/production/jobs/$replacementJobId/operator" @{operatorUserId=$me.userId;version=$replacementJob.version}|Out-Null
$replacementJob=Api Get "/api/v1/production/jobs/$replacementJobId"
Api Post "/api/v1/production/jobs/$replacementJobId/start" @{version=$replacementJob.version}|Out-Null
$replacementJob=Api Get "/api/v1/production/jobs/$replacementJobId"
Api Post "/api/v1/production/jobs/$replacementJobId/complete" @{quantityCompleted=$replacement;notes='Replacement output';version=$replacementJob.version}|Out-Null
$replacementInspection=Api Post '/api/v1/quality/inspections' @{productionJobId=$replacementJobId;templateId=$fixture.templateId;parentInspectionId=$fixture.inspectionId}
$replacementInspection=Api Get "/api/v1/quality/inspections/$($replacementInspection.id)"
Api Put "/api/v1/quality/inspections/$($replacementInspection.id)/disposition" @{acceptedQuantity=$replacement;rejectedQuantity=0;reproductionRequiredQuantity=0;acceptedReason='Replacement accepted';rejectedReason=$null;reproductionReason=$null;version=$replacementInspection.disposition.version}|Out-Null
Api Post "/api/v1/quality/inspections/$($replacementInspection.id)/results" @{criterionId=$criterion.id;passFailValue=$true;numericValue=$null;textValue=$null;choiceCode=$null}|Out-Null
$replacementInspection=Api Get "/api/v1/quality/inspections/$($replacementInspection.id)"
$replacementApproved=Api Post "/api/v1/quality/inspections/$($replacementInspection.id)/approve" @{version=$replacementInspection.version}
$retry=Api Post "/api/v1/quality/reproductions/$($request.id)/jobs" @{version=$request.version}
$originalReload=Api Get "/api/v1/quality/inspections/$($fixture.inspectionId)"
$sameJobReinspectionRejected=$false
try { Api Post '/api/v1/quality/inspections' @{productionJobId=$fixture.jobId;templateId=$fixture.templateId;parentInspectionId=$null}|Out-Null } catch { $sameJobReinspectionRejected=$true }
[ordered]@{orderId=$fixture.orderId;orderItemId=$fixture.itemIds[0];originalInspectionId=$fixture.inspectionId;originalStatus=$rejected.status;originalDisposition=$originalReload.disposition;reproductionRequestId=$request.id;replacementJobId=$replacementJobId;replacementInspectionId=$replacementInspection.id;replacementStatus=$replacementApproved.status;retryJobIds=@($retry);sameJobReinspectionRejected=$sameJobReinspectionRejected;source='supported-api';verifiedAtUtc=[DateTimeOffset]::UtcNow}|ConvertTo-Json -Depth 20
