# پUBLISH فروشگاه B2C برای استقرار روی VPS ویندوزی
# اجرا: ./publish.ps1   (خروجی در پوشه publish/store کنار اسکریپت)
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "BARANG.B2C.Web\BARANG.B2C.Web.csproj"
$out = Join-Path $PSScriptRoot "publish\store"

Write-Host "==> publish $project"
dotnet publish $project -c Release -o $out --nologo
if ($LASTEXITCODE -ne 0) { throw "publish failed" }

# web.config نمونه IIS را هم کنار خروجی بگذار (اگر IIS انتخاب شد)
Copy-Item (Join-Path $PSScriptRoot "web.config.iis") (Join-Path $out "web.config.iis.sample") -Force

Write-Host "==> done: $out"
Write-Host "خروجی را به پوشه نصب روی سرور (مثلاً C:\BARANG\store) منتقل کنید؛ برای سرویس ویندوزی install-service.ps1 روی سرور اجرا شود."
