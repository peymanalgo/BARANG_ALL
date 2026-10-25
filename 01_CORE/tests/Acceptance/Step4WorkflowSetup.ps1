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
$positionRows = @(Invoke-Json GET '/api/v1/workflows/definition/positions?limit=200' $null $headers)
if ($positionRows.Count -eq 1 -and $positionRows[0] -is [array]) { $positionRows = @($positionRows[0]) }
$positionsByCode = @{}
foreach ($position in $positionRows) { $positionsByCode[[string]$position.code] = [Guid]$position.id }
$mappedNodes = @($fixtureObject.nodes | ForEach-Object {
    $node = $_ | Select-Object *
    if ($null -ne $_.positionId -and $null -ne $fixtureObject.positionCodes) {
        $legacyKey = [string]$_.positionId
        $codeProperty = $fixtureObject.positionCodes.PSObject.Properties[$legacyKey]
        if ($null -ne $codeProperty -and $positionsByCode.ContainsKey([string]$codeProperty.Value)) {
            $node.positionId = [string]$positionsByCode[[string]$codeProperty.Value]
        }
    }
    $node
})
$mappedPositionCodes = @{}
if ($null -ne $fixtureObject.positionCodes) {
    foreach ($mapping in $fixtureObject.positionCodes.PSObject.Properties) {
        if ($positionsByCode.ContainsKey([string]$mapping.Value)) {
            $mappedPositionCodes[[string]$positionsByCode[[string]$mapping.Value]] = [string]$mapping.Value
        }
    }
}
$importFixture = [pscustomobject]@{
    code = $fixtureObject.code; name = $fixtureObject.name; changeNote = $fixtureObject.changeNote
    nodes = $mappedNodes; transitions = @($fixtureObject.transitions); positionCodes = $mappedPositionCodes
}
Write-Host "Importing workflow graph with $(@($importFixture.nodes).Count) nodes and $(@($importFixture.transitions).Count) transitions"
$imported = $null
try { $imported = Invoke-Json POST '/api/v1/workflows/definition/import' $importFixture $headers }
catch {
    if ($_.ErrorDetails.Message -notmatch 'workflow\.definition\.already_exists') { throw }
    $imported = Invoke-Json GET '/api/v1/workflows/definition' $null $headers
}
$imported = @($imported)[0]
$current = @($imported.current)[0]
$versionId = [Guid]$current.id

$typeMap = @{ Start = 0; End = 1; UserTask = 2; ExclusiveGateway = 3; ParallelGateway = 4 }
$nodes = @($current.nodes | Where-Object { $_.key -ne 'InventoryIssue' } | ForEach-Object {
    [pscustomobject]@{ key = $_.key; type = if ($_.type -is [string]) { $typeMap[[string]$_.type] } else { [int]$_.type }; title = $_.title; description = $_.description; positionId = $_.positionId; x = $_.x; y = $_.y }
})
$nodes += [pscustomobject]@{
    key = 'InventoryIssue'; type = 2
    title = [pscustomobject]@{ fa = 'صدور انبار'; en = 'Inventory issue'; ar = 'صرف المخزون' }
    description = [pscustomobject]@{ fa = 'تحویل واقعی از انبار به تولید'; en = 'Physical warehouse-to-production handover'; ar = 'تسليم فعلي من المستودع إلى الإنتاج' }
    # InventoryIssue is a warehouse-to-production handover and must be assigned
    # through the company-scoped warehouse responsibility policy.  The isolated
    # fixture creates the canonical WHSE position; do not leave this node
    # unassigned or silently fall back to administrator.
    positionId = if ($positionsByCode.ContainsKey('WHSE')) { [string]$positionsByCode['WHSE'] } else { throw 'Required WHSE position is not configured in the selected company.' }; x = 1680; y = 300
}
$transitions = @($current.transitions | Where-Object {
    -not (($_.fromKey -eq 'DECIDE_WAREHOUSE' -and $_.toKey -eq 'PRINT') -or
          ($_.fromKey -eq 'DECIDE_WAREHOUSE' -and $_.toKey -eq 'InventoryIssue') -or
          ($_.fromKey -eq 'InventoryIssue' -and $_.toKey -eq 'PRINT'))
})
$transitions += [pscustomobject]@{ fromKey = 'DECIDE_WAREHOUSE'; toKey = 'InventoryIssue'; fieldCode = $null; operator = $null; value = $null; isDefault = $false; sortOrder = 10; outcomeCode = 'Approve'; sourceAnchor = 'Right'; targetAnchor = 'LeftCenter'; title = [pscustomobject]@{ fa = 'تأیید'; en = 'Approve'; ar = 'موافقة' } }
$transitions += [pscustomobject]@{ fromKey = 'InventoryIssue'; toKey = 'PRINT'; fieldCode = $null; operator = $null; value = $null; isDefault = $false; sortOrder = 10; outcomeCode = $null; sourceAnchor = 'Right'; targetAnchor = 'LeftCenter'; title = [pscustomobject]@{ fa = 'تحویل شد'; en = 'Issued'; ar = 'تم الصرف' } }
$draft = [pscustomobject]@{ versionId = $versionId; name = $fixtureObject.name; changeNote = $fixtureObject.changeNote; nodes = $nodes; transitions = $transitions }
Write-Host "Saving candidate graph with $(@($draft.nodes).Count) nodes and $(@($draft.transitions).Count) transitions"
$saved = Invoke-Json PUT '/api/v1/workflows/definition/draft' $draft $headers
$saved = @($saved)[0]
$candidateId = [Guid](@($saved.current)[0].id)
$validation = Invoke-Json GET "/api/v1/workflows/definition/validate/$candidateId" $null $headers
if (@($validation).Count -ne 0) { throw "Candidate graph validation returned: $($validation | ConvertTo-Json -Depth 20 -Compress)" }
$published = Invoke-Json POST "/api/v1/workflows/definition/publish/$candidateId" $null $headers
$published.current | Select-Object id,number,status,version,nodes,transitions | ConvertTo-Json -Depth 30
