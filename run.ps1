# Chay du an QLThiTN: restore DB tu .bak (LocalDB) + API + Web
# Vi du:  .\run.ps1
#         .\run.ps1 -SkipRestore
#         .\run.ps1 -WinForms

param(
    [switch]$SkipRestore,
    [switch]$WinForms,
    [switch]$SkipBuild,
    [string]$SqlServer = "(localdb)\MSSQLLocalDB",
    [string]$BackupFile = ""
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot

$ApiProj = Join-Path $Root "QLThiTN\QLThiTN.API\QLThiTN.API.csproj"
$WebProj = Join-Path $Root "QLThiTN\QLThiTN.Web\QLThiTN.Web.csproj"
$WinProj = Join-Path $Root "QLThiTN\QLThiTN.WinForms\QLThiTN.WinForms.csproj"
$ApiProjRel = "QLThiTN\QLThiTN.API\QLThiTN.API.csproj"
$WebProjRel = "QLThiTN\QLThiTN.Web\QLThiTN.Web.csproj"
$WinProjRel = "QLThiTN\QLThiTN.WinForms\QLThiTN.WinForms.csproj"
$DataDir = Join-Path $Root "Database\Data"
$HinhAnhRoot = Join-Path $Root "QLThiTN\tai_nguyen"
$ApiUrl = "http://localhost:5264"
$WebUrl = "http://localhost:7245"

function Find-SqlCmd {
    $cmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $fallback = "${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE"
    if (Test-Path $fallback) { return $fallback }
    throw "Khong tim thay sqlcmd. Cai SQL Server hoac SQL Server Command Line Utilities."
}

function Resolve-BackupPath {
    param([string]$Name)
    $candidates = @(
        $Name,
        (Join-Path $Root $Name),
        (Join-Path $Root "Database\$Name"),
        (Join-Path $Root "ThiTracNghiem_20261009_235247.bak"),
        (Join-Path $Root "Database\ThiTracNghiem_20261009_235247.bak")
    ) | Where-Object { $_ -and (Test-Path $_) }
    if (-not $candidates) {
        throw "Khong tim thay file backup .bak. Dat file ThiTracNghiem_20261009_235247.bak o thu muc goc du an."
    }
    return (Resolve-Path $candidates[0]).Path
}

function Start-LocalDb {
    param([string]$Instance)
    if ($Instance -notmatch 'localdb') { return }
    $name = if ($Instance -match '\((localdb)\)\\(.+)') { $Matches[2] } else { "MSSQLLocalDB" }
    & sqllocaldb start $name 2>$null | Out-Null
}

function Restore-Database {
    param(
        [string]$Server,
        [string]$BakPath,
        [string]$MdfPath,
        [string]$LdfPath,
        [string]$SqlCmdExe
    )

    Write-Host ">> Phuc hoi database tu: $BakPath" -ForegroundColor Cyan
    $bakEsc = $BakPath.Replace("'", "''")
    $mdfEsc = $MdfPath.Replace("'", "''")
    $ldfEsc = $LdfPath.Replace("'", "''")

    $fileList = & $SqlCmdExe -S $Server -E -h -1 -W -Q "SET NOCOUNT ON; RESTORE FILELISTONLY FROM DISK = N'$bakEsc';"
    if ($LASTEXITCODE -ne 0) { throw "RESTORE FILELISTONLY that bai. Kiem tra SQL Server: $Server" }

    $dataLogical = $null
    $logLogical = $null
    foreach ($line in ($fileList -split "`n")) {
        $t = $line.Trim()
        if (-not $t) { continue }
        if ($t -match '^(\S+)\s+(.+\.(?:mdf|ldf|ndf))\s+(D|L)\s') {
            if ($Matches[3] -eq 'D') { $dataLogical = $Matches[1] }
            if ($Matches[3] -eq 'L') { $logLogical = $Matches[1] }
        }
    }
    if (-not $dataLogical -or -not $logLogical) {
        throw "Khong doc duoc ten logical file tu file backup."
    }

    $dropSql = @"
IF DB_ID(N'ThiTracNghiem') IS NOT NULL
BEGIN
  ALTER DATABASE [ThiTracNghiem] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
  DROP DATABASE [ThiTracNghiem];
END
"@
    & $SqlCmdExe -S $Server -E -Q $dropSql | Out-Null

    $restoreSql = @"
RESTORE DATABASE [ThiTracNghiem] FROM DISK = N'$bakEsc' WITH REPLACE,
  MOVE N'$($dataLogical.Replace("'", "''"))' TO N'$mdfEsc',
  MOVE N'$($logLogical.Replace("'", "''"))' TO N'$ldfEsc';
"@
    & $SqlCmdExe -S $Server -E -Q $restoreSql
    if ($LASTEXITCODE -ne 0) { throw "RESTORE DATABASE that bai." }
    Write-Host ">> Database ThiTracNghiem san sang tren $Server" -ForegroundColor Green
}

function Sync-AppSettings {
    param(
        [string]$ConnectionString,
        [string]$ImageRoot,
        [string[]]$Paths
    )

    foreach ($path in $Paths) {
        if (-not (Test-Path $path)) { continue }
        $json = Get-Content $path -Raw -Encoding UTF8 | ConvertFrom-Json
        if (-not $json.ConnectionStrings) {
            $json | Add-Member -NotePropertyName ConnectionStrings -NotePropertyValue ([pscustomobject]@{})
        }
        $json.ConnectionStrings.ThiTracNghiem = $ConnectionString
        $json.HinhAnhRoot = $ImageRoot
        $json | ConvertTo-Json -Depth 5 | Set-Content $path -Encoding UTF8
    }
}

function Copy-AppSettingsToOutput {
    $pairs = @(
        @(
            (Join-Path $Root "QLThiTN\QLThiTN.API\appsettings.json"),
            (Join-Path $Root "QLThiTN\QLThiTN.API\bin\Debug\net10.0\appsettings.json")
        ),
        @(
            (Join-Path $Root "QLThiTN\QLThiTN.WinForms\appsettings.json"),
            (Join-Path $Root "QLThiTN\QLThiTN.WinForms\bin\Debug\net10.0-windows\appsettings.json")
        )
    )
    foreach ($pair in $pairs) {
        if (-not (Test-Path $pair[0])) { continue }
        $outDir = Split-Path $pair[1] -Parent
        if (Test-Path $outDir) {
            Copy-Item $pair[0] $pair[1] -Force
        }
    }
    $emailSrc = Join-Path $Root "QLThiTN\QLThiTN.API\appsettings.Email.json"
    $emailDst = Join-Path $Root "QLThiTN\QLThiTN.API\bin\Debug\net10.0\appsettings.Email.json"
    if ((Test-Path $emailSrc) -and (Test-Path (Split-Path $emailDst -Parent))) {
        Copy-Item $emailSrc $emailDst -Force
    }
}

function Wait-Url {
    param([string[]]$Urls, [int]$TimeoutSec = 90)
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        foreach ($Url in $Urls) {
            try {
                Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3 | Out-Null
                return
            } catch { }
        }
        Start-Sleep -Seconds 1
    }
    throw "API chua san sang (thu: $($Urls -join ', '))."
}

$apiProcess = $null
$winProcess = $null

try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw "Chua cai .NET SDK. Can .NET 10 (hoac SDK tuong thich voi du an)."
    }

    $sqlCmd = Find-SqlCmd
    Start-LocalDb -Instance $SqlServer

    New-Item -ItemType Directory -Force -Path $DataDir | Out-Null
    New-Item -ItemType Directory -Force -Path $HinhAnhRoot | Out-Null

    $conn = "Server=$SqlServer;Database=ThiTracNghiem;Trusted_Connection=True;TrustServerCertificate=True"
    Sync-AppSettings -ConnectionString $conn -ImageRoot $HinhAnhRoot -Paths @(
        (Join-Path $Root "QLThiTN\QLThiTN.API\appsettings.json"),
        (Join-Path $Root "QLThiTN\QLThiTN.WinForms\appsettings.json")
    )

    if (-not $SkipRestore) {
        $bak = Resolve-BackupPath -Name $BackupFile
        $mdf = Join-Path $DataDir "ThiTracNghiem.mdf"
        $ldf = Join-Path $DataDir "ThiTracNghiem_log.ldf"
        Restore-Database -Server $SqlServer -BakPath $bak -MdfPath $mdf -LdfPath $ldf -SqlCmdExe $sqlCmd
    } else {
        Write-Host ">> Bo qua restore (-SkipRestore)" -ForegroundColor Yellow
    }

    $patchEmail = Join-Path $Root "Database\add_dang_ky_xac_minh_email.sql"
    if (Test-Path $patchEmail) {
        Write-Host ">> Cap nhat schema (xac minh email)..." -ForegroundColor Cyan
        & $sqlCmd -S $SqlServer -E -i $patchEmail | Out-Null
    }

    Copy-AppSettingsToOutput

    if (-not $SkipBuild) {
        Write-Host ">> Build du an..." -ForegroundColor Cyan
        dotnet build $ApiProj -v q
        dotnet build $WebProj -v q
        if ($WinForms) { dotnet build $WinProj -v q }
        Copy-AppSettingsToOutput
    }

    $logDir = Join-Path $Root "logs"
    New-Item -ItemType Directory -Force -Path $logDir | Out-Null
    $apiLog = Join-Path $logDir "api.log"

    Write-Host ">> Khoi dong API ($ApiUrl)..." -ForegroundColor Cyan
    $apiArgs = @("run", "--project", $ApiProjRel, "--launch-profile", "http")
    if ($SkipBuild) { $apiArgs += "--no-build" }
    $apiProcess = Start-Process -FilePath "dotnet" -ArgumentList $apiArgs `
        -WorkingDirectory $Root -PassThru -WindowStyle Hidden `
        -RedirectStandardOutput $apiLog -RedirectStandardError (Join-Path $logDir "api.err.log")

    Wait-Url -Urls @(
        "$ApiUrl/swagger/index.html",
        "$ApiUrl/swagger/v1/swagger.json"
    ) -TimeoutSec 120

    if ($WinForms) {
        Write-Host ">> Khoi dong WinForms..." -ForegroundColor Cyan
        $winArgs = @("run", "--project", $WinProjRel)
        if ($SkipBuild) { $winArgs += "--no-build" }
        $winProcess = Start-Process -FilePath "dotnet" -ArgumentList $winArgs `
            -WorkingDirectory $Root -PassThru
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  Web:    $WebUrl" -ForegroundColor Green
    Write-Host "  API:    $ApiUrl" -ForegroundColor Green
    Write-Host "  Swagger: $ApiUrl/swagger" -ForegroundColor Green
    Write-Host "  (Ctrl+C de dung Web; script se tat API)" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""

    $webArgs = @("run", "--project", $WebProjRel, "--launch-profile", "http")
    if ($SkipBuild) { $webArgs += "--no-build" }
    Push-Location $Root
    try { & dotnet @webArgs }
    finally { Pop-Location }
}
finally {
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
    }
    if ($winProcess -and -not $winProcess.HasExited) {
        Stop-Process -Id $winProcess.Id -Force -ErrorAction SilentlyContinue
    }
}
