# Admissions AI System

He thong tu van tuyen sinh dai hoc gom cong thong tin, cong quan tri du lieu tuyen sinh, chatbot RAG, upload tai lieu, OCR, Qdrant vector search, LLM local va live support ticket.

## Thanh phan

| Thanh phan | Cong nghe | Thu muc |
|---|---|---|
| Frontend | React + Vite | `apps/web` |
| Backend API | ASP.NET Core Web API | `apps/api` |
| Database | SQL Server LocalDB | `AdmissionsLocal` |
| AI service | Python FastAPI | `apps/ai-service` |
| Vector DB | Qdrant | `tools/qdrant/qdrant.exe` hoac Docker |
| OCR | Tesseract OCR + `vie.traineddata` | `apps/ai-service/tessdata` |
| LLM local | Ollama OpenAI-compatible API | `qwen2.5:3b` |

## Port mac dinh

| Service | URL |
|---|---|
| Frontend | `http://127.0.0.1:5173` |
| Backend API | `http://127.0.0.1:5000` |
| AI service | `http://127.0.0.1:8000` |
| Qdrant | `http://127.0.0.1:6333` |
| Ollama | `http://127.0.0.1:11434` |

## Tai khoan mac dinh

```text
Email: admin@example.com
Password: Admin123456!
```

Tai khoan nay duoc seed khi API ket noi duoc SQL Server.

## Yeu cau cai dat

Can co san:

- Windows + PowerShell.
- .NET SDK 10.
- Node.js + npm.
- Python 3.12.
- SQL Server LocalDB.
- Tesseract OCR tai `C:\Program Files\Tesseract-OCR\tesseract.exe`.
- Ollama.
- Model Ollama `qwen2.5:3b`.

Kiem tra Ollama:

```powershell
ollama --version
ollama list
```

Neu chua co model:

```powershell
ollama pull qwen2.5:3b
```

## Chay nhanh local runtime

Tai thu muc goc du an:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/start_local_runtime.ps1
```

Script nay se:

1. Start SQL Server LocalDB instance `AdmissionsLocal`.
2. Start Qdrant that tren `http://127.0.0.1:6333`.
3. Start AI service tren `http://127.0.0.1:8000`.
4. Build backend API artifact.
5. Start API tren `http://127.0.0.1:5000` voi LLM Ollama.
6. Start frontend tren `http://127.0.0.1:5173`.

Sau do mo:

```text
http://127.0.0.1:5173
```

## Kiem tra runtime AI

```powershell
powershell -ExecutionPolicy Bypass -File scripts/check_ai_runtime.ps1 -ApiBaseUrl http://127.0.0.1:5000
```

Ket qua tot se co dang:

```json
{
  "aiServiceStatus": "ok",
  "vectorBackend": "qdrant",
  "qdrantAvailable": true,
  "llmEnabled": true,
  "llmConfigured": true,
  "llmBaseUrl": "http://127.0.0.1:11434/v1",
  "llmModel": "qwen2.5:3b"
}
```

## Test end-to-end

```powershell
powershell -ExecutionPolicy Bypass -File scripts/smoke_test.ps1 -ApiBaseUrl http://127.0.0.1:5000
```

Smoke test kiem tra:

- API health.
- Admin login.
- RAG chat.
- Chat history.
- Feedback negative.
- Tao handoff ticket.
- Staff reply/resolve ticket.
- Dashboard.

## Chay tung service thu cong

### 1. SQL Server LocalDB

```powershell
sqllocaldb start AdmissionsLocal
```

Connection string local:

```text
Server=(localdb)\AdmissionsLocal;Database=AdmissionsAiSystem;Trusted_Connection=True;TrustServerCertificate=True
```

Apply migration:

```powershell
cd apps/api
$env:ConnectionStrings__DefaultConnection='Server=(localdb)\AdmissionsLocal;Database=AdmissionsAiSystem;Trusted_Connection=True;TrustServerCertificate=True'
dotnet ef database update --project src/Admissions.Infrastructure/Admissions.Infrastructure/Admissions.Infrastructure.csproj --startup-project src/Admissions.Api/Admissions.Api/Admissions.Api.csproj
```

### 2. Qdrant

```powershell
powershell -ExecutionPolicy Bypass -File scripts/start_qdrant.ps1
```

Kiem tra:

```powershell
Invoke-RestMethod http://127.0.0.1:6333/
```

### 3. AI service

```powershell
cd apps/ai-service
$env:TESSERACT_CMD='C:\Program Files\Tesseract-OCR\tesseract.exe'
$env:TESSDATA_DIR='D:\Do_an_chuyen_nganh\apps\ai-service\tessdata'
$env:LOCAL_VECTOR_STORE='D:\Do_an_chuyen_nganh\apps\ai-service\.local-vector-store.json'
$env:QDRANT_URL='http://127.0.0.1:6333'
.\.venv\Scripts\python.exe -m uvicorn app.main:app --host 127.0.0.1 --port 8000
```

Kiem tra:

```powershell
Invoke-RestMethod http://127.0.0.1:8000/health
```

### 4. Backend API

```powershell
cd apps/api
$env:ConnectionStrings__DefaultConnection='Server=(localdb)\AdmissionsLocal;Database=AdmissionsAiSystem;Trusted_Connection=True;TrustServerCertificate=True'
$env:Llm__Enabled='true'
$env:Llm__BaseUrl='http://127.0.0.1:11434/v1'
$env:Llm__ApiKey='ollama'
$env:Llm__Model='qwen2.5:3b'
$env:Llm__TimeoutSeconds='120'
dotnet run --project src/Admissions.Api/Admissions.Api/Admissions.Api.csproj --urls http://127.0.0.1:5000
```

Kiem tra:

```powershell
Invoke-RestMethod http://127.0.0.1:5000/api/health
```

### 5. Frontend

```powershell
cd apps/web
npm install
npm run dev -- --host 127.0.0.1 --port 5173
```

Mo:

```text
http://127.0.0.1:5173
```

## Build

Backend:

```powershell
cd apps/api
dotnet build AdmissionsAiSystem.slnx -v:minimal
```

Frontend:

```powershell
cd apps/web
npm run build
```

AI service import check:

```powershell
cd apps/ai-service
$env:PYTHONDONTWRITEBYTECODE='1'
.\.venv\Scripts\python.exe -B -c "import app.api.health; import app.rag.vector_store; print('python import ok')"
```

## Chuc nang chinh

- Cong thong tin tuyen sinh: xem khoa, nganh, chuong trinh, hoc phi, diem chuan, FAQ.
- Cong quan tri: them du lieu tuyen sinh, upload va xu ly tai lieu RAG.
- Chat RAG: hoi dap theo knowledge base, tra loi co source.
- Upload file trong khung chat: hoi theo PDF/DOCX/anh/TXT rieng.
- Feedback cau tra loi: huu ich/chua dung.
- Handoff/live support: feedback am tao ticket cho staff.
- Realtime staff support: SignalR hub va frontend client.
- Dashboard: thong ke chat, feedback, ticket, document, evaluation, AI runtime.
- Evaluation runner: golden questions, hit@k, keyword hit rate, latency.

## Tai lieu thiet ke

Bat dau tu:

- `docs/00_design_index.md`
- `docs/admissions_ai_system_plan.md`
- `docs/19_completion_hardening_report.md`

## Xu ly loi thuong gap

### API login loi 500

Thuong do SQL Server chua chay hoac sai connection string.

```powershell
sqllocaldb start AdmissionsLocal
```

Sau do restart API.

### AI runtime bao `vectorBackend = local`

Qdrant chua chay. Chay:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/start_qdrant.ps1
```

Restart AI service voi:

```powershell
$env:QDRANT_URL='http://127.0.0.1:6333'
```

### AI runtime bao `llmConfigured = false`

Kiem tra Ollama:

```powershell
ollama list
Invoke-RestMethod http://127.0.0.1:11434/api/tags
```

Khi start API can co:

```powershell
$env:Llm__Enabled='true'
$env:Llm__BaseUrl='http://127.0.0.1:11434/v1'
$env:Llm__ApiKey='ollama'
$env:Llm__Model='qwen2.5:3b'
```

### Port da bi chiem

Kiem tra process:

```powershell
netstat -ano | Select-String ':5000|:8000|:5173|:6333|:11434'
```

Dung process theo PID:

```powershell
Stop-Process -Id <PID> -Force
```

## Ghi chu

- `tools/qdrant/`, `.env`, `logs/`, `storage/`, `tessdata/` duoc ignore de khong day file lon/secret len git.
- Docker compose da co trong `docker-compose.yml`, nhung local hien tai dang chay Qdrant bang native binary vi Docker/WSL chua cai.
- Neu doi sang cloud LLM, chi can doi `Llm__BaseUrl`, `Llm__ApiKey`, `Llm__Model`.
