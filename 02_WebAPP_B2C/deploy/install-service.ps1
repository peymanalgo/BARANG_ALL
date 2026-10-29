# نصب/به‌روزرسانی فروشگاه B2C به‌صورت Windows Service روی سرور ویندوزی
# اجرا (روی VPS، PowerShell Administrator):
#   ./install-service.ps1 -InstallDir C:\BARANG\store -Port 5180
# سپس در IIS سایت/پروکسی را به http://127.0.0.1:5180 وصل کنید (VPS-DEPLOY.md)
param(
    [string]$ServiceName = "BARANG.Store",
    [string]$DisplayName = "BARANG B2C Store",
    [string]$InstallDir = "C:\BARANG\store",
    [int]$Port = 5180
)
$ErrorActionPreference = "Stop"

$exe = Join-Path $InstallDir "BARANG.B2C.Web.exe"
if (-not (Test-Path $exe)) { throw "exe پیدا نشد: $exe (ابتدا publish.ps1 و کپی فایل‌ها)" }

# پوشه لاگ stdout برای سناریوی IIS
New-Item -ItemType Directory -Force -Path (Join-Path $InstallDir "logs") | Out-Null

$exists = (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) -ne $null
if ($exists) {
    Write-Host "==> توقف و حذف سرویس قبلی"
    sc.exe stop $ServiceName | Out-Null
    Start-Sleep -Seconds 3
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

Write-Host "==> ساخت سرویس $ServiceName"
sc.exe create $ServiceName binPath= "`"$exe`" --urls http://127.0.0.1:$Port --environment Production" start= auto DisplayName= "$DisplayName"
if ($LASTEXITCODE -ne 0) { throw "sc create failed" }

# بازیابی خودکار در صورت کرش
sc.exe failure $ServiceName reset= 86400 actions= restart/10000/restart/30000/restart/60000 | Out-Null
sc.exe description $ServiceName "BARANG B2C storefront (Razor Pages SSR)" | Out-Null

Write-Host "==> start"
sc.exe start $ServiceName
Start-Sleep -Seconds 4
Get-Service $ServiceName | Format-List Name, Status
Write-Host "سلامت: http://127.0.0.1:$Port/health"
