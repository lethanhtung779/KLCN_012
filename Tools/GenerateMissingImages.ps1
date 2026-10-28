Add-Type -AssemblyName System.Drawing

$bmp = New-Object System.Drawing.Bitmap 280, 90
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(243, 244, 246))
$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(209, 213, 219), 2)
$g.DrawRectangle($pen, 1, 1, 277, 87)
$font = New-Object System.Drawing.Font('Segoe UI', 10, [System.Drawing.FontStyle]::Regular)
$brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(107, 114, 128))
$text = '[Hinh anh minh hoa]'
$size = $g.MeasureString($text, $font)
$g.DrawString($text, $font, $brush, ((280 - $size.Width) / 2), ((90 - $size.Height) / 2))
$ms = New-Object System.IO.MemoryStream
$bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
$bytes = $ms.ToArray()
$bmp.Dispose()
$g.Dispose()

$paths = & sqlcmd -S 'localhost' -d ThiTracNghiem -E -h -1 -W -Q 'SET NOCOUNT ON; SELECT DISTINCT DuongDan FROM HinhAnh WHERE DuongDan IS NOT NULL'
$count = 0
$root = "E:\hoc tap\hoc\KLCN\KLCN_012-main\QLThiTN\tai_nguyen"

foreach ($p in ($paths -split "`r?`n")) {
    $p = $p.Trim()
    if (-not $p) { continue }
    $target = Join-Path $root $p.Replace('/', '\')
    $dir = Split-Path $target -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    if (-not (Test-Path $target)) {
        [System.IO.File]::WriteAllBytes($target, $bytes)
        $count++
    }
}
Write-Host "Created $count placeholder image files in $root"

