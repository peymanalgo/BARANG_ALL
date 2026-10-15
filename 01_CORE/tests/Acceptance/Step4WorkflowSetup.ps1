[CmdletBinding()]
param(
    [string]$BaseUrl = $(if ($env:BARANG_ACCEPTANCE_API) { $env:BARANG_ACCEPTANCE_API } else { 'http://localhost:18091' }),
    [string]$CompanyId = $env:BARANG_ACCEPTANCE_COMPANY_ID,
    [string]$Username = $env:BARANG_ACCEPTANCE_USERNAME,
    [string]$Password = $env:BARANG_ACCEPTANCE_PASSWORD,
    [string]$Fixture = "$PSScriptRoot/fixtures/barang-order-workflow.v17.sanitized.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($CompanyId) -or [string]::IsNullOrWhiteSpace($Username) -or [string]::IsNullOrWhiteSpace($Password)) {
    throw 'BARANG_ACCEPTANCE_COMPANY_ID, BARANG_ACCEPTANCE_USERNAME and BARANG_ACCEPTANCE_PASSWORD are required.'
}

function Invoke-Json([string]$Method, [string]$Path, $Body = $null, [hashtable]$Headers = @{}) {
    $params = @{ Method = $Method; Uri = ($BaseUrl.TrimEnd('/') + $Path); Headers = $Headers; ContentType = 'application/json' }
    if ($null -ne $Body) { $params.Body = ($Body | ConvertTo-Json -Depth 30 -Compress) }
    Invoke-RestMethod @params
}

$login = Invoke-Json POST '/api/v1/auth/login' @{ username = $Username; password = $Password }
$headers = @{ Authorization = "Bearer $($login.accessToken)"; 'X-Company-Id' = $CompanyId }
Invoke-Json POST '/api/v1/auth/me/company' @{ companyId = $CompanyId } @{ Authorization = "Bearer $($login.accessToken)" } | Out-Null

$fixtureObject = Get-Content -Raw -LiteralPath $Fixture | ConvertFrom-Json
$imported = $null
try { $imported = Invoke-Json POST '/api/v1/workflows/definition/import' $fixtureObject $headers }
catch {
    if ($_.ErrorDetails.Message -notmatch 'workflow\.definition\.already_exists') { throw }
    $imported = Invoke-Json GET '/api/v1/workflows/definition' $null $headers
}
$versionId = [Guid]$imported.current.id

$typeMap = @{ Start = 0; End = 1; UserTask = 2; ExclusiveGateway = 3; ParallelGateway = 4 }
$nodes = @($imported.current.nodes | ForEach-Object {
    [pscustomobject]@{ key = $_.key; type = if ($_.type -is [string]) { $typeMap[[string]$_.type] } else { [int]$_.type }; title = $_.title; description = $_.description; positionId = $_.positionId; x = $_.x; y = $_.y }
})
$nodes += [pscustomobject]@{
    key = 'InventoryIssue'; type = 2
    title = [pscustomobject]@{ fa = 'صدور انبار'; en = 'Inventory issue'; ar = 'صرف المخزون' }
    description = [pscustomobject]@{ fa = 'تحویل واقعی از انبار به تولید'; en = 'Physical warehouse-to-production handover'; ar = 'تسليم فعلي من المستودع إلى الإنتاج' }
    positionId = $null; x = 1680; y = 300
}
$transitions = @($fixtureObject.transitions | Where-Object { -not ($_.fromKey -eq 'DECIDE_WAREHOUSE' -and $_.toKey -eq 'PRINT') })
$transitions += [pscustomobject]@{ fromKey = 'DECIDE_WAREHOUSE'; toKey = 'InventoryIssue'; fieldCode = $null; operator = $null; value = $null; isDefault = $false; sortOrder = 10; outcomeCode = 'Approve'; sourceAnchor = 'Right'; targetAnchor = 'LeftCenter'; title = [pscustomobject]@{ fa = 'تأیید'; en = 'Approve'; ar = 'موافقة' } }
$transitions += [pscustomobject]@{ fromKey = 'InventoryIssue'; toKey = 'PRINT'; fieldCode = $null; operator = $null; value = $null; isDefault = $false; sortOrder = 10; outcomeCode = $null; sourceAnchor = 'Right'; targetAnchor = 'LeftCenter'; title = [pscustomobject]@{ fa = 'تحویل شد'; en = 'Issued'; ar = 'تم الصرف' } }
$draft = [pscustomobject]@{ versionId = $versionId; name = $fixtureObject.name; changeNote = $fixtureObject.changeNote; nodes = $nodes; transitions = $transitions }
$saved = Invoke-Json PUT '/api/v1/workflows/definition/draft' $draft $headers
$candidateId = [Guid]$saved.current.id
$validation = Invoke-Json GET "/api/v1/workflows/definition/validate/$candidateId" $null $headers
if (@($validation).Count -ne 0) { throw "Candidate graph validation returned: $($validation | ConvertTo-Json -Depth 20 -Compress)" }
$published = Invoke-Json POST "/api/v1/workflows/definition/publish/$candidateId" $null $headers
$published.current | Select-Object id,number,status,version,nodes,transitions | ConvertTo-Json -Depth 30
