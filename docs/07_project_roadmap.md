# 07. Project Roadmap

## 1. Mục tiêu roadmap

Roadmap này biến thiết kế tổng thể thành kế hoạch triển khai theo từng phase. Mỗi phase có:

- Mục tiêu rõ ràng.
- Đầu ra kiểm chứng được.
- Module liên quan.
- Điều kiện hoàn thành.
- Rủi ro chính.

Nguyên tắc triển khai: đi từ nền tảng hệ thống đến nghiệp vụ tuyển sinh, sau đó mới đến AI/RAG và hoàn thiện báo cáo. Không code UI quá sâu khi database/API chưa chốt.

## 2. Tổng quan phase

| Phase | Tên | Kết quả chính |
|---|---|---|
| 0 | Chuẩn bị dữ liệu và POC | Tài liệu mẫu, seed data, golden questions, POC RAG nhỏ |
| 1 | Setup nền tảng | Repo structure, Docker, backend, frontend, AI service chạy được |
| 2 | Auth/RBAC | Đăng ký, đăng nhập, JWT, role, profile |
| 3 | Dữ liệu tuyển sinh | CRUD ngành, chương trình, điểm chuẩn, học phí, FAQ |
| 4 | Public/Admin Portal | UI tra cứu và UI quản trị dữ liệu |
| 5 | Document Ingestion | Upload PDF/DOCX/ảnh, parse, chunk, lưu metadata |
| 6 | RAG + Qdrant | Embedding, vector search, answer with citations |
| 7 | ChatGPT-like UI | Hội thoại, upload file chat, feedback, lịch sử |
| 8 | Staff Handoff | Ticket, staff reply, trạng thái xử lý |
| 9 | AI Evaluation | Golden questions, metrics, so sánh cấu hình |
| 10 | Hoàn thiện | Test, logging, docs, demo, slide, video |

## 3. Phase 0: Chuẩn bị dữ liệu và POC

### Mục tiêu

Giảm rủi ro AI trước khi code nhiều. Kiểm tra sớm rằng tài liệu tuyển sinh có thể đọc, chunk, embedding và trả lời có nguồn.

### Việc cần làm

- Thu thập 5-10 tài liệu tuyển sinh mẫu: quy chế, học phí, FAQ, thông báo xét tuyển.
- Chuẩn bị seed data ban đầu: khoa, ngành, chương trình, điểm chuẩn, học phí.
- Chuẩn bị 50 golden questions đầu tiên.
- Chạy thử parse PDF/DOCX/ảnh.
- Chạy thử embedding + FAISS/Qdrant với vài câu hỏi.
- Ghi lại lỗi OCR/tài liệu khó đọc.

### Đầu ra

- `data/sample_documents/`
- `data/seed/admissions_seed.xlsx` hoặc `.csv`
- `data/evaluation/golden_questions.csv`
- Ghi chú POC RAG: câu nào trả lời đúng/sai, nguồn nào được lấy.

### Hoàn thành khi

- Có ít nhất 5 tài liệu mẫu.
- Có ít nhất 10 ngành/chương trình mẫu.
- Có ít nhất 50 câu hỏi đánh giá.
- POC retrieval lấy được chunk liên quan cho một số câu hỏi cơ bản.

## 4. Phase 1: Setup nền tảng

### Mục tiêu

Tạo bộ khung kỹ thuật chạy được local.

### Module

- Frontend.
- ASP.NET Core API.
- Python AI service.
- SQL Server.
- Qdrant.
- Docker Compose.

### Đầu ra

- Solution backend build được.
- Frontend chạy được.
- AI service có health check.
- SQL Server và Qdrant chạy bằng Docker.
- `.env.example`.
- README chạy local.

### Hoàn thành khi

- `docker compose up` chạy được các service nền.
- API `/health` trả OK.
- AI service `/health` trả OK.
- Swagger mở được.

## 5. Phase 2: Auth/RBAC

### Mục tiêu

Xây nền tảng tài khoản học sinh, phụ huynh, staff, admin.

### Module

- Users.
- Roles.
- Profiles.
- JWT.
- Refresh token.
- RBAC middleware/guard.

### Đầu ra

- Register student/parent.
- Login/logout/refresh.
- `/auth/me`.
- Admin tạo staff.
- Profile API.
- Role-based protection.

### Hoàn thành khi

- Student không gọi được API admin.
- Admin tạo/khóa được staff.
- Token hết hạn refresh được.
- Password được hash.

## 6. Phase 3: Dữ liệu tuyển sinh

### Mục tiêu

Xây lõi nghiệp vụ tuyển sinh có dữ liệu quan hệ trong SQL Server.

### Module

- Faculties.
- Majors.
- Programs.
- Admission cycles.
- Admission methods.
- Subject combinations.
- Cutoff scores.
- Tuition fees.
- Scholarships.
- FAQ.

### Đầu ra

- Migration đầy đủ.
- Seed data.
- API public tra cứu.
- API admin CRUD.
- API compare programs bản đầu.

### Hoàn thành khi

- Public API lọc ngành theo tổ hợp/điểm/học phí.
- Admin CRUD được điểm chuẩn/học phí.
- Chi tiết ngành hiển thị đủ thông tin liên quan.

## 7. Phase 4: Public/Admin Portal

### Mục tiêu

Có giao diện dùng được cho hai nhóm: người tìm hiểu tuyển sinh và quản trị viên.

### Public portal

- Trang danh sách ngành.
- Trang chi tiết ngành.
- Bộ lọc/tìm kiếm.
- Trang điểm chuẩn/học phí.
- FAQ.

### Admin portal

- Login admin/staff.
- Quản lý ngành/chương trình.
- Quản lý điểm chuẩn/học phí.
- Quản lý FAQ.
- Quản lý user cơ bản.

### Hoàn thành khi

- User không cần Postman để tra cứu dữ liệu.
- Admin nhập/sửa dữ liệu trực tiếp từ web.
- UI responsive ở mức cơ bản.

## 8. Phase 5: Document Ingestion

### Mục tiêu

Admin/staff upload tài liệu, hệ thống xử lý thành chunk có metadata.

### Module

- Document upload.
- File storage.
- Ingestion job.
- PDF parser.
- DOCX parser.
- OCR.
- Text cleaning.
- Chunking.

### Đầu ra

- API upload tài liệu.
- Admin xem danh sách tài liệu và trạng thái xử lý.
- SQL Server lưu document/version/chunks.
- Job failed có log lỗi.

### Hoàn thành khi

- Upload PDF text tạo chunks.
- Upload DOCX tạo chunks.
- Upload ảnh rõ chữ có extracted text.
- Admin bật/tắt tài liệu khỏi RAG được.

## 9. Phase 6: RAG + Qdrant

### Mục tiêu

Chatbot trả lời câu hỏi tuyển sinh dựa trên tài liệu và database, có nguồn.

### Module

- Embedding.
- Qdrant collection.
- Vector search.
- Structured data retrieval.
- Prompt builder.
- LLM connector.
- Grounding/confidence check.

### Đầu ra

- Document chunks được upsert vào Qdrant.
- Câu hỏi được embed và search top-k.
- AI trả answer + sources + confidence.
- Fallback khi thiếu nguồn.

### Hoàn thành khi

- Hỏi học phí/điểm chuẩn ưu tiên dữ liệu SQL Server.
- Hỏi quy chế/hồ sơ dùng RAG tài liệu.
- Câu trả lời quan trọng có citation.
- Low confidence tạo handoff hoặc hỏi lại.

## 10. Phase 7: ChatGPT-like UI

### Mục tiêu

Tạo trải nghiệm chat giống ChatGPT trong miền tuyển sinh.

### Chức năng

- Sidebar hội thoại.
- Tạo/xóa/đổi tên hội thoại.
- Message list.
- Input box.
- Upload file trong chat.
- Loading state.
- Hiển thị nguồn.
- Feedback answer.

### Hoàn thành khi

- Student/parent chat được bằng web.
- Hội thoại được lưu và mở lại.
- File user upload được parse làm context tạm.
- Feedback hiển thị ở admin.

## 11. Phase 8: Staff Handoff

### Mục tiêu

Khi AI không chắc hoặc user yêu cầu, staff có thể tiếp quản.

### Chức năng

- Tạo ticket.
- Staff queue.
- Assign ticket.
- Staff reply.
- Update status.
- User thấy phản hồi.

### Hoàn thành khi

- User tạo yêu cầu gặp tư vấn viên.
- Staff nhận và phản hồi.
- Ticket có trạng thái rõ.
- Admin xem được lịch sử xử lý.

## 12. Phase 9: AI Evaluation

### Mục tiêu

Chứng minh phần học máy không chỉ là LLM bằng evaluation và so sánh cấu hình.

### Chức năng

- Golden questions.
- Model configs.
- Evaluation runs.
- Retrieval metrics.
- Answer quality manual score.
- So sánh FAISS/Qdrant hoặc chunk/top-k.
- Intent classifier baseline.

### Hoàn thành khi

- Có ít nhất 50 golden questions.
- Có ít nhất 2 evaluation runs.
- Có bảng kết quả retrieval hit@k, citation correctness, latency.
- Có confusion matrix hoặc metrics cho intent classifier nếu train.

## 13. Phase 10: Hoàn thiện demo và báo cáo

### Mục tiêu

Đưa hệ thống đến trạng thái bảo vệ được.

### Việc cần làm

- Kiểm thử luồng chính.
- Fix bug.
- Logging/audit.
- README.
- Hướng dẫn chạy local.
- API documentation.
- Database schema.
- AI evaluation report.
- Slide thuyết trình.
- Video demo.

### Hoàn thành khi

- Người mới đọc README chạy được project.
- Có tài khoản demo cho student, parent, staff, admin.
- Có video demo các flow chính.
- Có số liệu evaluation AI.

## 14. Mốc demo đề xuất

| Demo | Nội dung |
|---|---|
| Demo 1 | Auth + public portal + admin CRUD ngành/điểm chuẩn |
| Demo 2 | Upload tài liệu + ingestion + Qdrant search |
| Demo 3 | Chat UI + RAG answer with citations |
| Demo 4 | Feedback + staff handoff + dashboard |
| Demo 5 | Evaluation report + hoàn thiện báo cáo |

## 15. Rủi ro theo phase

| Phase | Rủi ro | Cách xử lý |
|---|---|---|
| 0 | Tài liệu mẫu không đủ | Tạo dữ liệu giả lập nhưng ghi rõ nguồn demo |
| 1 | Docker SQL Server nặng | Có phương án SQL Server local nếu Docker yếu |
| 2 | Auth/RBAC lỗi quyền | Viết test role sớm |
| 3 | Database đổi nhiều | Dùng migration và seed có kiểm soát |
| 5 | OCR kém | Cho phép admin preview/sửa hoặc đánh failed |
| 6 | RAG trả lời sai | Citation + confidence + evaluation |
| 7 | Chat UI phức tạp | Làm bản đơn giản trước, streaming để sau |
| 8 | Realtime khó | Dùng polling trước, SignalR sau |
| 9 | Không đủ câu hỏi test | Tạo golden questions ngay từ phase 0 |

