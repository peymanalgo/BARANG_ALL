[CmdletBinding()]
param(
    [string]$BaseUrl = $(if ($env:BARANG_ACCEPTANCE_API) { $env:BARANG_ACCEPTANCE_API } else { 'http://127.0.0.1:5869' }),
    [Guid]$CompanyId = $(if ($env:BARANG_ACCEPTANCE_COMPANY_ID) { [Guid]$env:BARANG_ACCEPTANCE_COMPANY_ID } else { throw 'BARANG_ACCEPTANCE_COMPANY_ID is required.' }),
    [string]$Username = $(if ($env:BARANG_ACCEPTANCE_USERNAME) { $env:BARANG_ACCEPTANCE_USERNAME } else { throw 'BARANG_ACCEPTANCE_USERNAME is required.' }),
    [string]$Password = $(if ($env:BARANG_ACCEPTANCE_PASSWORD) { $env:BARANG_ACCEPTANCE_PASSWORD } else { throw 'BARANG_ACCEPTANCE_PASSWORD is required.' })
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-Api([string]$Method, [string]$Path, $Body = $null) {
    $params = @{ Method = $Method; Uri = ($BaseUrl.TrimEnd('/') + $Path); Headers = $script:Headers; ContentType = 'application/json' }
    if ($null -ne $Body) { $params.Body = $Body | ConvertTo-Json -Depth 20 -Compress }
    try { return Invoke-RestMethod @params }
    catch {
        $message = if ($null -ne $_.ErrorDetails -and $null -ne $_.ErrorDetails.Message) { $_.ErrorDetails.Message } else { $_.Exception.Message }
        throw "${Method} ${Path} failed: $message"
    }
}

function Upload-File([string]$Path) {
    $file = Get-Item -LiteralPath $Path
    $client = [System.Net.Http.HttpClient]::new()
    $client.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', ($script:Headers.Authorization -replace '^Bearer ', ''))
    $client.DefaultRequestHeaders.Add('X-Company-Id', [string]$CompanyId)
    $form = [System.Net.Http.MultipartFormDataContent]::new()
    $content = [System.Net.Http.StreamContent]::new($file.OpenRead())
    $content.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse('image/png')
    $form.Add($content, 'file', $file.Name)
    $response = $client.PostAsync(($BaseUrl.TrimEnd('/') + '/api/v1/files/upload'), $form).GetAwaiter().GetResult()
    $body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    if (-not $response.IsSuccessStatusCode) { throw "POST /api/v1/files/upload failed: $body" }
    $body | ConvertFrom-Json
}

$login = Invoke-RestMethod -Method Post -Uri ($BaseUrl.TrimEnd('/') + '/api/v1/auth/login') -ContentType 'application/json' -Body (@{ username = $Username; password = $Password } | ConvertTo-Json)
$script:Headers = @{ Authorization = "Bearer $($login.accessToken)"; 'X-Company-Id' = $CompanyId }
Invoke-Api Post '/api/v1/auth/me/company' @{ companyId = $CompanyId } | Out-Null
$me = Invoke-Api Get '/api/v1/auth/me'
$actorId = [Guid]$me.userId

$suffix = [Guid]::NewGuid().ToString('N').Substring(0, 10).ToUpperInvariant()
$branch = Invoke-Api Post '/api/v1/organization/branches/' @{ code = "S9B$suffix"; displayName = 'STEP9 isolated production branch' }
$warehouse = Invoke-Api Post '/api/v1/organization/warehouses/' @{ code = "S9W$suffix"; name = 'STEP9 isolated production warehouse'; type = 0; branchId = $branch.id }
$customer = Invoke-Api Post '/api/v1/customers/' @{ customerCode = "S9C$suffix"; customerType = 'Organization'; displayName = 'STEP9 isolated customer'; legalName = 'STEP9 isolated customer LLC' }
$product = Invoke-Api Post '/api/v1/products/' @{ productCode = "S9P$suffix"; name = 'STEP9 production product'; productTypeCode = 'APPAREL'; unitOfMeasureCode = 'PCS' }
$variant = Invoke-Api Post "/api/v1/products/$($product.id)/variants" @{ sku = "S9SKU$suffix"; displayName = 'STEP9 shared stock variant' }
$equipment = Invoke-Api Post '/api/v1/equipment/' @{ equipmentCode = "S9E$suffix"; name = 'STEP9 press'; equipmentTypeCode = 'PRESS'; operationalStatusCode = 'AVAILABLE'; branchId = $branch.id }
Invoke-Api Post '/api/v1/pricing/tariffs' @{ tariffType = 0; code = "S9T$suffix"; productVariantId = $variant.id; amount = 100; currencyCode = 'IRR'; validFrom = (Get-Date).ToString('yyyy-MM-dd') } | Out-Null
Invoke-Api Post '/api/v1/pricing/tariffs' @{ tariffType = 1; code = "S9PRINT$suffix"; printPositionCode = 'FRONT'; printMethodCode = 'SCREEN'; amount = 10; currencyCode = 'IRR'; validFrom = (Get-Date).ToString('yyyy-MM-dd') } | Out-Null
$artworkId = if ($env:BARANG_STEP9_ARTWORK_ID) { [Guid]$env:BARANG_STEP9_ARTWORK_ID } else { [Guid]'4d72f0c0-d8f0-4ea9-bfc1-9da578b12bc1' }
$artworkVersionId = if ($env:BARANG_STEP9_ARTWORK_VERSION_ID) { [Guid]$env:BARANG_STEP9_ARTWORK_VERSION_ID } else { [Guid]'5dc25e80-62d9-4144-9663-4f7d076e3160' }
$fileVersionId = if ($env:BARANG_STEP9_FILE_VERSION_ID) { [Guid]$env:BARANG_STEP9_FILE_VERSION_ID } else { [Guid]'681336b6-d7de-4a24-bf07-6bedcb5f2dc5' }
$artwork = [pscustomobject]@{ id = $artworkId }
$artworkVersion = [pscustomobject]@{ id = $artworkVersionId; fileVersionId = $fileVersionId }
$upload = [pscustomobject]@{ fileId = [Guid]::Empty; fileVersionId = $fileVersionId }

# Stock is posted through the supported inventory movement API; no database seed is used.
Invoke-Api Post '/api/v1/inventory/adjustments/true' @{ warehouseId = $warehouse.id; productVariantId = $variant.id; quantity = 20; reference = "STEP9-STOCK-$suffix"; reasonCode = 'COUNT'; uomCode = 'PCS' } | Out-Null
Invoke-Api Post "/api/v1/inventory/warehouses/$($warehouse.id)/responsibilities" @{ applicationUserId = $actorId } | Out-Null

$order = Invoke-Api Post '/api/v1/orders/' @{ branchId = $branch.id; customerId = $customer.id; source = 'Internal'; orderDate = (Get-Date).ToString('yyyy-MM-dd') }
$item1 = Invoke-Api Post "/api/v1/orders/$($order.id)/items" @{ productId = $product.id; productVariantId = $variant.id; quantity = 2 }
$item2 = Invoke-Api Post "/api/v1/orders/$($order.id)/items" @{ productId = $product.id; productVariantId = $variant.id; quantity = 3 }
Invoke-Api Post "/api/v1/orders/$($order.id)/items/$($item1.id)/print-operations" @{ sequence = 1; printPositionCode = 'FRONT'; printMethodCode = 'SCREEN'; width = 10; height = 10; artworkVersionId = $artworkVersion.id; fileVersionId = $upload.fileVersionId } | Out-Null
Invoke-Api Post "/api/v1/orders/$($order.id)/items/$($item2.id)/print-operations" @{ sequence = 1; printPositionCode = 'FRONT'; printMethodCode = 'SCREEN'; width = 10; height = 10; artworkVersionId = $artworkVersion.id; fileVersionId = $upload.fileVersionId } | Out-Null
$current = Invoke-Api Get "/api/v1/orders/$($order.id)"
$submitted = Invoke-Api Post "/api/v1/orders/$($order.id)/submit" @{ version = $current.version }
$reservation = Invoke-Api Post "/api/v1/inventory/orders/$($order.id)/reservations" @{ warehouseId = $warehouse.id; idempotencyKey = "STEP9-RES-$($order.id)" }
$issueItems = @($reservation.reservations | ForEach-Object { @{ orderItemId = $_.orderItemId; quantity = $_.requestedQuantity; uomCode = $_.uomCode } })
$issue = Invoke-Api Post "/api/v1/inventory/orders/$($order.id)/issues" @{ warehouseId = $warehouse.id; reference = "STEP9-ISSUE-$($order.id)"; items = $issueItems }

$fixture = [ordered]@{ companyId = $CompanyId; actorUserId = $actorId; branchId = [Guid]$branch.id; warehouseId = [Guid]$warehouse.id; productId = [Guid]$product.id; variantId = [Guid]$variant.id; equipmentId = [Guid]$equipment.id; artworkId = [Guid]$artwork.id; artworkVersionId = [Guid]$artworkVersion.id; fileId = [Guid]$upload.fileId; fileVersionId = [Guid]$upload.fileVersionId; orderId = [Guid]$order.id; itemIds = @([Guid]$item1.id, [Guid]$item2.id); reservationIds = @($reservation.reservations | ForEach-Object { [Guid]$_.id }); issueTransactionIds = @($issue.transactions | ForEach-Object { [Guid]$_.id }); source = 'supported-api'; createdAtUtc = [DateTimeOffset]::UtcNow }
$fixture | ConvertTo-Json -Depth 10
