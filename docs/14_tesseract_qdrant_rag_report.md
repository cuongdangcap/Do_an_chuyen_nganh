# 14. Tesseract OCR, Vector Upsert, Retrieval API va Chat RAG

## Muc tieu

Hoan thien buoc sau module upload tai lieu:

- Cai dat Tesseract OCR engine va ngon ngu `vie`.
- Doc duoc anh upload bang OCR.
- Upsert chunk vao vector store.
- Cung cap API search/chat RAG cho cong thong tin.
- Gan UI chat RAG vao frontend.

## Trang thai cai dat

| Hang muc | Ket qua |
|---|---|
| Tesseract OCR | Da cai tai `C:\Program Files\Tesseract-OCR\tesseract.exe` |
| Vietnamese data | Da co `apps/ai-service/tessdata/vie.traineddata` |
| English data | Da co `apps/ai-service/tessdata/eng.traineddata` |
| AI service env | Dung `TESSERACT_CMD`, `TESSDATA_DIR`, `LOCAL_VECTOR_STORE`, `QDRANT_URL` |
| Qdrant runtime | Chua chay local vi Docker khong co san; service tu fallback sang local vector store |

## Thay doi source code

### AI service

- `apps/ai-service/app/ingestion/extractors.py`
  - Them OCR cho `png/jpg/jpeg` bang `pytesseract`.
  - Sua cau hinh `--tessdata-dir` tren Windows de Tesseract doc dung `vie.traineddata`.
- `apps/ai-service/app/rag/embeddings.py`
  - Baseline embedding deterministic 384 chieu de phuc vu prototype/offline test.
- `apps/ai-service/app/rag/vector_store.py`
  - Thu ket noi Qdrant tai `QDRANT_URL`.
  - Neu Qdrant khong san sang, fallback sang file local `storage/local_vectors.json`.
- `apps/ai-service/app/api/internal_rag.py`
  - `POST /internal/rag/upsert`
  - `POST /internal/rag/search`

### Backend ASP.NET Core

- `apps/api/src/Admissions.Api/Admissions.Api/Controllers/RagController.cs`
  - `POST /api/rag/search`
  - `POST /api/rag/chat`
- `apps/api/src/Admissions.Infrastructure/Admissions.Infrastructure/Services/RagService.cs`
  - Goi AI service search va tao cau tra loi co nguon.
- `apps/api/src/Admissions.Infrastructure/Admissions.Infrastructure/Services/DocumentService.cs`
  - Sau khi ingestion tao chunk, goi vector upsert.
- `apps/api/src/Admissions.Infrastructure/Admissions.Infrastructure/DependencyInjection.cs`
  - Cau hinh typed `HttpClient` cho AI service tai DI, tranh loi sua `Timeout/BaseAddress` sau request dau tien.

### Frontend React

- `apps/web/src/App.jsx`
  - Them panel `Tro ly RAG tuyen sinh` trong cong thong tin.
  - Goi `/api/rag/chat`, hien thi answer, backend vector va danh sach source chunk.
- `apps/web/src/styles.css`
  - Them style cho form chat, answer va source list.

## Ket qua test local

| Test | Ket qua |
|---|---|
| Backend build | Pass, 0 warning, 0 error |
| Frontend build | Pass |
| AI import check | Pass |
| API health | `http://localhost:5000/api/health` OK |
| AI health | `http://localhost:8000/health` OK |
| Upload TXT + process | Pass, tao 1 chunk, status `completed` |
| Vector upsert | Pass, backend `local` |
| RAG search | Pass, tra ve source phu hop |
| RAG chat | Pass, tra loi co nguon |
| Upload PNG OCR + process | Pass, Tesseract doc text va tao 1 chunk |
| RAG chat voi nguon anh OCR | Pass, source dau tien la tai lieu anh |
| Frontend smoke test | `http://127.0.0.1:5173/` tra HTTP 200 |

## Endpoint da san sang

### Public RAG

```http
POST /api/rag/search
Content-Type: application/json

{
  "query": "Ho so xet tuyen gom gi",
  "topK": 5
}
```

```http
POST /api/rag/chat
Content-Type: application/json

{
  "question": "Ho so xet tuyen gom gi?",
  "topK": 5
}
```

### Internal AI service

```http
POST /internal/rag/upsert
POST /internal/rag/search
POST /internal/ingestion/process
```

## Dich vu dang chay sau test

| Dich vu | URL | PID |
|---|---|---|
| ASP.NET Core API | `http://localhost:5000` | `4912` |
| React/Vite web | `http://127.0.0.1:5173` | `13588` |
| Python AI service | `http://localhost:8000` | `8708` |
| Qdrant | `http://localhost:6333` | Chua chay |

## Ghi chu ve Qdrant

Hien tai may local chua co Docker trong PATH va khong co package Qdrant phu hop qua `winget`, nen chua khoi dong duoc Qdrant local. Code da co adapter Qdrant HTTP; khi sau nay chay Qdrant tai `http://localhost:6333`, AI service se uu tien Qdrant. Neu Qdrant khong san sang, service dung fallback local vector store de tiep tuc demo va test RAG.

## Viec nen lam tiep

1. Cai/chay Qdrant that bang Docker Desktop hoac binary rieng, sau do test lai de backend vector tra `qdrant`.
2. Thay baseline hashing embedding bang model embedding local tot hon.
3. Them LLM generation that sau retrieval, kem citation va guardrail.
4. Them feedback/evaluation: golden questions, precision@k, groundedness, hallucination check.
