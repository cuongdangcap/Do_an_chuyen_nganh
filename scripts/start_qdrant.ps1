param(
    [string]$ComposeFile = "docker-compose.yml",
    [string]$QdrantExe = "tools/qdrant/qdrant.exe"
)

$ErrorActionPreference = "Stop"

function Wait-Qdrant {
    Write-Host "Checking Qdrant health..."
    for ($i = 1; $i -le 30; $i++) {
        try {
            $response = Invoke-RestMethod -Uri "http://127.0.0.1:6333/" -Method Get -TimeoutSec 3
            Write-Host "Qdrant is ready."
            $response | ConvertTo-Json -Depth 5
            return
        } catch {
            Start-Sleep -Seconds 2
        }
    }

    throw "Qdrant did not become ready on http://127.0.0.1:6333 within 60 seconds."
}

try {
    Invoke-RestMethod -Uri "http://127.0.0.1:6333/" -Method Get -TimeoutSec 2 | Out-Null
    Write-Host "Qdrant is already running."
    Wait-Qdrant
    exit 0
} catch {
    # Continue and start it below.
}

if (Test-Path $QdrantExe) {
    Write-Host "Starting local Qdrant binary..."
    $resolved = Resolve-Path $QdrantExe
    Start-Process -FilePath $resolved.Path -WorkingDirectory (Split-Path $resolved.Path) -WindowStyle Hidden | Out-Null
    Wait-Qdrant
    exit 0
}

if (Get-Command docker -ErrorAction SilentlyContinue) {
    Write-Host "Starting Qdrant with Docker Compose..."
    docker compose -f $ComposeFile up -d qdrant
    Wait-Qdrant
    exit 0
}

throw "Qdrant is not installed. Put qdrant.exe at tools/qdrant/qdrant.exe or install Docker Desktop."
