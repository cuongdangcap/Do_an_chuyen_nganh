# 18. Chat History va ChatGPT-like UI

## Muc tieu

Hoan thien trai nghiem chat giong ChatGPT hon:

- Guest/user co `clientSessionId` rieng.
- Luu hoi thoai da co trong SQL Server duoc doc lai qua API.
- Frontend co sidebar lich su hoi thoai.
- Nguoi dung co the mo hoi thoai cu hoac tao chat moi.
- Message list hien user/assistant message va source chunks.

## Backend API moi

```http
GET /api/chat/conversations?clientSessionId=<session>&page=1&pageSize=20
GET /api/chat/conversations/{id}?clientSessionId=<session>
```

Quyen doc:

- Neu co authenticated user: doc conversation cua user do.
- Neu la guest: chi doc conversation co cung `clientSessionId`.

## Frontend

Panel `Tro ly RAG tuyen sinh` duoc nang cap:

- Sidebar lich su chat.
- Nut `Chat moi`.
- Khung message list.
- Cau tra loi assistant hien sources.
- Feedback `Huu ich / Chua dung`; neu bam `Chua dung` thi mo o nhap gop y truoc khi gui.
- Handoff ticket van giu nguyen va tiep tuc duoc tao tu feedback am.

## Ket qua test local

| Test | Ket qua |
|---|---|
| Backend build | Pass, 0 warning, 0 error |
| Frontend build | Pass |
| `POST /api/rag/chat` voi `clientSessionId` | Pass |
| `GET /api/chat/conversations` | Pass, tra ve 1 conversation |
| `GET /api/chat/conversations/{id}` | Pass, tra ve 2 messages |
| Source chunks trong assistant message | Pass, 2 sources |
| Frontend dev server | Pass, `http://127.0.0.1:5173` |

Lan smoke test gan nhat:

| Field | Gia tri |
|---|---|
| `conversationId` | `09ca7e59-93fd-48eb-b8e7-128d0ade9102` |
| `listTotal` | `1` |
| `detailMessages` | `2` |
| `firstRole` | `user` |
| `lastRole` | `assistant` |
| `sources` | `2` |

## Y nghia voi do an

Module nay gan voi yeu cau "trang web giong ChatGPT":

- Khong chi co mot form hoi dap, ma co lich su hoi thoai.
- Moi cau tra loi van co citation/source.
- Feedback va handoff duoc gan truc tiep voi assistant message trong conversation, kem ghi chu gop y neu cau tra loi chua dung.
