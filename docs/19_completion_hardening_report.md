# 19. Hoan thien cac hang muc con lai

## 1. Qdrant that

Da co trong `docker-compose.yml`:

- Service `qdrant`.
- Volume `qdrant-data`.
- Bien `QDRANT_URL`.
- AI service uu tien Qdrant neu `http://qdrant:6333` san sang.
- Neu Qdrant khong chay, fallback local vector store van hoat dong de demo.

Can chay Docker Desktop de dung Qdrant that:

```powershell
docker compose up -d qdrant
```

Da them script:

```powershell
scripts/start_qdrant.ps1
```

Cap nhat ngay 26/06/2026:

- Da tai Qdrant Windows binary chinh thuc `v1.18.2` vao `tools/qdrant/`.
- Da chay Qdrant that tren `http://127.0.0.1:6333`.
- Collection `admissions_docs` da co `points_count=3`.
- AI service health da bao `vector.backend=qdrant`.
- Docker van chua co trong PATH, nhung Qdrant runtime that da chay bang binary native nen khong can Docker cho demo local.

## 2. LLM generation that

Da them adapter OpenAI-compatible trong backend:

- `LlmOptions`
- `LlmAnswerService`
- `RagService` uu tien LLM neu `Llm:Enabled=true`.
- Neu chua co API key/base URL, he thong fallback sang extractive answer hien tai.

Cau hinh qua env:

```env
LLM_ENABLED=true
LLM_BASE_URL=http://127.0.0.1:11434/v1
LLM_API_KEY=ollama
LLM_MODEL=qwen2.5:3b
```

Da them endpoint kiem tra cau hinh/runtime:

```http
GET /api/admin/ai/status
```

Cap nhat ngay 26/06/2026:

- Ollama da co model `qwen2.5:3b`.
- OpenAI-compatible endpoint `http://127.0.0.1:11434/v1/chat/completions` da test pass.
- API da chay voi `Llm__Enabled=true`.
- Chat RAG da sinh cau tra loi qua local LLM khi co source tu Qdrant.

## 3. Upload file trong khung chat

Da them:

- `POST /api/chat/conversations/file-question`
- Frontend chat co input file rieng.
- File duoc parse/chunk qua AI service.
- Cau hoi va cau tra loi theo file duoc luu vao chat history.
- File chat khong upsert vao knowledge base chung de tranh lam ban kho tri thuc tuyen sinh.

## 4. Realtime live support

Da them backend SignalR hub:

```http
/hubs/handoff
```

Hub co method:

- `JoinStaffQueue()`
- `LeaveStaffQueue()`
- `JoinTicket(ticketId)`
- `LeaveTicket(ticketId)`

Backend da broadcast event:

- `handoffTicketCreated`
- `handoffTicketUpdated`

Frontend da cai `@microsoft/signalr`, tu dong ket noi hub khi admin/staff dang nhap, join staff queue va refresh ticket/dashboard khi co event realtime.

## 5. Dashboard thong ke

Da them:

- `GET /api/admin/dashboard`
- Frontend panel `Dashboard van hanh`

Metric:

- Users
- Documents
- Completed document versions
- Conversations
- Chat messages
- Negative feedback
- Open/resolved handoff tickets
- Evaluation runs
- Latest Hit@K
- Latest keyword hit rate
- Average chat latency

## 6. Test tu dong / smoke test

Da them script:

```powershell
scripts/smoke_test.ps1
```

Script test:

- API health
- Admin login
- RAG chat
- Chat history
- Negative feedback
- Handoff ticket
- Staff resolve
- Dashboard

## 7. Deployment

Da cap nhat:

- `.env.example`
- `docker-compose.yml`

Compose gom:

- SQL Server
- Qdrant
- API
- AI service
- Web

Ghi chu: May local hien tai truoc do khong co Docker trong PATH, nen can cai Docker Desktop truoc khi chay full compose.

## 8. Bao mat / hardening

Da cai thien:

- Cau hinh LLM key qua env, khong hard-code key.
- `.env` bi ignore, `.env.example` giu placeholder.
- Upload tai lieu co gioi han dung luong.
- Chat file upload co gioi han 15MB.
- Admin/staff endpoints co RBAC.
- Handoff admin endpoints can role `admin`/`staff`.
- Chat history guest bi gioi han theo `clientSessionId`.

Can lam them neu dua vao production:

- Doi JWT secret va admin password.
- Bat HTTPS bat buoc.
- Them antivirus/file scanning cho upload.
- Them rate limit cho chat/upload.
- Giam CORS ve domain that.
- Them audit log rieng cho thao tac admin.

## Ket qua kiem thu ngay 26/06/2026

Da chay:

```powershell
dotnet build AdmissionsAiSystem.slnx --artifacts-path $env:TEMP\admissions-ai-build\solution -v:minimal
npm run build
scripts/smoke_test.ps1 -ApiBaseUrl http://127.0.0.1:5000
```

Ket qua:

- Backend build: pass.
- Frontend build: pass.
- EF database update tren LocalDB `AdmissionsLocal`: database da up to date.
- HTTP smoke test: pass.
- Luong RAG chat -> feedback negative -> tao handoff ticket -> admin reply/resolve -> dashboard: pass.
- Luong chat upload file -> parse/chunk -> tra loi co source -> luu conversation: pass.
- AI service health `http://127.0.0.1:8000/health`: pass.
- AI runtime status `GET /api/admin/ai/status`: pass.
- SignalR negotiate `/hubs/handoff/negotiate`: pass voi JWT admin.
- Frontend SignalR build: pass.
- `scripts/check_ai_runtime.ps1`: pass, bao dung Qdrant/LLM da san sang.
- Qdrant native `v1.18.2`: pass.
- Qdrant collection `admissions_docs` co 3 points: pass.
- Ollama OpenAI-compatible chat completions voi `qwen2.5:3b`: pass.
- Chat RAG voi Qdrant + LLM: pass, tra loi co 3 sources.

Ghi chu:

- API local can chay voi connection string `Server=(localdb)\AdmissionsLocal;Database=AdmissionsAiSystem;Trusted_Connection=True;TrustServerCertificate=True`.
- Qdrant runtime that da test bang native binary; Docker compose van can Docker Desktop neu muon chay full container.
- LLM runtime that da test bang Ollama local; neu dung cloud LLM thi doi `LLM_BASE_URL`, `LLM_API_KEY`, `LLM_MODEL`.
- SignalR backend/frontend da build pass; negotiate JWT da pass.

## Trang thai tong ket

| Hang muc | Trang thai |
|---|---|
| Qdrant service config | Da co |
| Qdrant runtime local | Da chay bang native binary |
| LLM adapter | Da co |
| LLM runtime | Da chay bang Ollama `qwen2.5:3b` |
| Chat file upload | Da co |
| SignalR hub | Da co backend + frontend client |
| Dashboard | Da co |
| Smoke test script | Da co |
| AI runtime status script | Da co |
| Docker compose/env | Da cap nhat |
| Security baseline | Da cap nhat |
