param(
    [string]$ApiBaseUrl = "http://127.0.0.1:5000",
    [string]$AdminEmail = "admin@example.com",
    [string]$AdminPassword = "Admin123456!"
)

$ErrorActionPreference = "Stop"

Write-Host "Logging in admin..."
$login = Invoke-RestMethod `
    -Uri "$ApiBaseUrl/api/auth/login" `
    -Method Post `
    -ContentType "application/json" `
    -Body (@{ email = $AdminEmail; password = $AdminPassword } | ConvertTo-Json)

$headers = @{ Authorization = "Bearer $($login.data.accessToken)" }

Write-Host "Checking AI runtime status..."
$status = Invoke-RestMethod -Uri "$ApiBaseUrl/api/admin/ai/status" -Headers $headers
$status.data | ConvertTo-Json -Depth 5

if ($status.data.qdrantAvailable -ne $true) {
    Write-Warning "Qdrant is not available. The AI service is using local vector fallback."
}

if ($status.data.llmConfigured -ne $true) {
    Write-Warning "LLM is not configured. RAG answers will use extractive fallback."
}
