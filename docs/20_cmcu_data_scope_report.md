# 20. Chốt phạm vi dữ liệu: Trường Đại học CMC

## 1. Quyết định phạm vi

Dự án được chốt theo hướng hệ thống tư vấn tuyển sinh cho **Trường Đại học CMC (CMC University)**, không phải chatbot tổng quát cho tất cả các trường đại học tại Việt Nam.

Vai trò “quản trị viên trường” trong hệ thống được hiểu là nhân viên tuyển sinh/nhân viên phụ trách dữ liệu của Trường Đại học CMC. Quản trị viên có quyền cập nhật ngành, chương trình, học phí, tài liệu RAG, phản hồi ticket hỗ trợ và theo dõi dashboard vận hành.

## 2. Nguồn dữ liệu đã dùng

Các nguồn công khai chính thức được dùng để cấu hình dữ liệu:

- Trang chủ Trường Đại học CMC: `https://cmcu.edu.vn/`
- Thông tin tuyển sinh 2026: `https://cmcu.edu.vn/thong-tin-tuyen-sinh/`
- Bảng học phí 2026: `https://cmcu.edu.vn/hoc-phi/`
- Chính sách học bổng, ưu đãi: `https://cmcu.edu.vn/chinh-sach-hoc-bong/`
- Câu hỏi thường gặp: `https://cmcu.edu.vn/cau-hoi-thuong-gap/`

Do một phần dữ liệu ngành/học phí trên website được công bố dưới dạng ảnh, dữ liệu đưa vào seed được trích lại thủ công từ ảnh chính thức và lưu thành bản tóm tắt tại `docs/source_materials/cmcu_admissions_2026.md`.

## 3. Dữ liệu đã đưa vào SQL Server seed

Seeder hiện cấu hình:

- Mã trường: `CMC`.
- Đợt tuyển sinh: `Tuyển sinh đại học chính quy CMCU 2026`.
- 6 nhóm khoa/đơn vị: Công nghệ thông tin & Truyền thông, Vi điện tử & Viễn thông, Kinh doanh & Quản lý, Truyền thông đa phương tiện, Mỹ thuật và Thiết kế, Ngôn ngữ.
- 19 ngành/chương trình theo bảng tuyển sinh 2026.
- Chỉ tiêu Hà Nội/TP.HCM theo bảng tuyển sinh 2026.
- Tổ hợp xét tuyển theo nhóm ngành.
- 4 phương thức tuyển sinh: `CMC401`, `CMC200`, `CMC100`, `CMC303`.
- Học phí/kỳ theo 3 giai đoạn học kỳ 1-3, 4-6, 7-9.
- FAQ cốt lõi: phạm vi trường, phương thức, hồ sơ, học phí, chỉ tiêu, điểm chuẩn, lệ phí, thời gian.

## 4. Dữ liệu RAG đã chuẩn bị

Tài liệu `docs/source_materials/cmcu_admissions_2026.md` là tài liệu nguồn dùng cho RAG. Nội dung gồm:

- phạm vi trường;
- chỉ tiêu;
- ngành/chương trình;
- tổ hợp xét tuyển;
- phương thức tuyển sinh;
- hồ sơ xét tuyển;
- học phí;
- học bổng/ưu đãi/lệ phí;
- thời gian tuyển sinh;
- nguyên tắc không bịa điểm chuẩn.

Tài liệu này có thể upload qua cổng quản trị với loại `Thông báo tuyển sinh`, bật `Xử lý và chia đoạn ngay` để đẩy vào Qdrant.

## 5. Dọn dữ liệu demo

Seeder đã chuyển các dữ liệu tuyển sinh cũ không thuộc bộ mã ngành CMCU sang trạng thái `inactive`, gồm dữ liệu test/demo trước đó. API danh sách khoa và đợt tuyển sinh chỉ trả dữ liệu `active`, nên cổng thông tin mặc định hiển thị dữ liệu CMCU.

## 6. Phạm vi AI trong báo cáo

Hệ thống không train LLM từ đầu. Phần AI/Học máy/Khai phá dữ liệu được thể hiện qua:

- OCR/parse tài liệu PDF, DOCX, ảnh;
- chunking tài liệu tuyển sinh;
- embedding văn bản;
- lưu và truy xuất vector bằng Qdrant;
- RAG answer with citations;
- feedback âm và handoff ticket để khai phá câu hỏi lỗi;
- evaluation runner/golden questions để đo `hit@k`, source hit, latency và đúng-sai.

LLM local qua Ollama chỉ là thành phần sinh câu trả lời cuối pipeline.

## 7. Kịch bản demo đề xuất

1. Mở cổng thông tin và lọc ngành `Trí tuệ Nhân tạo`.
2. Xem chi tiết ngành, tổ hợp xét tuyển và học phí.
3. Hỏi chatbot: `Hồ sơ xét tuyển trực tuyến Đại học CMC gồm những gì?`
4. Kiểm tra câu trả lời có nguồn.
5. Bấm `Chưa đúng` để tạo ticket hỗ trợ.
6. Đăng nhập quản trị và phản hồi ticket.
7. Upload lại `docs/source_materials/cmcu_admissions_2026.md` để chứng minh pipeline RAG.
8. Chạy Evaluation Runner để hiển thị chỉ số đánh giá.

