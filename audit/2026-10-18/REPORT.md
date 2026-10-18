# Báo cáo kiểm tra quy trình và trải nghiệm theo vai trò

Ngày kiểm tra: 18/10/2026. Phạm vi: mã nguồn hiện tại trong workspace, gồm các thay đổi bảo vệ API của đợt trước.

## Kết luận

**Chưa nên coi hệ thống là sẵn sàng cho kỳ thi thật có yêu cầu bảo mật và tính toàn vẹn cao.** Luồng thi cơ bản, truyền token, lưu tạm, phục hồi, bảo vệ bài làm cá nhân và xử lý media đã có các kiểm tra đạt. Tuy nhiên, còn lỗi ở ranh giới quyền WinForms, trạng thái/phạm vi đợt thi, HTML câu hỏi, đăng ký chỗ và tính nguyên vẹn của thao tác quản trị.

Giao diện Web đã có cấu trúc tương đối rõ; các màn hình thử nghiệm không tràn ngang ở kích thước đã kiểm tra. WinForms còn lỗi bố cục nhìn thấy được. Những kết quả tích cực này không phủ định các lỗi nghiệp vụ bên dưới.

Đợt này bổ sung **bộ kiểm tra và bằng chứng**. Các lỗi phát hiện vẫn cần được sửa và kiểm thử hồi quy.

## Phương pháp và bằng chứng

- Build solution: thành công, 0 lỗi; còn 6 cảnh báo dependency trong lần build cuối.
- 35 kiểm tra hồi quy API/Web: đạt, bao gồm xác thực, quyền sở hữu, CSRF, thời hạn, nộp đồng thời, công bố điểm và khóa tài khoản.
- 76 kiểm tra media/import/WinForms hiện có: đạt khi chạy trong fixture SQL riêng. Chạy trực tiếp bộ cũ ban đầu lỗi vì nó chưa nạp cấu hình DB trước khi mở form; wrapper audit đã cấp database tạm đúng trước khi chạy.
- 3 kiểm tra logic autosave bằng Node: đạt.
- Các probe bổ sung **chủ động tái hiện lỗi**, không phải tất cả đều là test đạt. Xem kết quả từng probe trong `observations.md`.
- Microsoft Edge headless thật: đăng nhập, các trang chính, phòng thi, ngắt/kết nối mạng, desktop 1440×1000 và mobile 390×844. Có 13 ảnh Web và kết quả DOM/JavaScript trong `browser-results.json`.
- WinForms thật trên STA thread: mở và chụp 8 màn hình của giáo viên/admin; thêm 4 ảnh editor/import/preview từ bộ smoke.
- Database được tạo tên riêng `QLThiTN_RoleAudit_<GUID>`; bộ media tạo thêm database riêng. Dọn trong `finally`. Không dùng database nghiệp vụ để thử ghi/xóa/restore.
- Lần chạy chốt dùng content root API tạm và không có cấu hình SMTP thật. Đăng ký được kiểm tra ở cấp ứng dụng, không chứng minh email đã đến hộp thư.

Bằng chứng: [observations.md](D:/KLCN/QLThiTN/audit/2026-10-18/observations.md), [browser-results.json](D:/KLCN/QLThiTN/audit/2026-10-18/browser-results.json), [workflow.log](D:/KLCN/QLThiTN/audit/2026-10-18/workflow.log), [media.log](D:/KLCN/QLThiTN/audit/2026-10-18/media.log), [autosave.log](D:/KLCN/QLThiTN/audit/2026-10-18/autosave.log), [build.log](D:/KLCN/QLThiTN/audit/2026-10-18/build.log).

## Ma trận vai trò và luồng đã kiểm tra

| Vai trò | Luồng | Kết quả |
|---|---|---|
| Khách | Trang chủ, kho đề, lịch thi, đăng nhập/đăng ký | Trang tải được; API cá nhân chặn khách; đăng ký còn lỗi cấu hình email và validation |
| Học viên | Đăng nhập → hồ sơ/lịch sử/lịch học | Truyền token đúng; chặn truy cập dữ liệu người khác |
| Học viên | Free/VIP → đăng ký → bắt đầu | VIP chặn đúng; phạm vi lớp, trạng thái đóng và số chỗ còn sai |
| Học viên | Làm bài → lưu → phục hồi → nộp | Autosave mạng lỗi/phục hồi đạt; hạn máy chủ và nộp đồng thời đạt; HTML câu hỏi có XSS |
| Học viên | Chấm → chờ công bố → xem kết quả | Các trường điểm/đáp án trước công bố được che; thống kê giữa các trang chưa thống nhất |
| Giáo viên | Đăng nhập WinForms → ngân hàng → CRUD/import/preview | Nhiều kiểm tra CRUD/media đạt; quyền môn/tác giả chưa được giới hạn ở desktop |
| Giáo viên | Xem thống kê | Đọc được thống kê các đợt ngoài phạm vi; cần thu hẹp |
| Giáo viên | Tạo/quản lý đề | Service tạo đề chặn giáo viên đúng; sidebar không có quản lý đề |
| Admin | Tài khoản → khóa/reset/cấp gói | CRUD cơ bản có; thiếu transaction, validation và kiểm tra quyền tại service |
| Admin | Khóa/lớp → xếp học viên → phân công → lịch | Luồng cơ bản đạt; trùng lịch và tác dụng phụ khi phân công thất bại |
| Admin | Tạo đề → đợt thi → đóng → xóa | Tạo ba loại đề đạt trong smoke; đóng không được Web/API thực thi đầy đủ; xóa đề lỗi SQL |
| Admin | Backup/restore | Chỉ rà mã; chưa thực hiện restore vì code cố định database thật |

## Các lỗi cần ưu tiên cao

### P1-01 — Nội dung câu hỏi có thể chạy JavaScript trên Web

**Đã tái hiện bằng trình duyệt thật.** Trong database tạm, đặt nội dung có ảnh với `onerror` chỉ gán một biến thử nghiệm trong trang. Mở phòng thi: `window.__auditXss === true`.

Nguồn: `QLThiTN.Web/Views/Exam/Taking.cshtml:121`, các phương án ở dòng 145/174; `Result.cshtml` cũng dùng `Html.Raw`. `ExamMapping.ToApiAbsoluteHtml` đổi URL nhưng không làm sạch HTML.

Ảnh hưởng: người có quyền nhập/sửa câu hỏi có thể đưa mã chạy trong phiên học viên. Việc token nằm trong session máy chủ không ngăn mã chạy cùng origin thao tác thay người dùng.

Hướng sửa: quy định nội dung là text hay HTML có giới hạn; encode text, hoặc sanitize HTML theo allowlist ở ranh giới nhập/lưu và hiển thị; thêm regression test cho script, event handler, URL nguy hiểm, nội dung đáp án và lời giải. Bản preview WinForms đã escape text và kiểm tra này đạt, nhưng Web chưa tương đương.

### P1-02 — Giáo viên desktop vượt phạm vi môn và tác giả

**Đã tái hiện T04/T05.** Giáo viên không được phân công môn vẫn thêm câu hỏi vào môn đó; một tài khoản giáo viên thứ hai hợp lệ vẫn sửa được câu hỏi chưa dùng của giáo viên thứ nhất.

Nguồn: `QLThiTN.WinForms/Services/QuestionService.cs:15`, `:27`, `:51`, `:121`. `frmQuestions` hiển thị danh sách toàn hệ thống và gọi trực tiếp các service này.

Hướng sửa: áp dụng cùng chính sách môn/tác giả ở truy vấn danh sách và mọi thao tác thêm/sửa/xóa/ẩn/import. Nếu trung tâm chủ ý dùng ngân hàng chung, phải xác định rõ quyền đọc chung và quyền sửa, đồng thời làm nhất quán với API đang kiểm tra môn được phân công.

### P1-03 — Service quản trị desktop không kiểm tra quyền

**Đã tái hiện T02/T03.** Với `Session` của giáo viên, gọi trực tiếp `AccountService.Them(..., "QuanTriVien", ...)` tạo được một admin; form tài khoản cũng khởi tạo được. Sidebar bình thường không hiện mục này.

Đây là lỗ hổng ở ranh giới ứng dụng desktop/service, **không phải bằng chứng học viên gọi HTTP để tạo admin**. Phần mềm desktop đang kết nối database trực tiếp; mô hình tin cậy và quyền SQL cần được xem xét cùng việc thêm guard.

Nguồn: `AccountService.cs:61`, các phương thức sửa/reset/cấp gói/khóa; `TrainingService` và `BackupService` cũng thiếu guard tương tự. `ExamService` đã có `YeuCauQuanTriVien`, cho thấy việc bảo vệ hiện chưa đồng nhất.

Hướng sửa: guard tập trung cho mọi service/form đặc quyền; với triển khai nhiều máy nên chuyển ghi dữ liệu qua API có phân quyền hoặc tài khoản SQL có quyền tối thiểu. Không coi ẩn menu là cơ chế bảo mật.

### P1-04 — Giáo viên xem thống kê toàn hệ thống

**Đã tái hiện T01.** Combo đợt thi đọc mọi đợt; `LayKetQuaDotThi` lấy danh sách học viên/điểm theo ID đợt mà không lọc giáo viên phụ trách.

Nguồn: `QLThiTN.WinForms/Services/StatisticsService.cs:9` và `:17`.

Hướng sửa: lọc theo môn/lớp/đợt được phân công, kiểm tra lại khi đọc chi tiết và xuất dữ liệu; phân biệt quyền của admin và giáo viên.

### P1-05 — Admin đóng đợt nhưng học viên vẫn bắt đầu được

**Đã tái hiện U01:** `TrangThai = DaDong`, thời gian vẫn đang mở → API bắt đầu trả **200**.

Nguồn: `DeThiController.cs:286`, `:301`, `:307`; chỉ dựa vào thời gian và thiếu kiểm tra trạng thái đóng. Danh sách cũng tự suy ra trạng thái từ thời gian tại `:879`.

Hướng sửa: thống nhất máy trạng thái đợt thi; định nghĩa rõ đóng đợt ảnh hưởng đăng ký mới, bài mới, bài đang làm thế nào. Dùng một chính sách ở API, Web và desktop.

### P1-06 — Đề theo lớp vẫn cho người ngoài lớp vào thi

**Đã tái hiện U02:** phạm vi `TheoLop`, tài khoản không thuộc lớp nào → bắt đầu trả **200** khi không giới hạn số lượng.

Nguồn: `DeThiController.cs:326` gắn yêu cầu đăng ký với `GioiHanSoLuong`, không kiểm tra `PhamVi`, `DotThi_LopHoc`, `HocVien_Lop`. API đăng ký cũng chưa xác minh tư cách theo lớp.

Hướng sửa: tách ba điều kiện độc lập: được phân công, đã đăng ký nếu bắt buộc, còn chỗ. Form “Tạo đợt thi” có lựa chọn “Theo lớp” nhưng chưa có bước chọn lớp tương ứng, cần hoàn thiện cả luồng admin.

### P1-07 — Đăng ký lại vượt giới hạn số chỗ

**Đã tái hiện U03:** A đăng ký rồi hủy; B chiếm chỗ duy nhất; A đăng ký lại thành công → **2/1 chỗ**.

Nguồn: `DotThiController.cs:125` chỉ kiểm tra giới hạn khi `existing is null`. Ngoài lỗi tái đăng ký, đọc số lượng rồi ghi mà không có khóa/giao dịch phù hợp còn có nguy cơ tranh chấp chỗ; phần đồng thời này chưa đo tải.

Hướng sửa: kiểm tra sức chứa cả lúc kích hoạt lại, khóa theo đợt thi và trả thông báo hết chỗ rõ ràng.

### P1-08 — Tạo tài khoản/đề thi để lại dữ liệu dở dang

**Đã tái hiện A07:** họ tên vượt độ dài cột làm tạo hồ sơ thất bại nhưng vẫn còn `TaiKhoan` không có `HocVien`.

**Đã tái hiện A08:** tạo đề có hai ID câu hỏi giống nhau làm insert sau thất bại; bản ghi đề đã tồn tại và `SoLuongCauHoi` khác số câu thật.

Nguồn: `AccountService.cs:61` và `ExamService.cs:87` thực hiện nhiều lệnh ngoài transaction. `RegistrationEmailService.cs:129/:139` cũng tạo tài khoản và hồ sơ qua hai lần lưu riêng; nhánh này được nhận diện từ mã, chưa ép lỗi giữa hai lần lưu.

Hướng sửa: validate trước khi ghi, transaction bao trọn thao tác, rollback sạch, thông báo lỗi có thể xử lý được. Không chỉ thêm try/catch ở UI.

## Lỗi nghiệp vụ và trải nghiệm mức P2

| Mã | Bằng chứng và tác động | Hướng sửa |
|---|---|---|
| P2-01 / U04 | Một đề có đợt cũ đã hết hạn và đợt mới đang mở nhưng bắt đầu trả 400 vì lấy `FirstOrDefault()`; Web cũng gộp theo đề rồi chọn đầu tiên | Luồng đăng ký/làm bài phải mang `DotThiID`; hiển thị và chọn đợt cụ thể |
| P2-02 / A01 | `ExamService.XoaDe` báo `Invalid column name 'DeThiID'`; bảng `BaiLam` liên kết qua `DotThiID` | Sửa truy vấn theo schema thật, bảo vệ đề có lịch sử, transaction; bổ sung test xóa có/không có liên kết |
| P2-03 / U05 | Đáp án ID không tồn tại trong autosave trả 500 | Validate câu hỏi/đáp án thuộc đề và loại câu trước ghi; trả 400 có thông báo |
| P2-04 / U06 | Production không có SMTP vẫn báo đăng ký thành công; không trả link dev nên người dùng không thể hoàn tất | Không báo đã gửi khi không gửi; kiểm tra cấu hình khởi động và cơ chế gửi lại |
| P2-05 / U07 | API đăng ký chấp nhận mật khẩu một ký tự; xác minh xong đăng nhập được | Dùng validation/policy chung giữa Web, API và desktop; Web hiện có MinLength(6), API đăng ký không có |
| P2-06 / A03 | Admin tạo tài khoản bằng email `not-an-email` thành công | Kiểm tra email và độ dài ở service, không chỉ trang Web |
| P2-07 / A05 | Hai buổi cùng lớp, cùng phòng, chồng thời gian đều được tạo | Kiểm tra xung đột lớp, phòng và giáo viên; cho phép ngoại lệ chỉ nếu có nghiệp vụ rõ ràng |
| P2-08 / A06 | Phân công trùng bị từ chối nhưng giáo viên được chọn vẫn được thêm vào `GiaoVien_MonHoc` | Kiểm tra điều kiện trước, transaction cả ánh xạ môn và phân công lớp |
| P2-09 | Điểm TB kho đề lọc `TongDiem > 0`, bỏ các bài 0 điểm; hồ sơ tính cả điểm 0 | Dùng `ScorePublished` và trạng thái hoàn thành, không dùng điểm >0 làm điều kiện. Nguồn: `ExamController.cs:144` |
| P2-10 | `History.cshtml:202` chèn tên đề bằng `Html.Raw` vào chuỗi JavaScript | Serialize JSON đúng cách; kiểm tra tên chứa dấu nháy, xuống dòng, đóng thẻ script. Nhận diện qua mã, chưa khai thác nhánh này |
| P2-11 | Băm mật khẩu vẫn là SHA-256 trực tiếp; reset dùng chung mật khẩu cố định và không bắt đổi | Chuyển dần sang cơ chế băm mật khẩu có salt; reset một lần/đặt lại bởi người dùng |
| P2-12 | Backup/restore cố định `[ThiTracNghiem]` và ổ `D:`; restore dùng `WITH REPLACE` và kết nối hiện tại | Dùng database từ cấu hình, kết nối master khi cần, kiểm tra backup trước, restore thử trên DB riêng, xác minh dữ liệu sau phục hồi |
| P2-13 | Khóa tài khoản API thu hồi token đúng; desktop giữ `Session.Current` và các service không kiểm tra trạng thái tài khoản lại | Định nghĩa và thực thi thu hồi phiên desktop. A04 rồi A05 cho thấy service vẫn ghi sau khi tài khoản phiên đã bị khóa |
| P2-14 | Khi đóng trình duyệt, chưa có worker tự chốt bài hết hạn; mất điện/đóng tab có thể mất thay đổi chưa đồng bộ | Worker chốt bài, chính sách phục hồi/offline, hiển thị giới hạn rõ ràng; chưa mô phỏng power loss |
| P2-15 | Nhiều tab cùng bài chưa có phiên bản đáp án; giao dịch chỉ tuần tự hóa ghi, không xác định tab mới hơn | Một phiên thi hoặc optimistic concurrency theo revision; thêm test hai tab |

## Giao diện và các chi tiết nhỏ

1. **Admin quản lý đề:** tiêu đề “Danh sách đề thi” đè lên header bảng; tiêu đề đợt thi và nút đóng cũng đè header cột. Quan sát trực tiếp [admin-exams.png](D:/KLCN/QLThiTN/audit/2026-10-18/admin-exams.png). Tách vùng tiêu đề/nút khỏi vùng `DataGridView`, tránh `Dock=Fill` phủ cùng không gian.
2. **Form tạo đợt thi:** combo phạm vi cắt nội dung dài; lựa chọn TheoLop chưa có bước chọn lớp. Câu “chờ quản trị công bố” chưa khớp cơ chế API tự công bố khi đóng cổng. Xem [admin-session-edit.png](D:/KLCN/QLThiTN/audit/2026-10-18/admin-session-edit.png).
3. **Giáo viên đăng nhập Web:** vẫn vào được giao diện học viên, thấy lịch thi/lịch học nhưng không có `HocVienID`. Cần điều hướng/thông báo đúng phân hệ hoặc cung cấp dashboard giáo viên rõ ràng; hiện là trải nghiệm gây nhầm.
4. **Ghi nhớ đăng nhập:** checkbox có trong ViewModel/view nhưng `AccountController.Login` không dùng `RememberMe`; chọn/không chọn không thay đổi chính sách session.
5. **Accessibility:** ô tìm kho đề, bộ sắp xếp kho đề và tìm lịch sử thiếu label liên kết/accessible name theo kiểm tra DOM. Các tùy chọn trắc nghiệm dùng input `d-none` cũng cần kiểm thử bàn phím và screen reader kỹ hơn.
6. **Mobile phòng thi:** không tràn ngang ở 390 px; nút “Nộp bài thi” và “Làm lại từ đầu” xuống dòng; bảng điều hướng nằm sau toàn bộ câu hỏi. Cần thử đề 40–50 câu để đánh giá thao tác quay lại câu, không suy từ fixture một câu.
7. **Ngày tháng:** ảnh hồ sơ có trình chọn ngày hiển thị `mm/dd/yyyy` theo locale trình duyệt thử nghiệm. Cần kiểm thử người dùng locale vi-VN và hướng dẫn định dạng nhất quán.
8. **Chế độ sáng/tối:** có toggle nhưng đợt này chưa kiểm đủ tương phản mọi component trong dark mode.
9. **Lỗi mạng lúc nộp:** autosave được kiểm tra tốt khi offline/online; luồng submit HTTP timeout, API chết giữa lúc trả response, hoặc session hết hạn khi đang làm vẫn cần thêm kiểm thử. Không lấy kết quả autosave để khẳng định nộp bài đã xử lý mọi lỗi mạng.
10. **Dữ liệu danh sách:** nhiều truy vấn desktop và danh sách đề/lịch sử tải toàn bộ. Chưa có số đo với dữ liệu lớn; cần phân trang và test khi ngân hàng có hàng chục nghìn câu.
11. **Phụ thuộc CDN:** jQuery/Bootstrap/KaTeX/Chart.js tải từ bên ngoài. Lần thử có Internet tải được; chưa chứng minh hệ thống hoạt động trong mạng thi chặn CDN hoặc mất mạng trước lần mở trang.
12. **Lỗi thông báo:** một số API dùng tiếng Việt không dấu hoặc thông báo chung, nhiều thao tác desktop không bắt lỗi SQL ở đường gọi. Cần thống nhất lỗi validation/quyền/kết nối/xung đột và giữ dữ liệu người dùng đã nhập.

Ảnh tham khảo: [Web mobile phòng thi](D:/KLCN/QLThiTN/audit/2026-10-18/web-taking-mobile.png), [hồ sơ desktop](D:/KLCN/QLThiTN/audit/2026-10-18/web-profile-desktop.png), [ngân hàng câu hỏi](D:/KLCN/QLThiTN/audit/2026-10-18/teacher-questions.png), [xem trước media](D:/KLCN/QLThiTN/audit/2026-10-18/media-preview.png).

## Phần đã hoạt động tốt trong các trường hợp kiểm tra

- Khách không đọc được lịch sử cá nhân; học viên không đọc/sửa hồ sơ, lịch học và bài làm người khác.
- API học viên không truy cập ngân hàng đáp án, không gọi được API quản trị.
- API giáo viên: môn chưa phân công trả 403; sau phân công trả 200.
- Bắt đầu đồng thời trả cùng bài; nộp đồng thời trả cùng mã bài và không chấm lại.
- Mã đề không được phân phối bị chặn; câu hỏi đúng mã được tải.
- Autosave trước hạn, phục hồi đáp án, chặn lưu sau hạn, chấm snapshot đã lưu khi nộp muộn.
- Điểm chưa công bố được che ở kết quả, lịch sử và thống kê; sau công bố hiển thị đúng.
- Chấm ba loại câu trong fixture: trắc nghiệm đúng, đúng/sai đúng 3/4 ý, trả lời ngắn chấp nhận dấu phẩy thập phân → **9.00**, đúng theo công thức chia điểm từng ý hiện có. Đây không phải xác nhận quy chế chấm của mọi kỳ thi quốc gia; cần chốt chính sách nghiệp vụ riêng.
- Không cho thi quá số lượt; tài khoản miễn phí bị chặn đề trả phí.
- Token email đã dùng bị từ chối khi dùng lại.
- Xếp học viên vào lớp thành công và từ chối xếp trùng.
- WinForms chặn giáo viên tạo đề; admin tạo đề thủ công, tự động và theo ma trận đạt trong smoke.
- Ảnh/công thức trong preview WebView2, nhập XLSX/TXT/CSV/ZIP, ảnh thiếu/đường dẫn vượt thư mục, giữ ID ảnh, rollback file và DB, bảo vệ câu hỏi đã dùng: các kiểm tra smoke đạt.

## Giới hạn kiểm chứng và phần cần chạy thêm

Không thể kết luận “đã kiểm hết mọi ngóc ngách” từ một lượt audit. Các phần sau chưa được chứng minh đầy đủ:

- SMTP thật tới các nhà cung cấp hộp thư, email VIP thật, spam/bounce, đăng ký đồng thời cùng email/tên.
- Backup/restore trên máy clone có schema và dữ liệu thật; mất kết nối khi restore.
- Nhiều máy, hàng trăm học viên đồng thời, nhiều API instance, độ trễ cao và mất gói.
- Safari/iOS/Android thật, nhiều DPI Windows, screen reader và toàn bộ dark mode.
- Quy chế chấm đúng/sai, chính sách đóng đợt đang diễn ra, phạm vi chia sẻ ngân hàng giáo viên: cần xác định yêu cầu nghiệp vụ chuẩn trước khi kết luận đúng/sai tuyệt đối.
- Database fixture được tạo từ EF với FK Restrict và một số default tương ứng script triển khai; không thay thế kiểm tra migration/backup schema thực tế. Lỗi `BaiLam.DeThiID` đã được đối chiếu thêm với script database của repository.
- Chưa kiểm mọi tổ hợp sửa/xóa của tất cả form; chưa có kiểm thử độ bền chạy nhiều giờ.

## Thứ tự khắc phục đề xuất

1. HTML câu hỏi/đáp án/lời giải và dữ liệu nhúng vào JavaScript.
2. Phân quyền WinForms ở service và chính sách môn/lớp/tác giả; xem lại quyền SQL của client desktop.
3. Luồng đợt thi dựa trên `DotThiID`: trạng thái, phạm vi, tư cách dự thi và chỗ đăng ký.
4. Transaction và validation cho tài khoản/đề/phân công; sửa truy vấn xóa đề và kiểm tra lịch sử.
5. Chính sách mật khẩu, email production, thu hồi phiên desktop, thống kê điểm nhất quán.
6. Bố cục admin, thông báo lỗi, ghi nhớ đăng nhập, accessibility; sau đó kiểm tải và triển khai phục hồi.

Tiêu chí nghiệm thu: mỗi lỗi có test mong đợi bị chặn/rollback/hiển thị đúng; chạy lại cả baseline và probe sau khi sửa. Không dùng số test pass tổng để thay thế việc đóng từng lỗi ưu tiên cao.
