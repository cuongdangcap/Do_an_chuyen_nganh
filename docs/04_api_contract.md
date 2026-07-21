# 04. API Contract

## 1. Quy ước chung

Base URL local đề xuất:

- Backend API: `http://localhost:5000/api`
- Swagger: `http://localhost:5000/swagger`
- AI service internal: `http://ai-service:8000`

Header chung:

```http
Authorization: Bearer <access_token>
Content-Type: application/json
X-Trace-Id: <optional-client-trace-id>
```

Upload file dùng:

```http
Content-Type: multipart/form-data
```

## 2. Response envelope

### 2.1. Thành công

```json
{
  "success": true,
  "data": {},
  "message": "OK",
  "traceId": "00-..."
}
```

### 2.2. Phân trang

```json
{
  "success": true,
  "data": {
    "items": [],
    "page": 1,
    "pageSize": 20,
    "totalItems": 125,
    "totalPages": 7
  },
  "message": "OK",
  "traceId": "00-..."
}
```

### 2.3. Lỗi

```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Dữ liệu không hợp lệ",
    "details": {
      "email": ["Email không đúng định dạng"]
    }
  },
  "traceId": "00-..."
}
```

## 3. Mã lỗi chuẩn

| Code | HTTP | Ý nghĩa |
|---|---:|---|
| `VALIDATION_ERROR` | 400 | Request không hợp lệ |
| `AUTH_INVALID_CREDENTIALS` | 401 | Sai email/mật khẩu |
| `AUTH_TOKEN_EXPIRED` | 401 | Access token hết hạn |
| `AUTH_FORBIDDEN` | 403 | Không đủ quyền |
| `USER_NOT_FOUND` | 404 | Không tìm thấy user |
| `MAJOR_NOT_FOUND` | 404 | Không tìm thấy ngành |
| `PROGRAM_NOT_FOUND` | 404 | Không tìm thấy chương trình |
| `DOCUMENT_NOT_FOUND` | 404 | Không tìm thấy tài liệu |
| `CONVERSATION_NOT_FOUND` | 404 | Không tìm thấy hội thoại |
| `FILE_TYPE_NOT_SUPPORTED` | 400 | Không hỗ trợ loại file |
| `FILE_TOO_LARGE` | 400 | File vượt dung lượng |
| `DOCUMENT_PROCESSING_FAILED` | 500 | Xử lý tài liệu thất bại |
| `AI_SERVICE_UNAVAILABLE` | 503 | AI service không sẵn sàng |
| `AI_LOW_CONFIDENCE` | 200/422 | AI không đủ tự tin |
| `RESOURCE_CONFLICT` | 409 | Trùng dữ liệu |

## 4. Auth API

### POST `/auth/register-student`

Quyền: Public

Endpoint này không dùng để sinh viên tự tạo tài khoản. Tài khoản sinh viên do nhà trường cấp sẵn theo định dạng `BIT######@st.cmcu.edu.vn`. Nếu gọi endpoint này, API trả lỗi:

Request:

```json
{
  "email": "bit240048@st.cmcu.edu.vn",
  "password": "StrongPassword123!",
  "fullName": "Tài khoản sinh viên do nhà trường cấp",
  "phone": "0912345678"
}
```

Response:

```json
{
  "success": false,
  "error": {
    "code": "STUDENT_SELF_REGISTER_DISABLED",
    "message": "Tài khoản sinh viên do nhà trường cấp, không tự đăng ký trên cổng này."
  },
  "traceId": "..."
}
```

### POST `/auth/register-parent`

Quyền: Public

Request gồm email, mật khẩu, họ tên và số điện thoại; role mặc định là `parent`.

### POST `/auth/login`

Quyền: Public

Request:

```json
{
  "email": "admin@example.com",
  "password": "StrongPassword123!"
}
```

Response:

```json
{
  "success": true,
  "data": {
    "accessToken": "jwt",
    "refreshToken": "refresh-token",
    "expiresIn": 3600,
    "user": {
      "id": "uuid",
      "email": "admin@example.com",
      "fullName": "Admin",
      "roles": ["admin"]
    }
  },
  "message": "Đăng nhập thành công",
  "traceId": "..."
}
```

### POST `/auth/refresh`

Quyền: Public

Request:

```json
{
  "refreshToken": "refresh-token"
}
```

### POST `/auth/logout`

Quyền: Authenticated

Request:

```json
{
  "refreshToken": "refresh-token"
}
```

### GET `/auth/me`

Quyền: Authenticated

Response trả user hiện tại và roles.

## 5. Profile API

### GET `/profiles/me`

Quyền: Authenticated

Response:

```json
{
  "success": true,
  "data": {
    "user": {
      "id": "uuid",
      "email": "bit240048@st.cmcu.edu.vn",
      "fullName": "Đặng Quang Long",
      "roles": ["student"]
    },
    "studentProfile": {
      "highSchool": "THPT A",
      "province": "Hà Nội",
      "graduationYear": 2026,
      "expectedScore": 24.5,
      "interestedSubjectGroup": "A01"
    }
  },
  "message": "OK",
  "traceId": "..."
}
```

### PUT `/profiles/me`

Quyền: Authenticated

Request cho student:

```json
{
  "fullName": "Nguyễn Văn A",
  "phone": "0912345678",
  "studentProfile": {
    "highSchool": "THPT A",
    "province": "Hà Nội",
    "graduationYear": 2026,
    "expectedScore": 24.5,
    "interestedSubjectGroup": "A01",
    "interestedMajorIds": ["uuid"]
  }
}
```

## 6. Admin Users API

### GET `/admin/users`

Quyền: Admin

Query:

- `page`
- `pageSize`
- `keyword`
- `role`
- `status`

### POST `/admin/users/staff`

Quyền: Admin

Request:

```json
{
  "email": "staff@example.com",
  "fullName": "Tư vấn viên",
  "phone": "0912345678",
  "department": "Phòng Tuyển sinh",
  "position": "Staff",
  "roles": ["staff"],
  "temporaryPassword": "Temp123456!"
}
```

### PATCH `/admin/users/{id}/status`

Quyền: Admin

Request:

```json
{
  "status": "locked",
  "reason": "Tài khoản không còn sử dụng"
}
```

### PUT `/admin/users/{id}/roles`

Quyền: Admin

Request:

```json
{
  "roles": ["staff"]
}
```

## 7. Admissions API

### GET `/majors`

Quyền: Public

Query:

- `keyword`
- `facultyId`
- `subjectCombinationCode`
- `minScore`
- `maxScore`
- `maxTuition`
- `campus`
- `page`
- `pageSize`

Response item:

```json
{
  "id": "uuid",
  "code": "7480201",
  "name": "Công nghệ thông tin",
  "facultyName": "Khoa CNTT",
  "programs": [
    {
      "id": "uuid",
      "name": "Công nghệ thông tin",
      "campus": "Hà Nội",
      "latestCutoffScore": 24.5,
      "tuitionRange": "25.000.000 - 30.000.000 VND/năm"
    }
  ]
}
```

### GET `/majors/{id}`

Quyền: Public

Response gồm ngành, chương trình, tổ hợp, điểm chuẩn, học phí, mô tả.

### POST `/admin/majors`

Quyền: Staff/Admin

Request:

```json
{
  "facultyId": "uuid",
  "code": "7480201",
  "name": "Công nghệ thông tin",
  "description": "Mô tả ngành",
  "careerOutcomes": "Lập trình viên, kỹ sư dữ liệu...",
  "status": "active"
}
```

### PUT `/admin/majors/{id}`

Quyền: Staff/Admin

Request giống tạo mới.

### DELETE `/admin/majors/{id}`

Quyền: Admin

Soft delete hoặc chuyển `status=inactive`.

### GET `/programs`

Quyền: Public

Query tương tự majors.

### GET `/cutoff-scores`

Quyền: Public

Query:

- `programId`
- `year`
- `methodCode`
- `subjectCombinationCode`

### POST `/admin/cutoff-scores`

Quyền: Staff/Admin

Request:

```json
{
  "programId": "uuid",
  "admissionCycleId": "uuid",
  "admissionMethodId": "uuid",
  "subjectCombinationId": "uuid",
  "score": 24.5,
  "note": "Điểm chuẩn theo phương thức THPT"
}
```

### GET `/tuition-fees`

Quyền: Public

Query:

- `programId`
- `academicYear`

### POST `/admissions/compare`

Quyền: Public hoặc Authenticated

Request:

```json
{
  "programIds": ["uuid-1", "uuid-2"],
  "criteria": ["cutoff_score", "tuition", "subject_combination", "career"]
}
```

Response:

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "programId": "uuid-1",
        "programName": "Công nghệ thông tin",
        "cutoffScores": [
          { "year": 2026, "method": "THPT", "score": 24.5 }
        ],
        "tuition": "25.000.000 - 30.000.000 VND/năm",
        "subjectCombinations": ["A00", "A01", "D01"],
        "careerOutcomes": "..."
      }
    ],
    "summary": "Chương trình A có điểm chuẩn cao hơn nhưng học phí thấp hơn..."
  },
  "message": "OK",
  "traceId": "..."
}
```

## 8. Documents API

### POST `/admin/documents`

Quyền: Staff/Admin

Content-Type: `multipart/form-data`

Fields:

- `file`
- `title`
- `documentType`
- `source`
- `admissionCycleId` optional

Response:

```json
{
  "success": true,
  "data": {
    "documentId": "uuid",
    "versionId": "uuid",
    "ingestionJobId": "uuid",
    "processingStatus": "pending"
  },
  "message": "Upload thành công, tài liệu đang được xử lý",
  "traceId": "..."
}
```

### GET `/admin/documents`

Quyền: Staff/Admin

Query:

- `keyword`
- `documentType`
- `status`
- `page`
- `pageSize`

### GET `/admin/documents/{id}`

Quyền: Staff/Admin

Trả chi tiết document, versions, chunk count, ingestion status.

### POST `/admin/documents/{id}/reindex`

Quyền: Admin

Request:

```json
{
  "reason": "Tài liệu được cập nhật",
  "modelConfigId": "uuid"
}
```

### PATCH `/admin/documents/{id}/status`

Quyền: Admin

Request:

```json
{
  "status": "inactive"
}
```

### GET `/admin/ingestion-jobs`

Quyền: Staff/Admin

Query:

- `status`
- `documentId`
- `page`
- `pageSize`

## 9. Chat API

### GET `/chat/conversations`

Quyền: Authenticated

Query:

- `page`
- `pageSize`
- `status`

### POST `/chat/conversations`

Quyền: Authenticated

Request:

```json
{
  "title": "Tư vấn ngành CNTT"
}
```

### GET `/chat/conversations/{id}`

Quyền: Owner/Staff/Admin

Response gồm conversation và messages.

### PATCH `/chat/conversations/{id}`

Quyền: Owner

Request:

```json
{
  "title": "Hỏi về học phí CNTT"
}
```

### DELETE `/chat/conversations/{id}`

Quyền: Owner

Soft delete/archive.

### POST `/chat/conversations/{id}/messages`

Quyền: Owner

Request:

```json
{
  "content": "Ngành Công nghệ thông tin năm 2026 học phí bao nhiêu?",
  "attachmentIds": [],
  "modelConfigId": "uuid"
}
```

Response:

```json
{
  "success": true,
  "data": {
    "userMessage": {
      "id": "uuid",
      "content": "Ngành Công nghệ thông tin năm 2026 học phí bao nhiêu?",
      "createdAt": "2026-06-25T10:00:00Z"
    },
    "assistantMessage": {
      "id": "uuid",
      "content": "Theo bảng học phí tuyển sinh 2026, ngành Công nghệ thông tin...",
      "confidence": 0.84,
      "requiresHandoff": false,
      "sources": [
        {
          "sourceType": "document",
          "sourceTitle": "Học phí 2026.pdf",
          "pageNumber": 3,
          "score": 0.9123,
          "snippet": "..."
        }
      ],
      "latencyMs": 7200
    }
  },
  "message": "OK",
  "traceId": "..."
}
```

### POST `/chat/conversations/{id}/attachments`

Quyền: Owner

Content-Type: `multipart/form-data`

Fields:

- `file`

Response:

```json
{
  "success": true,
  "data": {
    "attachmentId": "uuid",
    "fileName": "hoc-ba.jpg",
    "processingStatus": "pending"
  },
  "message": "Upload file thành công",
  "traceId": "..."
}
```

### POST `/chat/messages/{id}/feedback`

Quyền: Authenticated, owner của conversation

Request:

```json
{
  "rating": "not_helpful",
  "reason": "no_source",
  "comment": "Câu trả lời chưa có nguồn học phí"
}
```

### POST `/chat/conversations/{id}/handoff`

Quyền: Owner

Request:

```json
{
  "reason": "Tôi muốn gặp tư vấn viên",
  "priority": "normal"
}
```

## 10. Staff Handoff API

### GET `/staff/handoffs`

Quyền: Staff/Admin

Query:

- `status`
- `assignedToMe`
- `priority`
- `page`
- `pageSize`

### POST `/staff/handoffs/{id}/assign`

Quyền: Staff/Admin

Request:

```json
{
  "assignToUserId": "uuid"
}
```

Nếu không gửi `assignToUserId`, hệ thống gán cho staff hiện tại.

### POST `/staff/handoffs/{id}/reply`

Quyền: Staff/Admin được assign

Request:

```json
{
  "content": "Chào em, ngành CNTT hiện có các phương thức xét tuyển sau..."
}
```

### PATCH `/staff/handoffs/{id}/status`

Quyền: Staff/Admin

Request:

```json
{
  "status": "resolved",
  "note": "Đã tư vấn xong"
}
```

## 11. AI/Evaluation API

### GET `/admin/ai/model-configs`

Quyền: Admin

### POST `/admin/ai/model-configs`

Quyền: Admin

Request:

```json
{
  "name": "baseline-rag-v1",
  "llmModel": "local-or-provider-model",
  "embeddingModel": "sentence-transformers/all-MiniLM-L6-v2",
  "rerankerModel": null,
  "chunkSize": 800,
  "chunkOverlap": 120,
  "topK": 5,
  "temperature": 0.2,
  "isActive": true
}
```

### POST `/admin/ai/golden-questions`

Quyền: Admin

Request:

```json
{
  "question": "Ngành CNTT năm 2026 xét tuyển những tổ hợp nào?",
  "expectedAnswer": "A00, A01, D01...",
  "expectedSource": "Quy chế tuyển sinh 2026.pdf trang 12",
  "category": "subject_combination",
  "difficulty": "easy"
}
```

### POST `/admin/ai/evaluation-runs`

Quyền: Admin

Request:

```json
{
  "name": "So sánh topK=5 baseline",
  "modelConfigId": "uuid",
  "questionCategory": null
}
```

### GET `/admin/ai/evaluation-runs/{id}`

Quyền: Admin

Response gồm summary metrics và từng question result.

### POST `/admin/ai/intent-examples`

Quyền: Admin

Request:

```json
{
  "text": "Học phí ngành CNTT là bao nhiêu?",
  "intentLabel": "tuition",
  "source": "manual",
  "isVerified": true
}
```

## 12. Dashboard API

### GET `/admin/dashboard/summary`

Quyền: Staff/Admin

Response:

```json
{
  "success": true,
  "data": {
    "totalUsers": 120,
    "totalConversations": 350,
    "openHandoffs": 8,
    "documentsIndexed": 24,
    "positiveFeedbackRate": 0.78,
    "avgAiLatencyMs": 6400
  },
  "message": "OK",
  "traceId": "..."
}
```

### GET `/admin/dashboard/top-questions`

Quyền: Staff/Admin

Query:

- `from`
- `to`
- `limit`

## 13. Internal AI Service Contract

Backend API gọi AI service nội bộ, không expose trực tiếp cho frontend.

### POST `/internal/rag/answer`

Request:

```json
{
  "conversationId": "uuid",
  "messageId": "uuid",
  "question": "Ngành CNTT học phí bao nhiêu?",
  "userProfile": {
    "role": "student",
    "expectedScore": 24.5,
    "interestedSubjectGroup": "A01"
  },
  "attachments": [
    {
      "attachmentId": "uuid",
      "filePath": "uploads/chat/file.pdf",
      "fileType": "pdf"
    }
  ],
  "modelConfig": {
    "embeddingModel": "sentence-transformers/all-MiniLM-L6-v2",
    "topK": 5,
    "chunkSize": 800,
    "temperature": 0.2
  }
}
```

Response:

```json
{
  "answer": "Theo tài liệu...",
  "confidence": 0.84,
  "requiresHandoff": false,
  "intent": "tuition",
  "sources": [
    {
      "documentChunkId": "uuid",
      "sourceType": "document",
      "sourceTitle": "Học phí 2026.pdf",
      "pageNumber": 3,
      "score": 0.91,
      "snippet": "..."
    }
  ],
  "latencyMs": 7200,
  "debug": {
    "retrievalTopK": 5,
    "usedReranker": false
  }
}
```

### POST `/internal/documents/ingest`

Request:

```json
{
  "documentVersionId": "uuid",
  "filePath": "uploads/documents/quy-che.pdf",
  "fileType": "pdf",
  "documentTitle": "Quy chế tuyển sinh 2026",
  "documentType": "regulation",
  "modelConfigId": "uuid"
}
```

Response:

```json
{
  "status": "completed",
  "chunkCount": 128,
  "qdrantCollection": "admissions_docs",
  "errorMessage": null
}
```

## 14. Ghi chú bảo mật API

- Admin API phải có `[Authorize(Roles = "admin")]`.
- Staff API phải có `[Authorize(Roles = "staff,admin")]`.
- Conversation API phải kiểm tra owner hoặc staff/admin quyền phù hợp.
- Upload phải validate MIME type và extension.
- Không trả `password_hash`, `refresh_token`, internal file path nhạy cảm ra frontend.
- Tất cả lỗi server trả message chung, chi tiết lỗi ghi log.
