param(
    [string]$ApiBaseUrl = "http://localhost:5000",
    [string]$AdminEmail = "admin@example.com",
    [string]$AdminPassword = "Admin123456!"
)

$ErrorActionPreference = "Stop"

function Invoke-JsonPost($Uri, $Body, $Headers = @{}) {
    Invoke-RestMethod -Uri $Uri -Method Post -ContentType "application/json" -Headers $Headers -Body ($Body | ConvertTo-Json -Depth 10)
}

Write-Host "Checking API health..."
$health = Invoke-RestMethod -Uri "$ApiBaseUrl/api/health" -Method Get
if (-not $health.success) { throw "API health failed." }

Write-Host "Logging in admin..."
$login = Invoke-JsonPost "$ApiBaseUrl/api/auth/login" @{ email = $AdminEmail; password = $AdminPassword }
$headers = @{ Authorization = "Bearer $($login.data.accessToken)" }

Write-Host "Running RAG chat..."
$session = "smoke-" + [guid]::NewGuid().ToString()
$chat = Invoke-JsonPost "$ApiBaseUrl/api/rag/chat" @{ question = "Ho so xet tuyen gom gi?"; topK = 5; clientSessionId = $session }
if (-not $chat.data.assistantMessageId) { throw "RAG chat did not return assistantMessageId." }

Write-Host "Checking chat history..."
$history = Invoke-RestMethod -Uri "$ApiBaseUrl/api/chat/conversations?clientSessionId=$session&page=1&pageSize=5" -Method Get
if ($history.data.totalItems -lt 1) { throw "Chat history did not contain the new conversation." }

Write-Host "Creating negative feedback and handoff ticket..."
$feedback = Invoke-JsonPost "$ApiBaseUrl/api/chat/messages/$($chat.data.assistantMessageId)/feedback" @{ rating = "negative"; note = "Smoke test handoff." }
if (-not $feedback.data.handoffTicketId) { throw "Negative feedback did not create handoff ticket." }

Write-Host "Resolving handoff ticket..."
$reply = Invoke-JsonPost "$ApiBaseUrl/api/admin/handoff/tickets/$($feedback.data.handoffTicketId)/reply" @{ content = "Smoke test staff reply."; resolve = $true } $headers
if ($reply.data.status -ne "resolved") { throw "Handoff ticket was not resolved." }

Write-Host "Checking dashboard..."
$dashboard = Invoke-RestMethod -Uri "$ApiBaseUrl/api/admin/dashboard" -Method Get -Headers $headers
if ($null -eq $dashboard.data.totalConversations) { throw "Dashboard response is invalid." }

Write-Host "Smoke test passed."
