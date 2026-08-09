param(
    [string]$ApiBaseUrl = "http://127.0.0.1:5000",
    [string]$AiBaseUrl = "http://127.0.0.1:8000",
    [string]$QdrantBaseUrl = "http://127.0.0.1:6333",
    [string]$QdrantCollection = "admissions_docs_e5_v1",
    [string]$AdminEmail = "",
    [string]$AdminPassword = "",
    [switch]$ConfirmReset
)

$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$sourceFile = Join-Path $projectRoot "docs/source_materials/cmcu_admissions_2026.md"
$apiWorkingDirectory = Join-Path $projectRoot "apps/api/src/Admissions.Api/Admissions.Api"
$documentStorage = Join-Path $apiWorkingDirectory "storage/documents"
$localVectorStore = Join-Path $projectRoot "apps/ai-service/.local-vector-store.json"
$appSettingsPath = Join-Path $apiWorkingDirectory "appsettings.json"

if (-not $ConfirmReset) {
    throw "Lenh nay xoa du lieu local cua du an. Chay lai voi -ConfirmReset de xac nhan."
}

if ([string]::IsNullOrWhiteSpace($AdminEmail) -or [string]::IsNullOrWhiteSpace($AdminPassword)) {
    $seedAdmin = (Get-Content -LiteralPath $appSettingsPath -Raw | ConvertFrom-Json).SeedAdmin
    if ([string]::IsNullOrWhiteSpace($AdminEmail)) { $AdminEmail = $seedAdmin.Email }
    if ([string]::IsNullOrWhiteSpace($AdminPassword)) { $AdminPassword = $seedAdmin.Password }
}

if (-not (Test-Path $sourceFile -PathType Leaf)) {
    throw "Khong tim thay nguon chinh thuc: $sourceFile"
}

Write-Host "Stopping the current local application..."
$applicationPorts = 5000, 8000, 5173
Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
    Where-Object { $applicationPorts -contains $_.LocalPort } |
    Select-Object -ExpandProperty OwningProcess -Unique |
    ForEach-Object { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue }

Write-Host "Removing only the AdmissionsAiSystem LocalDB database..."
sqllocaldb start AdmissionsLocal | Out-Null
$connection = New-Object System.Data.SqlClient.SqlConnection
$connection.ConnectionString = "Server=(localdb)\AdmissionsLocal;Database=master;Trusted_Connection=True;TrustServerCertificate=True"
$connection.Open()
try {
    $command = $connection.CreateCommand()
    $command.CommandText = @"
IF DB_ID(N'AdmissionsAiSystem') IS NOT NULL
BEGIN
    ALTER DATABASE [AdmissionsAiSystem] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [AdmissionsAiSystem];
END
"@
    [void]$command.ExecuteNonQuery()
} finally {
    $connection.Dispose()
}

Write-Host "Removing uploaded demo files and the old vector collection..."
if (Test-Path $documentStorage) {
    Remove-Item -LiteralPath $documentStorage -Recurse -Force
}
if (Test-Path $localVectorStore) {
    Remove-Item -LiteralPath $localVectorStore -Force
}

& (Join-Path $PSScriptRoot "start_qdrant.ps1")
try {
    Invoke-RestMethod -Uri "$QdrantBaseUrl/collections/$QdrantCollection" -Method Delete -TimeoutSec 15 | Out-Null
} catch {
    if ($_.Exception.Response.StatusCode.value__ -ne 404) {
        throw
    }
}

Write-Host "Starting the clean application..."
# Keep the API seed credentials aligned with the credentials used below for indexing.
# Explicit parameters win over any stale SeedAdmin__* variables in the caller environment.
$env:SeedAdmin__Email = $AdminEmail
$env:SeedAdmin__Password = $AdminPassword
& (Join-Path $PSScriptRoot "start_local_runtime.ps1") -QdrantCollection $QdrantCollection

Write-Host "Waiting for the API and AI service..."
$ready = $false
for ($attempt = 1; $attempt -le 90; $attempt++) {
    try {
        Invoke-RestMethod -Uri "$ApiBaseUrl/api/health" -TimeoutSec 3 | Out-Null
        Invoke-RestMethod -Uri "$AiBaseUrl/health" -TimeoutSec 3 | Out-Null
        $ready = $true
        break
    } catch {
        Start-Sleep -Seconds 2
    }
}
if (-not $ready) {
    throw "API/AI service khong san sang sau 180 giay."
}

Write-Host "Uploading and indexing the verified CMCU 2026 source..."
$login = Invoke-RestMethod `
    -Uri "$ApiBaseUrl/api/auth/login" `
    -Method Post `
    -ContentType "application/json" `
    -Body (@{ email = $AdminEmail; password = $AdminPassword } | ConvertTo-Json)
$token = $login.data.accessToken
Add-Type -AssemblyName System.Net.Http
$httpClient = [System.Net.Http.HttpClient]::new()
$multipart = [System.Net.Http.MultipartFormDataContent]::new()
$fileStream = $null
$fileContent = $null
try {
    $httpClient.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $token)

    $multipart.Add(([System.Net.Http.StringContent]::new("Nguồn tuyển sinh CMCU 2026 - nguồn chính thức")), "title")
    $multipart.Add(([System.Net.Http.StringContent]::new("admission_notice")), "documentType")
    $multipart.Add(([System.Net.Http.StringContent]::new("https://tuyensinh.cmcu.edu.vn/")), "source")
    $multipart.Add(([System.Net.Http.StringContent]::new("true")), "processNow")

    $fileStream = [System.IO.File]::OpenRead($sourceFile)
    $fileContent = [System.Net.Http.StreamContent]::new($fileStream)
    $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::new("text/markdown")
    $multipart.Add($fileContent, "file", [System.IO.Path]::GetFileName($sourceFile))

    $uploadResponse = $httpClient.PostAsync(
        "$ApiBaseUrl/api/admin/documents",
        $multipart
    ).GetAwaiter().GetResult()
    $upload = $uploadResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    if (-not $uploadResponse.IsSuccessStatusCode) {
        throw "Tai lieu chinh thuc khong duoc tai len thanh cong ($([int]$uploadResponse.StatusCode)): $upload"
    }
} finally {
    if ($null -ne $fileContent) { $fileContent.Dispose() }
    elseif ($null -ne $fileStream) { $fileStream.Dispose() }
    $multipart.Dispose()
    $httpClient.Dispose()
}
$uploadResult = $upload | ConvertFrom-Json
if ($uploadResult.success -ne $true) {
    throw "Tai lieu chinh thuc khong duoc xu ly thanh cong: $upload"
}

Write-Host "Running representative admissions questions..."
$questions = @(
    "Học phí ngành Trí tuệ Nhân tạo năm 2026 là bao nhiêu?",
    "hoc fi nghanh tri tue nhan tao bn vay",
    "Trường Đại học CMC có những phương thức xét tuyển nào?",
    "chi tieu tuyen sin 2026 la bao nhieu",
    "Thời tiết Hà Nội hôm nay thế nào?"
)
foreach ($question in $questions) {
    $response = Invoke-RestMethod `
        -Uri "$ApiBaseUrl/api/rag/chat" `
        -Method Post `
        -ContentType "application/json" `
        -Body (@{ question = $question; topK = 5 } | ConvertTo-Json)
    Write-Host "Q: $question"
    Write-Host "A: $($response.data.answer)"
    Write-Host ""
}

Write-Host "Clean local runtime is ready at http://127.0.0.1:5173"
