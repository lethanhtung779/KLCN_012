# Thiet lap gui email xac minh qua Brevo (SMTP mien phi, khong can bat 2 buoc Gmail)
$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$ApiDir = Join-Path $Root "QLThiTN\QLThiTN.API"
$Example = Join-Path $ApiDir "appsettings.Email.example.json"
$Target = Join-Path $ApiDir "appsettings.Email.json"

if (-not (Test-Path $Example)) {
    Write-Host "Khong tim thay $Example" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $Target)) {
    Copy-Item $Example $Target
    Write-Host ">> Da tao: appsettings.Email.json" -ForegroundColor Green
} else {
    Write-Host ">> File da ton tai: appsettings.Email.json (giu nguyen)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Cac buoc tren Brevo (1 lan) ===" -ForegroundColor Cyan
Write-Host "1. Dang ky mien phi: https://www.brevo.com/"
Write-Host "2. Menu: Settings -> Senders -> Add a sender -> xac minh email (FromAddress trong file JSON)"
Write-Host "3. Menu: SMTP & API -> SMTP -> Create SMTP key -> copy key vao SmtpPassword"
Write-Host "4. Sua file:" -ForegroundColor White
Write-Host "   $Target"
Write-Host "   - SmtpUser     : copy CHINH XAC o 'Login' (tab SMTP) — co the la xxx@smtp-brevo.com, KHONG doan Gmail"
Write-Host "   - SmtpPassword : SMTP key (khong phai mat khau dang nhap web)"
Write-Host "   - FromAddress  : trung email da xac minh o buoc 2"
Write-Host ""
Write-Host "5. Khoi dong lai API, dang ky tren web -> kiem tra hop thu (ca Spam)."
Write-Host ""

$open = Read-Host "Mo file cau hinh bang Notepad? (y/n)"
if ($open -eq "y" -or $open -eq "Y") {
    notepad $Target
}
