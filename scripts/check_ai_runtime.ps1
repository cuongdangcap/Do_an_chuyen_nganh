param(
    [string]$ApiBaseUrl = "http://127.0.0.1:5000",
    [string]$AiBaseUrl = "http://127.0.0.1:8000",
    [string]$AdminEmail = "admin@example.com",
    [string]$AdminPassword = "Admin123456!",
    [string]$ExpectedCollection = "admissions_docs_e5_v1",
    [string]$ExpectedEmbeddingProvider = "sentence-transformers",
    [string]$ExpectedEmbeddingModel = "intfloat/multilingual-e5-small",
    [int]$ExpectedEmbeddingDimension = 384
)

$ErrorActionPreference = "Stop"

Write-Host "Logging in admin..."
$login = Invoke-RestMethod `
    -Uri "$ApiBaseUrl/api/auth/login" `
    -Method Post `
    -ContentType "application/json" `
    -Body (@{ email = $AdminEmail; password = $AdminPassword } | ConvertTo-Json)

$headers = @{ Authorization = "Bearer $($login.data.accessToken)" }

Write-Host "Checking API AI runtime status..."
$status = Invoke-RestMethod -Uri "$ApiBaseUrl/api/admin/ai/status" -Headers $headers
$status.data | ConvertTo-Json -Depth 8

if ($status.data.qdrantAvailable -ne $true) {
    throw "Qdrant is not available; semantic runtime validation cannot continue."
}

if ($status.data.llmConfigured -ne $true) {
    throw "LLM is not configured. Expected local Ollama runtime."
}

if ($status.data.vectorBackend -ne "qdrant") {
    throw "Expected API vector backend 'qdrant' but received '$($status.data.vectorBackend)'."
}

Write-Host "Checking AI service health details..."
$health = Invoke-RestMethod -Uri "$AiBaseUrl/health"
$health | ConvertTo-Json -Depth 8

$vector = $health.vector
if ($null -eq $vector) {
    throw "AI service health response does not include vector details."
}

if ($vector.backend -ne "qdrant") {
    throw "Expected vector backend 'qdrant' but received '$($vector.backend)'."
}

if ($vector.qdrant_available -ne $true) {
    throw "AI service reports Qdrant unavailable."
}

if ($vector.collection -ne $ExpectedCollection) {
    throw "Expected Qdrant collection '$ExpectedCollection' but received '$($vector.collection)'."
}

$embedding = $vector.embedding
if ($null -eq $embedding) {
    throw "AI service health response does not include embedding details."
}

if ($embedding.provider -ne $ExpectedEmbeddingProvider) {
    throw "Expected embedding provider '$ExpectedEmbeddingProvider' but received '$($embedding.provider)'."
}

if ($embedding.model -ne $ExpectedEmbeddingModel) {
    throw "Expected embedding model '$ExpectedEmbeddingModel' but received '$($embedding.model)'."
}

if ([int]$embedding.dimension -ne $ExpectedEmbeddingDimension) {
    throw "Expected embedding dimension '$ExpectedEmbeddingDimension' but received '$($embedding.dimension)'."
}

if ($embedding.semantic -ne $true) {
    throw "Embedding runtime is not semantic."
}

Write-Host "AI runtime validation passed."
