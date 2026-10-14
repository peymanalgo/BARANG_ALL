param(
    [string]$BaseUrl = $env:STEP3_BASE_URL,
    [string]$Token = $env:STEP3_ACCESS_TOKEN,
    [string]$CompanyId = '11111111-1111-1111-1111-111111111111',
    [string]$WarehouseId = '66666666-6666-6666-6666-666666666666',
    [string]$OrderId = '77777777-7777-7777-7777-777777777778',
    [string]$IdempotencyKey = 'step3-acceptance-retry'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($BaseUrl) -or [string]::IsNullOrWhiteSpace($Token)) { throw 'Set STEP3_BASE_URL and STEP3_ACCESS_TOKEN; never commit credentials.' }
$headers = @{ Authorization = "Bearer $Token"; 'X-Company-Id' = $CompanyId }
$body = @{ warehouseId = $WarehouseId; idempotencyKey = $IdempotencyKey } | ConvertTo-Json

function Invoke-Step3([string]$order, [string]$json) {
    try {
        $response = Invoke-WebRequest -Method Post -Uri "$BaseUrl/api/v1/inventory/orders/$order/reservations" -Headers $headers -ContentType 'application/json' -Body $json
        return [pscustomobject]@{ Status = [int]$response.StatusCode; Body = ($response.Content | ConvertFrom-Json) }
    } catch {
        $response = $_.Exception.Response
        return [pscustomobject]@{ Status = [int]$response.StatusCode; Body = $null }
    }
}

$first = Invoke-Step3 $OrderId $body
if ($first.Status -notin 200,201) { throw "reservation failed with HTTP $($first.Status)" }
$retry = Invoke-Step3 $OrderId $body
if ($retry.Status -notin 200,201) { throw "retry failed with HTTP $($retry.Status)" }
if (@($retry.Body.reservations).Count -ne @($first.Body.reservations).Count) { throw 'retry changed reservation cardinality' }
Write-Output "PASS reservation and retry: HTTP $($first.Status)/$($retry.Status), rows=$(@($retry.Body.reservations).Count)"
