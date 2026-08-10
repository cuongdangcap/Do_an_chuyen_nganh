$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Set-Location $projectRoot

function Require-Command([string]$Name, [string]$InstallHint) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Thieu '$Name'. $InstallHint"
    }
}

function Wait-Url([string]$Url, [string]$Label, [int]$Attempts = 90) {
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3 | Out-Null
            Write-Host "[OK] $Label da san sang: $Url" -ForegroundColor Green
            return $true
        } catch {
            Start-Sleep -Seconds 2
        }
    }

    Write-Host "[LOI] $Label chua san sang: $Url" -ForegroundColor Red
    return $false
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "   CMC ADMISSIONS - KHOI DONG DU AN LOCAL" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Thu muc: $projectRoot"
Write-Host ""

Require-Command "dotnet" "Can cai .NET SDK 10."
Require-Command "npm.cmd" "Can cai Node.js/npm."
Require-Command "sqllocaldb" "Can cai SQL Server LocalDB."

$pythonExe = Join-Path $projectRoot "apps/ai-service/.venv/Scripts/python.exe"
if (-not (Test-Path $pythonExe -PathType Leaf)) {
    throw "Chua co Python virtual environment. Hay chay lenh CAP NHAT/Cai dat mot lan truoc khi mo file BAT."
}

$localDbInstances = @(sqllocaldb info 2>$null)
if ($localDbInstances -notcontains "AdmissionsLocal") {
    Write-Host "Tao SQL Server LocalDB instance AdmissionsLocal..." -ForegroundColor Yellow
    sqllocaldb create AdmissionsLocal | Out-Null
}

$qdrantExe = Join-Path $projectRoot "tools/qdrant/qdrant.exe"
$dockerAvailable = [bool](Get-Command docker -ErrorAction SilentlyContinue)
if (-not (Test-Path $qdrantExe -PathType Leaf) -and -not $dockerAvailable) {
    throw "Chua co Qdrant. Dat qdrant.exe tai tools/qdrant/qdrant.exe hoac cai Docker Desktop."
}

Write-Host "Dang khoi dong LocalDB, Qdrant, AI service, Backend va Frontend..." -ForegroundColor Yellow
& (Join-Path $PSScriptRoot "start_local_runtime.ps1")

Write-Host ""
Write-Host "Dang doi cac dich vu san sang..." -ForegroundColor Yellow
$apiReady = Wait-Url "http://127.0.0.1:5000/api/health" "Backend API"
$aiReady = Wait-Url "http://127.0.0.1:8000/health" "AI service"
$webReady = Wait-Url "http://127.0.0.1:5173" "Frontend"

Write-Host ""
if ($apiReady -and $aiReady -and $webReady) {
    Write-Host "============================================================" -ForegroundColor Green
    Write-Host "   DU AN DA CHAY THANH CONG" -ForegroundColor Green
    Write-Host "============================================================" -ForegroundColor Green
    Write-Host "Frontend : http://127.0.0.1:5173"
    Write-Host "Backend  : http://127.0.0.1:5000"
    Write-Host "AI       : http://127.0.0.1:8000"
    Write-Host "Qdrant   : http://127.0.0.1:6333"
    Write-Host ""
    Start-Process "http://127.0.0.1:5173"
} else {
    Write-Host "Mot hoac nhieu dich vu chua khoi dong duoc. Xem thong bao loi phia tren." -ForegroundColor Red
}

Write-Host ""
Write-Host "Co the dong cua so nay sau khi trinh duyet da mo. Cac dich vu du an van tiep tuc chay." -ForegroundColor DarkGray
Read-Host "Nhan Enter de dong cua so"
