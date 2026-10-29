# ساخت آیکن‌های PWA و شخصیت پیرنگ‌خان (placeholder تا تصویر واقعی جایگزین شود)
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$icons = Join-Path $root "BARANG.B2C.Web\wwwroot\icons"
$img = Join-Path $root "BARANG.B2C.Web\wwwroot\images"
New-Item -ItemType Directory -Force -Path $icons | Out-Null
New-Item -ItemType Directory -Force -Path $img | Out-Null

$dark = [System.Drawing.Color]::FromArgb(255, 19, 26, 42)
$colors = @(
    [System.Drawing.Color]::FromArgb(255, 248, 113, 113),  # red
    [System.Drawing.Color]::FromArgb(255, 251, 191, 36),   # yellow
    [System.Drawing.Color]::FromArgb(255, 96, 165, 250),   # blue
    [System.Drawing.Color]::FromArgb(255, 74, 222, 128)    # green
)

# آیکن‌های استاندارد
function New-AppIcon {
    param([int]$size, [string]$path, [bool]$maskable)
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $bg = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 246, 247, 251))
    $g.FillRectangle($bg, 0, 0, $size, $size)
    $bg.Dispose()
    $padD = 0.14
    if (-not $maskable) { $padD = 0.06 }
    $pad = [double]$padD
    $inner = [int]([double]$size * (1.0 - (2.0 * $pad)))
    $x0 = [int]([double]$size * $pad)
    $barW = [int]([math]::Ceiling([double]$inner / 4.0))
    for ($i = 0; $i -lt 4; $i++) {
        $b = New-Object System.Drawing.SolidBrush($colors[$i])
        $g.FillRectangle($b, [int]($x0 + $i * $barW), $x0, $barW + 1, $inner)
        $b.Dispose()
    }
    $d = [int]([double]$inner * 0.66)
    $off = [int](($inner - $d) / 2)
    $white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $g.FillEllipse($white, $x0 + $off, $x0 + $off, $d, $d)
    $white.Dispose()
    $font = New-Object System.Drawing.Font("Tahoma", [single]($inner * 0.40), [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $darkBrush = New-Object System.Drawing.SolidBrush($dark)
    $fmt = New-Object System.Drawing.StringFormat
    $fmt.Alignment = [System.Drawing.StringAlignment]::Center
    $fmt.LineAlignment = [System.Drawing.StringAlignment]::Center
    $rect = New-Object System.Drawing.RectangleF([single]$x0, [single]($x0 + $inner * 0.06), [single]$inner, [single]($inner * 0.88))
    $g.DrawString("پ", $font, $darkBrush, $rect, $fmt)
    $fmt.Dispose(); $font.Dispose(); $darkBrush.Dispose()
    $g.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "icon: $path"
}

New-AppIcon 192 (Join-Path $icons "icon-192.png") $false
New-AppIcon 512 (Join-Path $icons "icon-512.png") $false
# maskable با حاشیه امن
New-AppIcon 512 (Join-Path $icons "maskable-512.png") $true
# apple touch (روی پس‌زمینه تیره)
$apple = New-Object System.Drawing.Bitmap(180, 180)
$ag = [System.Drawing.Graphics]::FromImage($apple)
$ag.Clear($dark)
$src = [System.Drawing.Image]::FromFile((Join-Path $icons "icon-192.png"))
$ag.DrawImage($src, 10, 10, 160, 160)
$ag.Dispose(); $src.Dispose()
$apple.Save((Join-Path $icons "apple-touch-icon.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$apple.Dispose()
Write-Host "apple-touch-icon ok"

# ---------- شخصیت پیرنگ‌خان (placeholder) ----------
$S = 512
$m = New-Object System.Drawing.Bitmap($S, $S)
$g = [System.Drawing.Graphics]::FromImage($m)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

function Ell($gr, [System.Drawing.Color]$col, [float]$x, [float]$y, [float]$w, [float]$h) {
    $b = New-Object System.Drawing.SolidBrush($col)
    $gr.FillEllipse($b, $x, $y, $w, $h)
    $b.Dispose()
}
# بدن
Ell $g ([System.Drawing.Color]::FromArgb(255, 46, 52, 84)) 156 300 200 190
# صورت
Ell $g ([System.Drawing.Color]::FromArgb(255, 255, 221, 184)) 146 120 220 210
# کلاه چهاررنگ (نیم‌دایره با ۴ قطاع)

for ($i = 0; $i -lt 4; $i++) {
    $b = New-Object System.Drawing.SolidBrush($colors[$i])
    $g.FillPie($b, [single]120, [single]52, [single]272, [single]150, [single](180 + ($i * 45)), [single]45)
    $b.Dispose()
}
Ell $g ([System.Drawing.Color]::FromArgb(255, 19, 26, 42)) 108 178 296 26
    $capBorder = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 19, 26, 42), [single]6)
    $g.DrawArc($capBorder, [single]108, [single]178, [single]296, [single]26, [single]0, [single]180)
    $capBorder.Dispose()
# چشم‌ها
Ell $g $dark 208 190 26 34
Ell $g $dark 282 190 26 34
# لپ‌ها
Ell $g ([System.Drawing.Color]::FromArgb(255, 250, 176, 156)) 176 244 40 24
Ell $g ([System.Drawing.Color]::FromArgb(255, 250, 176, 156)) 300 244 40 24
# لبخند
$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 19, 26, 42), 8)
$pen.StartCap = 'Round'; $pen.EndCap = 'Round'
$g.DrawArc($pen, [single]220, [single]232, [single]80, [single]60, [single]20, [single]140)
# دست‌ها
Ell $g ([System.Drawing.Color]::FromArgb(255, 255, 221, 184)) 118 330 52 52
Ell $g ([System.Drawing.Color]::FromArgb(255, 255, 221, 184)) 344 330 52 52
$pen2 = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 19, 26, 42), 6)
$g.DrawLine($pen2, 120, 356, 88, 330)
$g.DrawLine($pen2, 396, 356, 428, 330)
$g.Dispose()
$m.Save((Join-Path $img "pirang-khan.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$m.Dispose()
Write-Host "mascot: $img\pirang-khan.png"
