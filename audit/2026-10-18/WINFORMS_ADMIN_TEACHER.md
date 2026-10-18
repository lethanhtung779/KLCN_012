# Kiểm tra WinForms: quản trị viên và giáo viên

Ngày chạy: 18/10/2026. Ứng dụng WinForms được mở bằng bộ `RoleWorkflowAudit` trên Windows và chụp ảnh từng form/tab. Mọi thao tác thêm, sửa, xóa dùng cơ sở dữ liệu SQL tạm có tên riêng; bộ chạy xóa cơ sở dữ liệu đó khi kết thúc. Không thao tác lên dữ liệu nghiệp vụ đang dùng.

| Vai trò | Chức năng | Kết quả thực chạy |
|---|---|---|
| Giáo viên | Đăng nhập thường và nút đăng nhập nhanh tài khoản mẫu | Đạt; mật khẩu sai và tài khoản học viên bị từ chối ở desktop. |
| Giáo viên | Ngân hàng câu hỏi | Đạt: xem môn được phân công; thêm, đọc, ẩn, sửa, hiện, xóa câu hỏi. Chặn tạo ở môn chưa phân công và sửa câu hỏi của giáo viên khác. |
| Giáo viên | Nội dung đa phương tiện | Đạt bộ smoke: ba loại câu hỏi, ảnh, công thức, preview, import XLSX/TXT/CSV/ZIP, rollback file và DB khi lưu lỗi. |
| Giáo viên | Thống kê kết quả | Form hiển thị bình thường khi có đợt thi; khi chưa có đợt thi, có lời giải thích rõ thay cho vùng trắng. Dữ liệu thống kê bị giới hạn theo môn được phân công. |
| Giáo viên | Quyền quản trị/tạo đề | Chặn mở quản lý tài khoản và gọi service quản trị; chặn giáo viên tạo đề. |
| Admin | Quản lý tài khoản | Đạt: tạo, sửa, đặt lại mật khẩu, cấp gói, khóa, mở khóa, xóa; chặn tự khóa tài khoản đang đăng nhập và dữ liệu không hợp lệ. |
| Admin | Khóa học, lớp, học viên, phân công, lịch học | Đạt thao tác tạo/sửa/xóa ở lớp, khóa, lịch, phân công; xếp học viên và chặn trùng; chặn lịch chồng và phân công trùng. Đã kiểm tra 6 tab hiển thị, gồm các bảng từng bị ẩn. |
| Admin | Đề thi, mã đề, đợt thi | Form mở/hiển thị; bộ smoke tạo được đề thủ công, tự động, theo ma trận, tạo/đóng đợt thi, trộn mã và kiểm tra ghim đáp án cuối. Đề đã có bài làm bị chặn xóa. |
| Admin | Sao lưu | Đạt: tạo file `.bak` của DB thử, xác nhận file có dữ liệu, sau đó xóa file thử. |

Bộ audit chính kết thúc với **41/41 kiểm tra bảo vệ dữ liệu đạt**. Bộ smoke media/đề thi cũng hoàn tất thành công. Build: **0 lỗi**, còn 11 cảnh báo phụ thuộc, gồm cảnh báo bảo mật của MailKit, MimeKit và `System.Security.Cryptography.Pkcs`, cùng xung đột tham chiếu WindowsBase.

Trong lượt này đã sửa bố cục WinForms ở các tab Đào tạo của admin (trước đó `Xếp học viên` và `Tổng hợp` có vùng bảng trắng/ẩn) và sửa mật khẩu của nút đăng nhập nhanh giáo viên cho khớp dữ liệu mẫu. Ảnh sau sửa: [phân công](admin-training-assignment.png), [xếp học viên](admin-training-students.png), [tổng hợp](admin-training-overview.png), [thống kê giáo viên chưa có dữ liệu](teacher-statistics-no-assignment.png).

**Giới hạn:** Chưa chạy `Phục hồi` vì nó thay thế toàn bộ DB đích và code hiện ghi file MDF/LDF vào đường dẫn cố định `D:\KLCN`; cần thử trên bản clone tách biệt. Chưa kiểm email SMTP/VIP thật, tải lớn, nhiều máy và tất cả tương tác thủ công trên từng nút. Kết quả mở/chụp form chỉ chứng minh form render; các thao tác nghiệp vụ được ghi riêng ở bảng trên. Báo cáo audit toàn hệ thống trước đó ở [REPORT.md](REPORT.md).
