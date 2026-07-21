# 02. SRS và Use Case

## 1. Giới thiệu

Tài liệu này đặc tả yêu cầu phần mềm cho hệ thống Tư vấn Tuyển sinh Đại học. Đây là chuẩn để thiết kế database, API, giao diện, AI pipeline và test case.

## 2. Mục tiêu hệ thống

Hệ thống cho phép học sinh/phụ huynh tra cứu thông tin tuyển sinh và hỏi trợ lý AI. Nhà trường quản lý dữ liệu tuyển sinh, tài liệu tri thức, tài khoản, hội thoại, feedback và phản hồi trực tiếp khi AI không đủ tự tin.

## 3. Actor và quyền tổng quan

| Actor | Quyền chính |
|---|---|
| Guest | Xem public portal, hỏi chatbot giới hạn, đăng ký tài khoản |
| Student | Quản lý hồ sơ cá nhân, chat AI, upload file, lưu lịch sử, feedback |
| Parent | Chat AI, upload file, lưu lịch sử, xem thông tin tuyển sinh |
| Staff | Xem hội thoại được gán, phản hồi trực tiếp, xem tài liệu/dữ liệu tuyển sinh |
| Admin | Quản lý toàn bộ dữ liệu, tài khoản, tài liệu, AI config, evaluation |
| System Admin | Cấu hình hệ thống, backup, log, deployment |

## 4. Quy tắc phân quyền

| Tài nguyên | Guest | Student | Parent | Staff | Admin |
|---|---:|---:|---:|---:|---:|
| Xem ngành/điểm chuẩn/học phí | Có | Có | Có | Có | Có |
| Tạo hội thoại | Giới hạn | Có | Có | Có | Có |
| Xem hội thoại của mình | Không | Có | Có | Có | Có |
| Xem mọi hội thoại | Không | Không | Không | Theo phân công | Có |
| Upload file chat | Không | Có | Có | Có | Có |
| Upload tài liệu tri thức | Không | Không | Không | Có | Có |
| CRUD dữ liệu tuyển sinh | Không | Không | Không | Có giới hạn | Có |
| Quản lý tài khoản | Không | Không | Không | Không | Có |
| Xem dashboard/evaluation | Không | Không | Không | Có giới hạn | Có |

## 5. Yêu cầu chức năng

### FR-AUTH: Tài khoản và xác thực

| ID | Yêu cầu |
|---|---|
| FR-AUTH-01 | Người dùng đăng ký tài khoản học sinh bằng email, mật khẩu, họ tên |
| FR-AUTH-02 | Người dùng đăng ký tài khoản phụ huynh bằng email, mật khẩu, họ tên |
| FR-AUTH-03 | Người dùng đăng nhập và nhận JWT access token + refresh token |
| FR-AUTH-04 | Người dùng đăng xuất, refresh token bị thu hồi |
| FR-AUTH-05 | Admin tạo tài khoản staff/admin |
| FR-AUTH-06 | Admin khóa/mở khóa tài khoản |
| FR-AUTH-07 | Hệ thống hash mật khẩu, không lưu plain text |
| FR-AUTH-08 | API kiểm tra quyền theo role trước khi xử lý |

### FR-PROFILE: Hồ sơ người dùng

| ID | Yêu cầu |
|---|---|
| FR-PROFILE-01 | Student cập nhật trường THPT, tỉnh/thành, năm tốt nghiệp |
| FR-PROFILE-02 | Student cập nhật điểm dự kiến/điểm thi |
| FR-PROFILE-03 | Student lưu tổ hợp và ngành quan tâm |
| FR-PROFILE-04 | Parent cập nhật thông tin liên hệ |
| FR-PROFILE-05 | Parent có thể liên kết với hồ sơ student nếu được student/admin cho phép |
| FR-PROFILE-06 | Staff cập nhật phòng ban/chức vụ |

### FR-ADM: Thông tin tuyển sinh

| ID | Yêu cầu |
|---|---|
| FR-ADM-01 | Guest/user xem danh sách ngành/chương trình |
| FR-ADM-02 | Guest/user tìm kiếm ngành theo từ khóa |
| FR-ADM-03 | Guest/user lọc ngành theo tổ hợp, điểm, học phí, campus |
| FR-ADM-04 | Guest/user xem chi tiết ngành/chương trình |
| FR-ADM-05 | Guest/user xem điểm chuẩn theo năm/phương thức |
| FR-ADM-06 | Guest/user xem học phí theo năm/chương trình |
| FR-ADM-07 | Admin/staff CRUD khoa/ngành/chương trình |
| FR-ADM-08 | Admin/staff CRUD điểm chuẩn/học phí/phương thức xét tuyển/tổ hợp |
| FR-ADM-09 | Hệ thống cho phép so sánh 2-4 chương trình theo tiêu chí |

### FR-DOC: Tài liệu tuyển sinh

| ID | Yêu cầu |
|---|---|
| FR-DOC-01 | Admin/staff upload PDF/DOCX/PNG/JPG |
| FR-DOC-02 | Hệ thống lưu file gốc và metadata |
| FR-DOC-03 | Hệ thống tạo ingestion job sau khi upload |
| FR-DOC-04 | Hệ thống parse text từ PDF/DOCX |
| FR-DOC-05 | Hệ thống OCR ảnh hoặc PDF scan |
| FR-DOC-06 | Hệ thống chia text thành chunk |
| FR-DOC-07 | Hệ thống tạo embedding và lưu vào Qdrant |
| FR-DOC-08 | Admin/staff xem trạng thái xử lý tài liệu |
| FR-DOC-09 | Admin bật/tắt tài liệu khỏi RAG |
| FR-DOC-10 | Admin re-index tài liệu khi cập nhật |

### FR-CHAT: Chat AI

| ID | Yêu cầu |
|---|---|
| FR-CHAT-01 | User tạo hội thoại mới |
| FR-CHAT-02 | User xem danh sách hội thoại cũ |
| FR-CHAT-03 | User gửi tin nhắn text |
| FR-CHAT-04 | User upload file vào hội thoại |
| FR-CHAT-05 | AI trả lời dựa trên dữ liệu tuyển sinh và tài liệu |
| FR-CHAT-06 | AI trả lời kèm citations |
| FR-CHAT-07 | AI hỏi lại nếu câu hỏi thiếu thông tin |
| FR-CHAT-08 | AI từ chối hoặc chuyển staff nếu câu hỏi ngoài phạm vi/độ tin cậy thấp |
| FR-CHAT-09 | User đánh giá câu trả lời |
| FR-CHAT-10 | Hệ thống lưu message, latency, confidence, sources |

### FR-HANDOFF: Phản hồi trực tiếp

| ID | Yêu cầu |
|---|---|
| FR-HANDOFF-01 | User yêu cầu gặp tư vấn viên |
| FR-HANDOFF-02 | AI tự tạo ticket khi confidence thấp |
| FR-HANDOFF-03 | Staff xem hàng đợi ticket |
| FR-HANDOFF-04 | Staff nhận xử lý ticket |
| FR-HANDOFF-05 | Staff gửi phản hồi cho user |
| FR-HANDOFF-06 | Staff cập nhật trạng thái ticket |

### FR-AI-EVAL: Đánh giá và cải thiện AI

| ID | Yêu cầu |
|---|---|
| FR-AI-01 | Admin tạo bộ câu hỏi chuẩn và đáp án kỳ vọng |
| FR-AI-02 | Hệ thống chạy evaluation trên bộ câu hỏi |
| FR-AI-03 | Hệ thống lưu retrieval hit rate, latency, citation correctness |
| FR-AI-04 | Admin so sánh nhiều cấu hình chunk/top-k/embedding |
| FR-AI-05 | Hệ thống lưu dữ liệu feedback để phân tích cải thiện |
| FR-AI-06 | Hệ thống hỗ trợ dataset train intent classifier |

## 6. Yêu cầu phi chức năng

| ID | Nhóm | Yêu cầu |
|---|---|---|
| NFR-01 | Bảo mật | API riêng tư phải yêu cầu JWT |
| NFR-02 | Bảo mật | Mật khẩu hash bằng thuật toán an toàn |
| NFR-03 | Bảo mật | File upload bị giới hạn loại file và dung lượng |
| NFR-04 | Riêng tư | User chỉ xem được hội thoại/hồ sơ của chính mình |
| NFR-05 | Hiệu năng | API CRUD phản hồi dưới 1 giây trong demo local |
| NFR-06 | Hiệu năng | AI trả lời trong 5-15 giây với tài liệu vừa phải |
| NFR-07 | Tin cậy AI | Câu trả lời quan trọng phải có nguồn hoặc báo không đủ dữ liệu |
| NFR-08 | Khả dụng | Hệ thống chạy được bằng Docker Compose |
| NFR-09 | Bảo trì | Có Swagger, README, seed data, tài liệu schema |
| NFR-10 | Logging | Log lỗi API, ingestion, AI latency, audit admin action |
| NFR-11 | Responsive | Web chạy tốt trên desktop và mobile |
| NFR-12 | Backup | Có hướng dẫn backup SQL Server và upload files |

## 7. Use case chi tiết

### UC-01. Đăng ký tài khoản học sinh

| Mục | Nội dung |
|---|---|
| Actor | Guest |
| Tiền điều kiện | Email chưa tồn tại |
| Luồng chính | Nhập email, mật khẩu, họ tên -> hệ thống validate -> tạo user role student -> tạo student profile |
| Ngoại lệ | Email tồn tại, mật khẩu yếu, dữ liệu thiếu |
| Hậu điều kiện | User đăng nhập được và có profile student |

### UC-02. Tra cứu ngành

| Mục | Nội dung |
|---|---|
| Actor | Guest, Student, Parent |
| Tiền điều kiện | Dữ liệu ngành đã được admin nhập |
| Luồng chính | Mở danh sách ngành -> tìm/lọc -> xem chi tiết |
| Ngoại lệ | Không có kết quả, ngành bị ẩn |
| Hậu điều kiện | User xem được thông tin ngành và điểm chuẩn/học phí liên quan |

### UC-03. Chat hỏi thông tin tuyển sinh

| Mục | Nội dung |
|---|---|
| Actor | Student, Parent |
| Tiền điều kiện | User đã đăng nhập |
| Luồng chính | Tạo hội thoại -> gửi câu hỏi -> backend lưu message -> AI truy xuất RAG -> trả lời có nguồn |
| Ngoại lệ | AI không có nguồn, timeout, câu hỏi ngoài phạm vi |
| Hậu điều kiện | Message, answer, sources, latency được lưu |

### UC-04. Upload tài liệu tri thức

| Mục | Nội dung |
|---|---|
| Actor | Staff, Admin |
| Tiền điều kiện | User có quyền upload |
| Luồng chính | Chọn file -> nhập metadata -> upload -> tạo ingestion job -> parse/chunk/embed -> sẵn sàng RAG |
| Ngoại lệ | File quá lớn, sai định dạng, parse/OCR lỗi |
| Hậu điều kiện | Document/chunks/vectors được lưu hoặc job failed có log |

### UC-05. Staff phản hồi hội thoại

| Mục | Nội dung |
|---|---|
| Actor | Staff |
| Tiền điều kiện | Có handoff ticket open |
| Luồng chính | Staff nhận ticket -> xem lịch sử chat -> gửi phản hồi -> cập nhật trạng thái |
| Ngoại lệ | Ticket đã được người khác nhận, user không còn active |
| Hậu điều kiện | User nhận phản hồi; ticket được lưu trạng thái |

### UC-06. Admin chạy evaluation AI

| Mục | Nội dung |
|---|---|
| Actor | Admin |
| Tiền điều kiện | Có bộ golden questions và tài liệu đã index |
| Luồng chính | Chọn model config -> chạy evaluation -> hệ thống hỏi từng câu -> lưu metrics -> xem báo cáo |
| Ngoại lệ | AI service lỗi, Qdrant không kết nối, thiếu dữ liệu |
| Hậu điều kiện | Evaluation run và kết quả được lưu |

## 8. Business rules

- Một email chỉ có một user.
- Một user có thể có nhiều role, nhưng MVP nên dùng role chính để đơn giản.
- Student/Parent không được xem hội thoại của người khác.
- Staff chỉ xem ticket được gán hoặc ticket đang open.
- Admin có quyền xem toàn bộ dữ liệu.
- Tài liệu ở trạng thái inactive không được dùng trong RAG.
- AI chỉ trả lời câu hỏi tuyển sinh/đại học/hồ sơ/học phí/chính sách liên quan.
- Nếu câu trả lời không có nguồn đủ tốt, AI phải nói chưa đủ dữ liệu hoặc tạo handoff.
- Điểm chuẩn/học phí luôn gắn với năm tuyển sinh hoặc năm học.
- Mọi thao tác admin quan trọng phải ghi audit log.

## 9. Acceptance criteria theo nhóm

### Auth

- Đăng nhập đúng trả token.
- Đăng nhập sai trả 401.
- Student gọi API admin trả 403.
- Token hết hạn có thể refresh.

### Admissions

- Lọc ngành theo tổ hợp và khoảng điểm trả đúng.
- Admin thêm/sửa/xóa mềm ngành được.
- Chi tiết ngành hiển thị điểm chuẩn nhiều năm.

### Chat/RAG

- Câu hỏi về học phí trả lời có citation.
- Câu hỏi ngoài phạm vi trả lời từ chối lịch sự.
- Câu hỏi thiếu ngành/năm khiến AI hỏi lại.
- Feedback được lưu và hiển thị trong admin.

### Document

- Upload PDF text tạo chunks.
- Upload DOCX tạo chunks.
- Upload ảnh rõ chữ tạo text OCR.
- Job lỗi có trạng thái failed và error message.

### Evaluation

- Admin tạo ít nhất 50 golden questions.
- Evaluation lưu được metrics từng câu.
- Báo cáo so sánh ít nhất 2 cấu hình retrieval.

