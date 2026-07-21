# 17. Staff Handoff va Live Support

## Muc tieu

Bo sung luong chuyen tiep cho tu van vien khi chatbot tra loi khong chac hoac bi nguoi dung danh gia `Chua dung`.

## Chuc nang da them

- Khi nguoi dung gui feedback `negative`, backend tu tao `handoff ticket`.
- Co API tao ticket thu cong tu `assistantMessageId`.
- Staff/admin xem danh sach ticket.
- Staff/admin gui phan hoi cho ticket.
- Staff/admin co the doi trang thai ticket: `open`, `in_progress`, `resolved`, `closed`.
- Frontend admin co panel `Live support tickets`.
- Frontend public hien thong bao ma ticket sau khi feedback am tinh tao handoff.

## Bang du lieu moi

| Bang | Muc dich |
|---|---|
| `handoff_tickets` | Luu ticket can tu van vien xu ly |
| `handoff_messages` | Luu phan hoi qua lai trong ticket |

Migration da tao va apply:

```text
20260626055658_HandoffSupportSchema
```

## API moi

### Public

```http
POST /api/handoff/tickets
```

Tao ticket thu cong tu mot assistant message.

### Admin / Staff

```http
GET /api/admin/handoff/tickets
GET /api/admin/handoff/tickets/{id}
POST /api/admin/handoff/tickets/{id}/reply
PATCH /api/admin/handoff/tickets/{id}/status
```

## Tich hop voi feedback

`POST /api/chat/messages/{assistantMessageId}/feedback` khi nhan:

```json
{
  "rating": "negative",
  "note": "Can bo sung hoc phi moi nhat va dan dung trang tai lieu."
}
```

Backend se:

1. Luu feedback.
2. Tao ticket neu chua co ticket dang mo cho assistant message do.
3. Tra ve `handoffTicketId` trong response feedback.

## Ket qua kiem tra

| Hang muc | Ket qua |
|---|---|
| Backend build | Pass, 0 warning, 0 error |
| Frontend build | Pass |
| EF migration add | Pass |
| EF database update LocalDB | Pass |
| API start | Pass, `http://localhost:5000` |
| AI service dang chay | Pass, `http://localhost:8000` |
| Frontend dev server | Khong can cho HTTP handoff test; frontend build da pass |
| HTTP end-to-end handoff test | Pass |

## Ket qua HTTP end-to-end

Da chay thanh cong luong:

1. `POST /api/rag/chat` de lay `assistantMessageId`.
2. `POST /api/chat/messages/{assistantMessageId}/feedback` voi `rating=negative`.
3. Response co `handoffTicketId`.
4. `GET /api/admin/handoff/tickets` bang admin token.
5. `POST /api/admin/handoff/tickets/{handoffTicketId}/reply` voi `resolve=true`.
6. Ticket chuyen sang `resolved` va co message cua staff.

Lan test gan nhat:

| Field | Gia tri |
|---|---|
| `assistantMessageId` | `26743bfc-e120-4e2e-980e-80724599fc15` |
| `feedbackRating` | `negative` |
| `handoffTicketId` | `77ec28de-c5ae-486a-bac9-9439983d9a2b` |
| `replyStatus` | `resolved` |
| `replyMessages` | `2` |

## Y nghia voi do an

Module nay lam ro phan van hanh thuc te:

- Chatbot khong thay the hoan toan tu van vien.
- Cau tra loi bi danh gia sai duoc dua vao quy trinh xu ly cua nguoi that.
- Ticket va feedback la nguon du lieu de cai thien golden questions, retrieval, chunking va noi dung tai lieu.
