# Đợt nâng cấp xác thực và luồng thi

## Thay đổi hành vi

- Đăng nhập API trả thêm `accessToken` và `tokenExpiresAt`. Các API cá nhân yêu cầu `Authorization: Bearer <accessToken>`. Web giữ token trong session phía máy chủ, không đưa token vào trang HTML hoặc localStorage.
- Token có hạn 8 giờ. Web yêu cầu đăng nhập lại sau khi token hết hạn. Khóa tài khoản, đổi vai trò hoặc đổi mật khẩu làm token cũ mất hiệu lực khi gọi API tiếp theo.
- Học viên chỉ đọc/sửa hồ sơ, lịch học và bài làm của chính mình. ID gửi lên phải khớp danh tính đã xác thực.
- Ngân hàng câu hỏi/đáp án dành cho quản trị viên hoặc giáo viên được phân công môn. Các API tạo/xóa môn, đề và đợt thi dành cho quản trị viên; WinForms vẫn sử dụng luồng quản trị hiện có qua database.
- Lấy câu hỏi thi yêu cầu một bài đang làm và đúng mã đề được phân phối.
- Nộp bài phải có bài làm được tạo bởi thao tác bắt đầu thi. Không còn đường nộp trực tiếp hoặc tự chọn học viên khách.
- Hạn làm bài là mốc sớm hơn giữa thời điểm bắt đầu cộng thời lượng và thời điểm đóng đợt thi. Máy chủ xác định trạng thái hết giờ, không tin `HetGio` hoặc thời điểm bắt đầu do client gửi.
- Sau hạn, không nhận đáp án mới. Khi nộp muộn, máy chủ chấm snapshot đã lưu trước hạn. Khi quay lại một bài đã hết giờ, Web yêu cầu chốt snapshot này rồi mở kết quả.
- Lưu và nộp dùng giao dịch và khóa SQL Server theo bài làm. Nộp lại trả mã bài đã chốt, không chấm lại hoặc ghi đè.
- Trước công bố, response chi tiết/nộp bài không có breakdown; điểm và số câu đúng/sai bị che. Lịch sử và thống kê cũng không tiết lộ điểm chưa công bố. Client phải dùng `ScorePublished`, không diễn giải giá trị điểm bị che thành điểm thật bằng 0.
- Autosave lưu sau khoảng 800 ms ngừng thao tác và thử lại định kỳ 15 giây hoặc khi có mạng trở lại. Chỉ đánh dấu đã lưu sau xác nhận; không gửi request chồng nhau. Lưu tạm Web yêu cầu antiforgery token.

## Vận hành và giới hạn còn lại

- Cần khởi động lại API và Web sau cập nhật. Phiên Web cũ không có token sẽ yêu cầu đăng nhập lại.
- Cần giữ an toàn key ring ASP.NET Core Data Protection; khi triển khai nhiều API instance phải chia sẻ key ring phù hợp để token có thể được xác thực ở mọi instance.
- Bản này chưa có worker nền tự chốt bài khi học viên đóng trình duyệt và không quay lại. Hạn thi vẫn được máy chủ cưỡng chế, nhưng việc chốt điểm xảy ra khi có yêu cầu nộp/quay lại.
- Chưa hỗ trợ phục hồi thay đổi chưa gửi sau khi đóng tab, tải lại trang hoặc mất điện; chỉ đáp án máy chủ xác nhận được bảo đảm khôi phục. Cần giữ trang mở khi có lỗi đồng bộ.
- Chưa giải quyết ghi đè đáp án giữa nhiều tab đang mở cùng một bài; giao dịch bảo đảm ghi nguyên vẹn, không thay thế kiểm soát phiên hoặc phiên bản dữ liệu giữa các tab.
- Băm mật khẩu SHA-256 cũ, cảnh báo dependency, giới hạn tần suất đăng nhập và rà soát HTML câu hỏi cần được xử lý tiếp. Các URL ảnh hiện vẫn công khai để tương thích cách Web tải ảnh trực tiếp.
- Dashboard học tập, sổ tay câu sai, quy trình duyệt đề và nâng cấp giao diện thuộc các đợt tiếp theo.

## Kiểm chứng

Xem [hướng dẫn kiểm thử](Tests/ExamSecurityChecks/README.md). Bộ kiểm tra chạy trên database tạm, không dùng dữ liệu thật để thử ghi.
