# Kiểm tra xác thực và độ tin cậy của bài thi

Chạy từ thư mục gốc repository với .NET 10 SDK, Node.js và SQL Server đã cấu hình cho API:

```powershell
dotnet run --project Tests/ExamSecurityChecks/ExamSecurityChecks.csproj -- .
node --test Tests/ExamSecurityChecks/autosave.test.cjs
```

Bộ kiểm tra tạo database riêng `QLThiTN_SecurityChecks_<GUID>` trên cùng SQL Server trong cấu hình API. Tài khoản SQL cần quyền tạo/xóa database. Không restore hoặc sửa database ứng dụng. API và Web được chạy ở cổng loopback tạm; tiến trình và database thử được dọn trong `finally`.

Các kiểm tra HTTP thực bao gồm đăng nhập, token, chặn người chưa đăng nhập, chặn đọc/sửa dữ liệu người khác, giới hạn quyền quản trị/ngân hàng đáp án, bắt đầu đồng thời, mã đề được phân phối, khôi phục đáp án, lưu sau hạn, nộp đồng thời, chấm snapshot đã lưu khi quá hạn, ẩn điểm/đáp án/thống kê trước công bố, công bố kết quả và thu hồi token khi khóa tài khoản. Luồng MVC kiểm tra session, truyền token sang API, tải trang thi, chống CSRF cho autosave và chốt bài đã hết giờ khi quay lại.

Kiểm tra JavaScript xác nhận autosave giữ thay đổi khi lỗi mạng hoặc API từ chối, tự lưu lại được ở lần thử tiếp theo, không gửi chồng request và không bỏ mất thay đổi phát sinh khi request đang chạy.

Đây là kiểm tra HTTP và logic JavaScript; chưa thay thế kiểm tra giao diện bằng trình duyệt trên thiết bị thật.
