[CmdletBinding()]
param(
    [string]$BaseUrl = $(if ($env:BARANG_ACCEPTANCE_API) { $env:BARANG_ACCEPTANCE_API } else { 'http://127.0.0.1:5869' }),
    [Guid]$CompanyId = $(if ($env:BARANG_ACCEPTANCE_COMPANY_ID) { [Guid]$env:BARANG_ACCEPTANCE_COMPANY_ID } else { throw 'BARANG_ACCEPTANCE_COMPANY_ID is required.' }),
    [string]$Username = $(if ($env:BARANG_ACCEPTANCE_USERNAME) { $env:BARANG_ACCEPTANCE_USERNAME } else { throw 'BARANG_ACCEPTANCE_USERNAME is required.' }),
    [string]$Password = $(if ($env:BARANG_ACCEPTANCE_PASSWORD) { $env:BARANG_ACCEPTANCE_PASSWORD } else { throw 'BARANG_ACCEPTANCE_PASSWORD is required.' }),
    [switch]$PrepareOnly
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function Invoke-Api([string]$Method,[string]$Path,$Body=$null) {
    $p=@{Method=$Method;Uri=($BaseUrl.TrimEnd('/')+$Path);Headers=$script:Headers;ContentType='application/json'}
    if($null -ne $Body){$p.Body=$Body|ConvertTo-Json -Depth 20 -Compress}
    try{return Invoke-RestMethod @p}catch{$m=if($_.ErrorDetails -and $_.ErrorDetails.Message){$_.ErrorDetails.Message}else{try{$reader=[IO.StreamReader]::new($_.Exception.Response.GetResponseStream());$v=$reader.ReadToEnd();$reader.Dispose();$v}catch{$_.Exception.ToString()}};throw "${Method} ${Path} failed: $m"}
}
$login=Invoke-RestMethod -Method Post -Uri ($BaseUrl.TrimEnd('/')+'/api/v1/auth/login') -ContentType 'application/json' -Body (@{username=$Username;password=$Password}|ConvertTo-Json)
$script:Headers=@{Authorization="Bearer $($login.accessToken)";'X-Company-Id'=$CompanyId}
Invoke-Api Post '/api/v1/auth/me/company' @{companyId=$CompanyId}|Out-Null
$me=Invoke-Api Get '/api/v1/auth/me';$suffix=(Get-Date).ToString('yyyyMMddHHmmss')
$branch=Invoke-Api Post '/api/v1/organization/branches/' @{code="Q10$suffix";displayName='STEP10 Quality branch'}
$warehouse=Invoke-Api Post '/api/v1/organization/warehouses/' @{code="QW$suffix";name='STEP10 Quality warehouse';type=0;branchId=$branch.id}
$customer=Invoke-Api Post '/api/v1/customers/' @{customerCode="QC$suffix";customerType='Organization';displayName='STEP10 Quality customer';legalName='STEP10 Quality customer'}
$product=Invoke-Api Post '/api/v1/products/' @{productCode="QP$suffix";name='STEP10 Quality product';productTypeCode='APPAREL';unitOfMeasureCode='PCS'}
$variant=Invoke-Api Post "/api/v1/products/$($product.id)/variants" @{sku="QSKU$suffix";displayName='STEP10 Quality variant'}
$tariff=Invoke-Api Post '/api/v1/pricing/tariffs' @{tariffType=0;code="QT$suffix";productVariantId=$variant.id;amount=100;currencyCode='IRR';validFrom=(Get-Date).ToString('yyyy-MM-dd')}
$equipment=Invoke-Api Post '/api/v1/equipment/' @{equipmentCode="QEQ$suffix";name='STEP10 HeatPress';equipmentTypeCode='PRESS';operationalStatusCode='AVAILABLE';branchId=$branch.id}
Invoke-Api Post "/api/v1/inventory/warehouses/$($warehouse.id)/responsibilities" @{applicationUserId=[Guid]$me.userId}|Out-Null
Invoke-Api Post '/api/v1/inventory/adjustments/true' @{warehouseId=$warehouse.id;productVariantId=$variant.id;quantity=20;reference="QSTOCK$suffix";reasonCode='COUNT';uomCode='PCS'}|Out-Null
$order=Invoke-Api Post '/api/v1/orders/' @{branchId=$branch.id;customerId=$customer.id;source='Internal';orderDate=(Get-Date).ToString('yyyy-MM-dd')}
$item1=Invoke-Api Post "/api/v1/orders/$($order.id)/items" @{productId=$product.id;productVariantId=$variant.id;quantity=2}
$item2=Invoke-Api Post "/api/v1/orders/$($order.id)/items" @{productId=$product.id;productVariantId=$variant.id;quantity=3}
$current=Invoke-Api Get "/api/v1/orders/$($order.id)";Invoke-Api Post "/api/v1/orders/$($order.id)/submit" @{version=$current.version}|Out-Null
$reservation=Invoke-Api Post "/api/v1/inventory/orders/$($order.id)/reservations" @{warehouseId=$warehouse.id;idempotencyKey="QRES-$($order.id)"}
$issueItems=@($reservation.reservations|ForEach-Object {@{orderItemId=$_.orderItemId;quantity=$_.requestedQuantity;uomCode=$_.uomCode}})
Invoke-Api Post "/api/v1/inventory/orders/$($order.id)/issues" @{warehouseId=$warehouse.id;reference="QISS-$suffix";items=$issueItems}|Out-Null
$jobs=Invoke-Api Post "/api/v1/production/orders/$($order.id)/jobs"
$template=Invoke-Api Post '/api/v1/quality/templates' @{code="QCT$suffix";name='STEP10 Standard';applicableJobType=0;productTypeCode='APPAREL';criteria=@(@{code='VIS';name='Visual';criterionType=0;isRequired=$true;sortOrder=1;minimumValue=$null;maximumValue=$null;allowedChoiceCodes=$null})}
$job=@($jobs)|Where-Object {$_.jobType -eq 0}|Select-Object -First 1
Invoke-Api Put "/api/v1/production/jobs/$($job.id)/plan" @{priority=0;plannedStartUtc=$null;plannedEndUtc=$null;queuePosition=1;version=$job.version}|Out-Null
$job=Invoke-Api Get "/api/v1/production/jobs/$($job.id)"
Invoke-Api Put "/api/v1/production/jobs/$($job.id)/operator" @{operatorUserId=[Guid]$me.userId;version=$job.version}|Out-Null
$job=Invoke-Api Get "/api/v1/production/jobs/$($job.id)";Invoke-Api Post "/api/v1/production/jobs/$($job.id)/start" @{version=$job.version}|Out-Null
$job=Invoke-Api Get "/api/v1/production/jobs/$($job.id)";$completeQuantity=if([Guid]$job.orderItemId -eq [Guid]$item1.id){2}else{3};Invoke-Api Post "/api/v1/production/jobs/$($job.id)/complete" @{quantityCompleted=$completeQuantity;notes='STEP10 output';version=$job.version}|Out-Null
$inspection=Invoke-Api Post '/api/v1/quality/inspections' @{productionJobId=$job.id;templateId=$template.id;parentInspectionId=$null}
$criteria=$template.criteria|Select-Object -First 1;$disposition=$null;$approved=$null
if(-not $PrepareOnly){
    $disposition=Invoke-Api Put "/api/v1/quality/inspections/$($inspection.id)/disposition" @{acceptedQuantity=$completeQuantity;rejectedQuantity=0;reproductionRequiredQuantity=0;acceptedReason='Passed';rejectedReason=$null;reproductionReason=$null;version=$inspection.version}
    Invoke-Api Post "/api/v1/quality/inspections/$($inspection.id)/results" @{criterionId=$criteria.id;passFailValue=$true;numericValue=$null;textValue=$null;choiceCode=$null}|Out-Null
    $latest=Invoke-Api Get "/api/v1/quality/inspections/$($inspection.id)";$approved=Invoke-Api Post "/api/v1/quality/inspections/$($inspection.id)/approve" @{version=$latest.version}
}
[ordered]@{companyId=$CompanyId;userId=$me.userId;branchId=$branch.id;warehouseId=$warehouse.id;productId=$product.id;variantId=$variant.id;equipmentId=$equipment.id;orderId=$order.id;itemIds=@($item1.id,$item2.id);jobId=$job.id;templateId=$template.id;inspectionId=$inspection.id;disposition=$disposition;approved=$approved;source='supported-api';createdAtUtc=[DateTimeOffset]::UtcNow}|ConvertTo-Json -Depth 20
