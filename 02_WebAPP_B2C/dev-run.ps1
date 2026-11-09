# اجرای dev فروشگاه B2C روی همه کارت‌های شبکه — http://localhost:5170 و http://<LAN-IP>:5170
# اگر از قبل روی پورت در حال گوش دادن باشد، دوباره اجرا نمی‌کند (برای Startup ویندوز امن است).
$port = 5170
$exe  = 'D:\00_BARANG\02_WebAPP_B2C\BARANG.B2C.Web\bin\Debug\net10.0\BARANG.B2C.Web.exe'

if (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) {
    Write-Host "port $port is already listening - nothing to do"
    exit 0
}
if (-not (Test-Path $exe)) { throw "exe not found: $exe (run 'dotnet build' first)" }

$env:ASPNETCORE_ENVIRONMENT = 'Development'
Start-Process -FilePath $exe `
    -ArgumentList '--urls', "http://0.0.0.0:$port;http://[::1]:$port" `
    -WorkingDirectory (Split-Path $exe) `
    -WindowStyle Hidden
Write-Host "started on 0.0.0.0:$port (Development)"
