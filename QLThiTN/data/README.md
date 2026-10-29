# Dữ liệu dùng chung

Thư mục `tai_nguyen` chứa ảnh câu hỏi dùng chung cho API, Web và WinForms.

- Trong cơ sở dữ liệu, `HinhAnh.DuongDan` chỉ lưu đường dẫn tương đối, ví dụ `ab/abc123.png`.
- API và WinForms đọc thư mục này qua cấu hình `HinhAnhRoot` là `..\data\tai_nguyen`.
- Khi chuyển dự án sang máy khác, cần sao chép hoặc clone **toàn bộ** thư mục dự án để giữ nguyên thư mục `data`.
- Không lưu đường dẫn tuyệt đối theo ổ đĩa vào cơ sở dữ liệu hoặc `appsettings.json`.
