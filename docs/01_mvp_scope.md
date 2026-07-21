# 01. Phạm vi MVP

## 1. Mục tiêu MVP

MVP của đồ án là một hệ thống tư vấn tuyển sinh đại học có đủ ba phần:

1. Cổng thông tin tuyển sinh cho học sinh/phụ huynh tra cứu ngành, chương trình, điểm chuẩn, học phí và FAQ.
2. Cổng quản trị cho nhà trường quản lý dữ liệu tuyển sinh, tài khoản, tài liệu, hội thoại, feedback.
3. Chatbot AI dạng ChatGPT nội bộ, trả lời câu hỏi tuyển sinh dựa trên dữ liệu database và tài liệu PDF/DOCX/ảnh đã được xử lý bằng pipeline RAG.

MVP phải chứng minh được cả hai môn:

- Công nghệ lập trình Web: REST API, auth, RBAC, SQL Server, ORM, validation, response chuẩn, logging, admin portal, chat UI, upload file.
- Học máy và Khai phá dữ liệu: embedding, vector search, retrieval, RAG, intent classification, reranking hoặc benchmark retrieval, evaluation, feedback mining.

## 2. Đối tượng người dùng

| Actor | Mô tả |
|---|---|
| Guest | Người chưa đăng nhập, chỉ xem thông tin public và hỏi chatbot giới hạn |
| Student | Học sinh quan tâm tuyển sinh, có hồ sơ cá nhân và lịch sử chat |
| Parent | Phụ huynh, có thể hỏi thông tin học phí/chính sách/hồ sơ |
| Staff | Nhân viên tư vấn của trường, tiếp nhận hội thoại và phản hồi trực tiếp |
| Admin | Quản trị viên tuyển sinh, quản lý dữ liệu, tài khoản, tài liệu, AI config |
| System Admin | Quản trị kỹ thuật, quản lý cấu hình, log, backup, vận hành |

## 3. Chức năng bắt buộc P0

### 3.1. Auth và phân quyền

- Seed/cấp sẵn tài khoản sinh viên theo định dạng `BIT######@st.cmcu.edu.vn`; sinh viên không tự đăng ký.
- Đăng ký phụ huynh.
- Đăng nhập bằng email/password.
- JWT access token và refresh token.
- Phân quyền theo role: student, parent, staff, admin.
- Admin tạo/sửa/khóa tài khoản staff.
- User cập nhật hồ sơ cá nhân.

### 3.2. Cổng thông tin tuyển sinh

- Xem danh sách ngành/chương trình.
- Tìm kiếm ngành theo từ khóa.
- Lọc theo tổ hợp, khoảng điểm, học phí, campus.
- Xem chi tiết ngành: mô tả, tổ hợp, điểm chuẩn, học phí, cơ hội nghề nghiệp.
- Xem điểm chuẩn theo năm/phương thức.
- Xem FAQ tuyển sinh.

### 3.3. Cổng quản trị

- CRUD khoa/ngành/chương trình.
- CRUD phương thức xét tuyển.
- CRUD tổ hợp xét tuyển.
- CRUD điểm chuẩn.
- CRUD học phí.
- CRUD FAQ.
- Upload tài liệu tuyển sinh PDF/DOCX/ảnh.
- Xem trạng thái xử lý tài liệu.
- Bật/tắt tài liệu khỏi kho tri thức.
- Xem danh sách hội thoại và feedback.

### 3.4. Chat AI

- Giao diện chat giống ChatGPT: sidebar hội thoại, khung chat, ô nhập, nút upload.
- Tạo hội thoại mới.
- Lưu lịch sử hội thoại.
- Gửi câu hỏi dạng text.
- Upload PDF/DOCX/ảnh trong hội thoại.
- AI trả lời dựa trên RAG và dữ liệu tuyển sinh.
- Câu trả lời có nguồn: tên tài liệu, trang/chunk, điểm liên quan.
- Feedback câu trả lời: hữu ích/không hữu ích + ghi chú.
- AI chuyển sang staff khi không đủ tự tin.

### 3.5. Staff handoff

- Staff xem danh sách hội thoại cần hỗ trợ.
- Staff nhận xử lý một ticket.
- Staff phản hồi người dùng.
- Đổi trạng thái ticket: open, assigned, resolved, closed.

### 3.6. AI/RAG tối thiểu

- Parse PDF text.
- Parse DOCX.
- OCR ảnh hoặc PDF scan ở mức cơ bản.
- Chunk tài liệu.
- Tạo embedding.
- Lưu vector vào Qdrant.
- Tìm kiếm top-k chunk liên quan.
- Rerank hoặc benchmark top-k nếu chưa kịp train reranker.
- Prompt LLM trả lời có nguồn.
- Evaluation bằng bộ câu hỏi chuẩn.

## 4. Chức năng nên có P1

- So sánh ngành/chương trình theo điểm chuẩn, học phí, tổ hợp, cơ hội nghề nghiệp.
- Dashboard thống kê câu hỏi phổ biến.
- Dashboard tỷ lệ feedback tích cực/tiêu cực.
- Train intent classifier từ dữ liệu câu hỏi mẫu.
- FAISS benchmark để so sánh với Qdrant trên cùng bộ câu hỏi.
- Export hội thoại hoặc báo cáo tư vấn.
- SignalR realtime cho staff reply.

## 5. Chức năng mở rộng P2

- Đăng nhập Google/OAuth2.
- Gợi ý ngành cá nhân hóa theo điểm, sở thích, tổ hợp.
- Fine-tune reranker hoặc classifier.
- Streaming response thật từ LLM.
- Multi-tenant cho nhiều trường.
- App mobile native.
- Tự động crawl dữ liệu từ website trường.

## 6. Ngoài phạm vi

- Không tự train LLM lớn từ đầu.
- Không thay thế quyết định chính thức của phòng tuyển sinh.
- Không tích hợp thanh toán hồ sơ/xét tuyển.
- Không đảm bảo OCR chính xác với ảnh mờ, nghiêng, thiếu sáng.
- Không làm microservices đầy đủ trong MVP.
- Không xử lý mọi định dạng file; MVP ưu tiên PDF, DOCX, PNG/JPG.

## 7. Tiêu chí hoàn thành MVP

| Nhóm | Tiêu chí nghiệm thu |
|---|---|
| Web | User đăng ký/đăng nhập được; role kiểm soát đúng quyền |
| Web | Public portal tra cứu được ngành, điểm chuẩn, học phí |
| Web | Admin CRUD được dữ liệu tuyển sinh và tài liệu |
| Web | API có Swagger, validation, response lỗi chuẩn |
| Web | Dữ liệu nghiệp vụ lưu trong SQL Server qua EF Core |
| Chat | User tạo hội thoại, gửi câu hỏi, upload file, xem lịch sử |
| AI | Tài liệu được parse, chunk, embedding, lưu Qdrant |
| AI | Chatbot trả lời có citations từ tài liệu/database |
| AI | Có bộ test câu hỏi và báo cáo evaluation |
| Staff | Staff xem ticket, phản hồi và đóng ticket |
| Docs | Có README, SRS, DB design, API contract, AI pipeline, diagrams |

## 8. Quyết định công nghệ đã chốt

| Thành phần | Công nghệ |
|---|---|
| Frontend | React + Vite hoặc Next.js |
| Backend API | ASP.NET Core Web API |
| ORM | Entity Framework Core |
| Relational DB | SQL Server |
| Vector DB | Qdrant |
| Vector benchmark | FAISS |
| AI service | Python FastAPI |
| Auth | JWT + refresh token + RBAC |
| Realtime | SignalR hoặc polling trong MVP |
| Deploy local | Docker Compose |

## 9. Thứ tự triển khai sau khi thiết kế

1. Setup repo, env, Docker Compose.
2. Setup SQL Server schema bằng EF Core migration.
3. Auth/RBAC.
4. Module tuyển sinh CRUD.
5. Public portal + admin portal.
6. Upload tài liệu + ingestion.
7. Qdrant vector search + RAG answer.
8. Chat UI + history + feedback.
9. Staff handoff.
10. Evaluation + báo cáo AI.
