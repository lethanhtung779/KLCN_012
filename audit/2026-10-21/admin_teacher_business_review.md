# Rà soát logic nghiệp vụ Admin và Giáo viên — 21/10/2026

Phạm vi: WinForms và các API liên quan đến đợt thi; đọc mã, đối chiếu schema SQL, chạy `RoleWorkflowAudit` trên database tạm. Không ghi/xóa dữ liệu nghiệp vụ hoặc thử phục hồi database thật. 41 kiểm tra tự động đạt; các kiểm tra này chứng minh những trường hợp đã có trong harness, không chứng minh toàn bộ tổ hợp trạng thái.

| Luồng | Đã kiểm | Kết quả chính |
|---|---|---|
| Đăng nhập/quyền | Admin, giáo viên, khóa tài khoản, teacher bị chặn service admin | Cơ bản đạt; `Session.IsAccountActive` còn fail-open khi SQL lỗi |
| Tài khoản/hồ sơ | Thêm, sửa, tìm/lọc, khóa/mở, reset, xóa học viên/giáo viên | Đạt trong database tạm; reset ở tab tài khoản vẫn dùng mật khẩu chung |
| Khóa/lớp/xếp học viên | CRUD, phân công, lịch học, lớp giáo viên được xem | Luồng cơ bản đạt; chưa giữ nhất quán khi đổi môn khóa/chuyển khóa của lớp |
| Ngân hàng câu hỏi | Quyền môn, quyền tác giả, CRUD/ẩn/hiện, import/preview | Các luồng có test đạt; đọc chi tiết theo ID chưa kiểm tra quyền môn |
| Đề/mã đề/đợt thi | Tạo thủ công, tự động, ma trận, trộn 4 mã, lịch thi | Các luồng có test đạt; `TheoLop` thiếu chọn lớp, trộn phá hủy bài làm ngoài giao dịch |
| Thống kê | Giới hạn theo môn, điểm và chất lượng câu hỏi | Lấy dữ liệu được; phạm vi giáo viên theo môn quá rộng nếu phân quyền dựa theo lớp/đề |
| Sao lưu/phục hồi | Sao lưu database tạm | Sao lưu đạt; không thử restore vì đây là thao tác thay thế dữ liệu thật |

## Vấn đề cần xử lý

1. **P1 — Đợt thi `TheoLop` không có bước gán lớp.** `frmDotThiEdit` cho chọn `TheoLop`, nhưng `ExamService.TaoDotThi` chỉ INSERT `DotThi`, không ghi `DotThi_LopHoc`. Không có UI/API gán lớp trong repository. API đăng ký/bắt đầu thi yêu cầu có bản ghi `DotThi_LopHoc`, nên đợt được tạo theo luồng WinForms không ai đủ điều kiện dự thi. Nguồn: `frmDotThiEdit.cs:49-51,83-103`, `ExamService.cs:243-271`, `DotThiController.cs:115-124`, `DeThiController.cs:341-350`.

2. **P1 — Luồng bắt đầu bài thi không dùng đúng đợt do admin chọn.** Khi đề chưa có `DotThi`, `DeThiController.BatDau` tự tạo đợt dự phòng mở 30 ngày, `ToanTruong`, không giới hạn lượt. Nếu cùng đề có nhiều đợt đang mở, API chọn đợt có ID lớn nhất mà không xét đợt người dùng đã đăng ký; Web lại gộp theo đề và chọn phần tử đầu tiên. Người học có thể bị từ chối ở đợt sai hoặc vào đợt admin chưa định mở. Nguồn: `DeThiController.cs:313-329,752-770`, `ExamController.cs:25-26,64-65,183-192`. Cần truyền `DotThiID` xuyên suốt và bỏ fallback tự mở đợt.

3. **P1 — “Làm mới mã đề và xóa bài làm cũ” không nguyên tử.** `TronDeThi` gọi `XoaBaiLamCuaDe` trước khi đọc/kiểm tra pool và trước giao dịch tạo mã mới. Nếu bước sau lỗi, lịch sử bài thi đã bị xóa nhưng mã đề mới không được tạo. Việc xóa bài làm cũng là một batch không có transaction rõ ràng. Nguồn: `ExamService.cs:350-400,744-761`. Đề xuất: chỉ cho thao tác với dữ liệu thử nghiệm, hoặc gộp toàn bộ vào một transaction và sao lưu/xác nhận số bản ghi trước khi thực hiện.

4. **P1 — Phân công giáo viên tự cấp quyền môn, gỡ phân công không thu hồi.** `LuuPhanCong` tự thêm `GiaoVien_MonHoc` cho giáo viên bất kể chuyên môn; `XoaPhanCong` chỉ xóa `PhanCongGiangDay`. Kết quả: một lần chọn nhầm giáo viên có thể cho xem/tạo câu hỏi trong môn đó lâu dài, kể cả khi đã gỡ khỏi lớp. Nguồn: `TrainingService.cs:319-395`; quyền ngân hàng dựa vào `GiaoVien_MonHoc` ở `QuestionService.cs:11-25,169-184`. Cần tách “môn chuyên môn” và “lớp đang dạy”, hoặc yêu cầu xác nhận cấp quyền môn độc lập.

5. **P1 — Đổi môn khóa học/chuyển khóa của lớp không kiểm tra dữ liệu liên quan.** `SuaKhoaHoc` đổi `MonHocID` khi khóa đã có lớp/phân công; `SuaLopHoc` đổi `KhoaHocID` khi lớp đã có học viên, lịch, phân công. Điều này có thể khiến lớp hiển thị môn mới nhưng phân công/đợt thi vẫn ở môn cũ. Nguồn: `TrainingService.cs:57-78,211-219`. Cần chặn thay đổi khi đã có dữ liệu, hoặc thiết kế quy trình chuyển có kiểm tra toàn bộ liên kết.

6. **P1 — Kiểm tra trạng thái tài khoản desktop bỏ qua lỗi database.** `Session.IsAccountActive` nuốt lỗi truy vấn và trả `true`; tài khoản không tìm thấy cũng không bị coi là vô hiệu. Khi SQL gặp lỗi tạm thời, quyền admin tiếp tục được xem là hợp lệ nếu session cũ còn trong bộ nhớ. Nguồn: `AuthService.cs:24-43`. Nên từ chối thao tác khi không xác minh được trạng thái.

7. **P2 — Lịch học kiểm tra trùng lớp/phòng, chưa kiểm tra giáo viên.** Hai lớp khác phòng, cùng giờ và cùng giáo viên vẫn có thể tạo lịch, vì truy vấn chỉ so `LopHocID` hoặc `DiaDiem`. Nguồn: `TrainingService.cs:406-472`. Cần lấy giáo viên được phân công và kiểm tra chồng lịch giữa các lớp; đồng thời xác định môn/buổi học để tránh kết luận sai khi lớp có nhiều giáo viên.

8. **P2 — Đề thi chưa bảo toàn môn/chuyên môn ở tầng service.** `TaoDeCore` không kiểm tra giáo viên phụ trách thuộc `GiaoVien_MonHoc` của môn đề, cũng không xác minh từng câu hỏi thủ công thuộc cùng môn và đang hoạt động. UI thông thường lọc pool đúng, nhưng gọi service với ID bất kỳ vẫn có thể lưu đề sai môn. Nguồn: `ExamService.cs:62-73,126-182`, `LayCauHoiPool:288-301`. Cần kiểm tra lại trong transaction.

9. **P2 — Tạo đề “tự động” chưa theo ma trận.** `TaoDeTuDong` lấy ngẫu nhiên tất cả câu trong một môn/mức độ, không ràng buộc số chủ đề tối thiểu, số câu mỗi chủ đề, tỷ lệ loại câu và độ khó; chỉ tính năng `TaoDeTheoMaTranTHPTQG` có các kiểm tra đó. Người dùng chọn nhầm chế độ có thể tưởng đề đã cân bằng. Nguồn: `ExamService.cs:98-124,796-923`. Cần tách tên/chú thích rõ hoặc dùng cấu hình ma trận chung.

10. **P2 — Thống kê giáo viên theo môn có thể rộng hơn phạm vi giảng dạy.** Chỉ cần có `GiaoVien_MonHoc` thì giáo viên xem toàn bộ đợt thi, danh sách học viên và điểm của môn, kể cả đề do giáo viên khác phụ trách hoặc lớp khác. Nếu chính sách dự kiến là thống kê theo lớp/đề của mình, đây là lộ phạm vi dữ liệu. Nguồn: `StatisticsService.cs:9-45`. Cần chốt chính sách đọc chung theo tổ bộ môn hay theo lớp/đề.

11. **P2 — Schema tạo mới và database đang chạy khác nhau.** `create_database_trung_tam.sql:226` chỉ cho `HinhThucTao` là `ThuCong`/`TuDong`, trong khi chức năng ma trận ghi `MaTranTHPTQG`. Database `.\\SQLEXPRESS` hiện có constraint cho phép giá trị mới (đã kiểm bằng truy vấn chỉ đọc), nhưng cài mới từ script có thể lỗi. Cần đưa migration vào repository và cập nhật script.

12. **P2 — Phục hồi chưa đủ an toàn để nghiệm thu.** `BackupService.PhucHoi` chuyển DB đang dùng sang SINGLE_USER rồi vẫn dùng `Database.Query/Execute` qua connection tới chính DB đó, `WITH REPLACE` và ép đường file `D:\\KLCN`. Không có kiểm tra backup bằng `RESTORE VERIFYONLY`, không có phục hồi thử và xác minh dữ liệu sau restore. Nguồn: `BackupService.cs:44-82`. Chỉ nên thử trên bản sao cho tới khi thiết kế lại qua connection `master`.

13. **P3 — Một số thông báo/giao diện gây hiểu lầm.** Tab tài khoản reset về mật khẩu cố định `Abc@123456` trong khi hai tab hồ sơ dùng mật khẩu ngẫu nhiên; nút xóa đề hỏi sẽ xóa cả bài làm nhưng service từ chối nếu đã có bài; thống kê “Số lần trả lời” thực tế đếm số bản ghi chi tiết bài nộp, gồm câu bỏ trống. Nguồn: `frmAccounts.cs:188-196`, `frmExams.cs:298-303`, `ExamService.cs:195-203`, `StatisticsService.cs:74-98`.

## Giới hạn kiểm chứng

Không chạy restore thật, không mô phỏng hàng trăm học viên đồng thời, không thao tác thủ công mọi hộp thoại ở mọi DPI. Các điểm P1/P2 nêu từ đối chiếu mã và schema, ngoại trừ các hành vi đã có trong `RoleWorkflowAudit`; cần thêm hồi quy riêng cho từng tình huống trước khi sửa/triển khai. Những điểm phụ thuộc chính sách (phạm vi thống kê giáo viên, chia sẻ môn chuyên môn) cần chốt với chủ dự án trước khi đổi quyền.
