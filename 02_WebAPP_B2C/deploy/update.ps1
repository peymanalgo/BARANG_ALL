# به‌روزرسانی فروشگاه روی سرور: سرویس را می‌بندد، فایل‌های publish را کپی و سرویس را بالا می‌آورد
# اجرا روی VPS: ./update.ps1 -PublishDir D:\staging\store -InstallDir C:\BARANG\store
param(
    [string]$ServiceName = "BARANG.Store",
    [string]$PublishDir = "$(Split-Path -Parent $PSScriptRoot)\deploy\publish\store",
    [string]$InstallDir = "C:\BARANG\store"
)
$ErrorActionPreference = "Stop"

if (-not (Test-Path (Join-Path $PublishDir "BARANG.B2C.Web.dll"))) { throw "publish dir نامعتبر: $PublishDir" }

$running = (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) -ne $null
if ($running) { sc.exe stop $ServiceName | Out-Null; Start-Sleep -Seconds 4 }

# appsettings سرور را حفظ کن
$keep = Join-Path $InstallDir "appsettings.Production.json"
$kept = $null
if (Test-Path $keep) { $kept = Get-Content $keep -Raw }

New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
Copy-Item (Join-Path $PublishDir "*") $InstallDir -Recurse -Force

# تنظیمات محلی سرور را برگردان (فایل publish را بازنویسی نکن)
if ($kept) { Set-Content -Path $keep -Value $kept -Encoding UTF8 }

if ($running) { sc.exe start $ServiceName; Start-Sleep -Seconds 4 }
Get-Service $ServiceName -ErrorAction SilentlyContinue | Format-List Name, Status
Write-Host "به‌روزرسانی انجام شد. سلامت: /health"
