# 15. Chat History va Feedback RAG

## Muc tieu

Bo sung nen tang du lieu de chung minh he thong khong chi la chatbot tra loi tuc thoi:

- Luu hoi thoai chat RAG.
- Luu cau hoi, cau tra loi, source chunks va latency.
- Cho nguoi dung danh gia cau tra loi huu ich/chua dung.
- Cho admin/staff xem feedback de khai pha cau hoi loi va cai thien RAG.

## Bang du lieu moi

| Bang | Muc dich |
|---|---|
| `chat_conversations` | Luu phien hoi thoai cua guest/user |
| `chat_messages` | Luu user message va assistant message |
| `chat_message_sources` | Luu chunks/source da dung de tra loi |
| `chat_feedback` | Luu danh gia positive/negative va ghi chu |

Migration da tao va apply:

```text
20260626051900_ChatFeedbackSchema
```

## API moi

### Chat RAG co luu lich su

`POST /api/rag/chat`

Response bo sung:

- `conversationId`
- `userMessageId`
- `assistantMessageId`
- `latencyMs`

### Gui feedback

```http
POST /api/chat/messages/{assistantMessageId}/feedback
Content-Type: application/json

{
  "rating": "positive",
  "note": "Tra loi co nguon phu hop."
}
```

### Admin xem feedback

```http
GET /api/admin/chat/feedback?page=1&pageSize=20
Authorization: Bearer <admin_or_staff_token>
```

Co the loc:

```http
GET /api/admin/chat/feedback?rating=negative
```

## Frontend

- Cong thong tin: them nut `Huu ich` va `Chua dung` duoi cau tra loi RAG.
- Khi nguoi dung bam `Chua dung`, frontend mo textarea de nhap gop y cu the ve thong tin sai/thieu truoc khi gui feedback.
- Cong quan tri: them khung `Feedback chat RAG` de xem feedback gan day.
- Cong quan tri co the xem ca ghi chu feedback am de staff va admin bo sung du lieu, sua prompt hoac cap nhat tai lieu.

## Ket qua test local

| Test | Ket qua |
|---|---|
| Backend build | Pass, 0 warning, 0 error |
| Frontend build | Pass |
| EF migration add | Pass |
| EF database update LocalDB | Pass |
| `POST /api/rag/chat` | Pass, tra ve `conversationId` va `assistantMessageId` |
| `POST /api/chat/messages/{id}/feedback` | Pass, luu feedback `positive/negative` kem `note` |
| `GET /api/admin/chat/feedback` | Pass, tra ve feedback vua tao |

## Y nghia voi mon Hoc may va Khai pha du lieu

Du lieu feedback la dau vao cho cac buoc sau:

- Thong ke cau hoi bi tra loi sai/bi dislike nhieu.
- Tao bo golden questions tu cau hoi that.
- Do retrieval hit@k theo tung nhom cau hoi.
- Cai thien chunking, embedding, reranking va prompt dua tren loi thuc te.
