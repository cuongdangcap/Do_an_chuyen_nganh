# 13. Documents/RAG Ingestion Report

## 1. Muc tieu

Da xay module upload tai lieu va pipeline ingestion cho RAG:

- Admin/staff upload PDF, DOCX, PNG/JPG, TXT/MD.
- Backend luu metadata tai lieu vao SQL Server.
- Backend luu file vao local storage.
- AI service trich xuat text va chia chunk.
- Backend luu chunk metadata vao SQL Server de dung cho RAG/vector search.

## 2. Database schema

Migration moi:

```text
20260626001554_DocumentsRagSchema
```

SQL script:

```text
scripts/sql/documents_rag_schema.sql
```

Bang moi:

```text
knowledge_documents
document_versions
document_chunks
ingestion_jobs
```

Da apply vao LocalDB `AdmissionsAiSystem`.

## 3. Backend API

Controller:

```text
apps/api/src/Admissions.Api/Admissions.Api/Controllers/AdminDocumentsController.cs
```

Endpoints:

```text
GET  /api/admin/documents
GET  /api/admin/documents/{id}
GET  /api/admin/documents/versions/{versionId}/chunks
POST /api/admin/documents
POST /api/admin/documents/versions/{versionId}/process
```

Tat ca endpoint can role:

```text
admin, staff
```

Upload dung multipart/form-data:

```text
title
documentType
source
processNow
file
```

## 4. AI service

Endpoint moi:

```text
POST /internal/ingestion/process
```

Parser da co:

- PDF text: `pypdf`.
- DOCX: `python-docx`.
- TXT/MD: doc text truc tiep.
- PNG/JPG/JPEG: `pytesseract` + `Pillow`.

Ghi chu OCR:

- Thu vien Python OCR da cai.
- May local hien chua co binary `tesseract` trong PATH.
- Upload anh van duoc chap nhan, nhung xu ly OCR se bao loi ro rang neu chua cai Tesseract OCR engine va language data.

## 5. Chunking

AI service thuc hien:

- Clean text.
- Detect section title co ban.
- Chunk theo ky tu voi overlap.
- Tao `point_id` on dinh bang UUID5.
- Gan metadata:
  - `document_id`
  - `document_version_id`
  - `title`
  - `document_type`
  - `page_number`
  - `section_title`
  - `embedding_status = pending`
  - `vector_backend = qdrant`

Chunk duoc luu vao SQL Server trong `document_chunks`.

## 6. Frontend

Da them khu `Tai lieu RAG` trong cong quan tri:

- Upload file.
- Chon loai tai lieu.
- Nhap nguon.
- Tuy chon xu ly/chia chunk ngay.
- Xem danh sach tai lieu.
- Re-process version.
- Xem chunk preview.

File chinh:

```text
apps/web/src/App.jsx
apps/web/src/styles.css
```

## 7. Test da thuc hien

Backend build:

```text
PASS dotnet build AdmissionsAiSystem.slnx
```

Frontend build:

```text
PASS npm run build
```

AI service import/health:

```text
PASS ai-import-ok
PASS GET http://localhost:8000/health
```

Database:

```text
PASS migration history co DocumentsRagSchema
PASS 4 bang document/RAG da ton tai
```

End-to-end ingestion:

```text
PASS TXT upload/process chunks=1
PASS DOCX upload/process chunks=1
PASS GET /api/admin/documents
PASS GET /api/admin/documents/versions/{versionId}/chunks
```

So lieu sau test:

```text
knowledge_documents: 3
document_versions: 3
document_chunks: 2
ingestion_jobs: 3
```

## 8. Services dang chay

```text
API:        http://localhost:5000
AI service: http://localhost:8000
Frontend:   http://127.0.0.1:5173
```

## 9. Buoc tiep theo

Module nay da tao duoc chunk metadata cho RAG. Buoc tiep theo nen lam:

1. Qdrant upsert: tao embedding va luu vector.
2. RAG retrieval API: search chunk theo cau hoi.
3. Chat UI: hoi dap dua tren retrieved chunks va structured data.
4. Evaluation: golden questions, hit@k, MRR, citation correctness.
