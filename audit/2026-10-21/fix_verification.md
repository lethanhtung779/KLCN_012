# Kết quả sửa logic Admin/Giáo viên — 21/10/2026

Đối chiếu với [báo cáo rà soát](admin_teacher_business_review.md):

| Mục | Kết quả |
|---|---|
| 1. Đợt thi theo lớp | Form WinForms và API buộc chọn lớp cùng môn; lưu `DotThi_LopHoc` cùng giao dịch. |
| 2. Bắt đầu nhầm đợt | Web truyền `DotThiID` xuyên suốt; API yêu cầu chọn đợt khi đề có nhiều đợt và không tự mở đợt dự phòng. Lịch/kho đề hiển thị từng đợt. |
| 3. Trộn mã đề xóa bài làm ngoài giao dịch | Xóa bài làm và tạo lại mã đề cùng một giao dịch; lỗi sẽ hoàn tác toàn bộ. |
| 4. Phân công tự cấp môn | Chỉ giáo viên có môn chuyên môn đã khai báo và tài khoản hoạt động mới được phân vào lớp đúng môn. Gỡ lớp không đổi chuyên môn độc lập. |
| 5. Đổi môn/chuyển khóa làm lệch dữ liệu | Chặn đổi môn khóa đã có lớp và chặn chuyển khóa lớp đã có học viên, giáo viên, lịch hoặc đợt thi. |
| 6. Session bỏ qua lỗi DB | Trạng thái tài khoản không xác minh được sẽ bị từ chối; các luồng giáo viên đọc ngân hàng câu hỏi cũng kiểm tra trạng thái. |
| 7. Trùng lịch giáo viên | Thêm/sửa lịch và phân công mới đều kiểm tra lớp khác cùng giáo viên có giờ chồng nhau. |
| 8. Đề sai môn/câu hỏi | Tầng service xác minh chuyên môn giáo viên phụ trách, môn và trạng thái hoạt động của toàn bộ câu hỏi trước khi lưu. |
| 9. Tạo ngẫu nhiên dễ hiểu nhầm ma trận | Đổi nhãn thành “Tạo đề ngẫu nhiên” và giải thích chế độ này; ma trận THPT là chức năng riêng. |
| 10. Thống kê giáo viên quá rộng | Chỉ xem đợt thuộc đề mình phụ trách hoặc lớp mình được phân công đúng môn. |
| 11. Schema thiếu `MaTranTHPTQG` | Script tạo mới và bước cập nhật constraint đều cho phép giá trị này. |
| 12. Phục hồi database | Sao lưu có checksum, xác minh backup, kiểm tra database đích, phục hồi qua connection `master` với đường file hiện có; đã thử trên database kiểm thử dùng một lần. |
| 13. Thông báo chưa đúng | Reset mật khẩu ngẫu nhiên, sửa xác nhận xóa đề và nhãn thống kê. |

Kiểm chứng: build API, Web và WinForms thành công; `RoleWorkflowAudit` đạt **42 kiểm tra**, gồm phân quyền giáo viên, phân công đúng chuyên môn, lịch chồng giờ, đợt thi theo lớp, 4 mã đề, trình duyệt và sao lưu/phục hồi database kiểm thử. Các thao tác thử nghiệm chỉ dùng database tạm có tên riêng; không phục hồi database đang sử dụng.

Giới hạn: chưa thử tải đồng thời quy mô lớn hoặc kiểm tra thủ công mọi hộp thoại trên mọi DPI. Các cảnh báo NuGet về phiên bản MailKit, MimeKit và `System.Security.Cryptography.Pkcs` đang tồn tại trong dependency của dự án; cần cập nhật phiên bản sau khi kiểm tra tương thích.
