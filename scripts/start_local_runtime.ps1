param(
    [string]$ApiUrl = "http://127.0.0.1:5000",
    [string]$AiUrl = "127.0.0.1",
    [int]$AiPort = 8000,
    [string]$WebHost = "127.0.0.1",
    [int]$WebPort = 5173,
    [string]$OllamaBaseUrl = "http://127.0.0.1:11434/v1",
    [string]$OllamaModel = "qwen2.5:3b"
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")

Write-Host "Starting SQL Server LocalDB..."
sqllocaldb start AdmissionsLocal | Out-Null

Write-Host "Starting Qdrant..."
& (Join-Path $PSScriptRoot "start_qdrant.ps1")

Write-Host "Starting AI service..."
$env:TESSERACT_CMD = "C:\Program Files\Tesseract-OCR\tesseract.exe"
$env:TESSDATA_DIR = Join-Path $root "apps/ai-service/tessdata"
$env:LOCAL_VECTOR_STORE = Join-Path $root "apps/ai-service/.local-vector-store.json"
$env:QDRANT_URL = "http://127.0.0.1:6333"
Start-Process `
    -FilePath (Join-Path $root "apps/ai-service/.venv/Scripts/python.exe") `
    -ArgumentList @("-m", "uvicorn", "app.main:app", "--host", $AiUrl, "--port", "$AiPort") `
    -WorkingDirectory (Join-Path $root "apps/ai-service") `
    -WindowStyle Hidden | Out-Null

Write-Host "Building API artifact..."
dotnet build (Join-Path $root "apps/api/AdmissionsAiSystem.slnx") --artifacts-path "$env:TEMP\admissions-ai-build\solution" -v:minimal | Out-Null

Write-Host "Starting API with Ollama LLM..."
$env:ConnectionStrings__DefaultConnection = "Server=(localdb)\AdmissionsLocal;Database=AdmissionsAiSystem;Trusted_Connection=True;TrustServerCertificate=True"
$env:Llm__Enabled = "true"
$env:Llm__BaseUrl = $OllamaBaseUrl
$env:Llm__ApiKey = "ollama"
$env:Llm__Model = $OllamaModel
$env:Llm__TimeoutSeconds = "120"
$apiDll = Join-Path $env:TEMP "admissions-ai-build/solution/bin/Admissions.Api/debug/Admissions.Api.dll"
Start-Process `
    -FilePath dotnet `
    -ArgumentList @($apiDll, "--urls", $ApiUrl) `
    -WorkingDirectory (Join-Path $root "apps/api/src/Admissions.Api/Admissions.Api") `
    -WindowStyle Hidden | Out-Null

Write-Host "Starting frontend..."
Start-Process `
    -FilePath npm.cmd `
    -ArgumentList @("run", "dev", "--", "--host", $WebHost, "--port", "$WebPort") `
    -WorkingDirectory (Join-Path $root "apps/web") `
    -WindowStyle Hidden | Out-Null

Write-Host "Local runtime requested. Check status with scripts/check_ai_runtime.ps1."
