# 16. Evaluation Runner va Golden Questions

## Muc tieu

Tao module danh gia RAG co the chay lap lai de chung minh phan Hoc may/Khai pha du lieu:

- Quan ly bo cau hoi chuan golden questions.
- Chay tu dong qua retrieval RAG.
- Do `hit@k`, source hit, keyword hit rate, top score, latency.
- Luu ket qua dung/sai tung cau vao SQL Server.
- Hien thi metric trong cong quan tri.

## Bang du lieu moi

| Bang | Muc dich |
|---|---|
| `evaluation_questions` | Bo cau hoi chuan, expected answer, keywords, source ky vong |
| `evaluation_runs` | Mot lan chay evaluation va metric tong hop |
| `evaluation_results` | Ket qua tung cau: source, keyword match, latency, correct/incorrect |

Migration da tao va apply:

```text
20260626053611_EvaluationSchema
```

## API moi

```http
GET /api/admin/evaluation/questions
POST /api/admin/evaluation/questions
POST /api/admin/evaluation/questions/seed-defaults
GET /api/admin/evaluation/runs
GET /api/admin/evaluation/runs/{id}
POST /api/admin/evaluation/runs
```

Tat ca endpoint can role `admin` hoac `staff`.

## Cach cham diem

Moi golden question co:

- `expectedKeywords`
- `expectedSourceTitle`
- `expectedDocumentType`

Mot cau duoc xem la dung khi:

- Co source hit trong top-k.
- Keyword hit rate dat toi thieu 50%.

Metric tong hop:

- `HitRateAtK`
- `AverageKeywordHitRate`
- `AverageTopScore`
- `AverageLatencyMs`
- `CorrectQuestions / TotalQuestions`

## Frontend

Cong quan tri co them panel `Evaluation RAG`:

- Xem so golden questions.
- Seed cau hoi mau.
- Chay evaluation.
- Xem metric tong hop.
- Xem ket qua tung cau dung/sai.

## Ket qua test local

| Test | Ket qua |
|---|---|
| Backend build | Pass, 0 warning, 0 error |
| Frontend build | Pass |
| EF migration add | Pass |
| EF database update LocalDB | Pass |
| Seed default golden questions | Pass, 4 cau |
| Run evaluation | Pass |
| List evaluation runs | Pass |
| Frontend smoke test | Pass |

Ket qua lan chay dau:

| Metric | Gia tri |
|---|---|
| Total questions | 4 |
| Correct questions | 4 |
| Hit@K | 100% |
| Average keyword hit rate | 100% |
| Average latency | Khoang 2206ms |
| Vector backend | `local` fallback |

## Y nghia voi bao cao

Module nay giup trinh bay ro rang:

- RAG khong chi tra loi cam tinh, ma co quy trinh danh gia.
- Co du lieu do luong de so sanh cau hinh chunking/embedding/vector search.
- Feedback nguoi dung co the bien thanh golden questions moi.
- Khi thay Qdrant/embedding model/reranking, co the chay lai evaluation de chung minh cai tien.
