# Soạn câu hỏi có ảnh và công thức

Trong **Ngân hàng câu hỏi → Thêm câu hỏi/Sửa**:

- Tab **Nội dung câu hỏi**: gõ nội dung; chọn mẫu Phân số, Căn, Số mũ, Tích phân hoặc LaTeX để chỉnh và chèn công thức tại con trỏ. Có thể gõ trực tiếp `$...$` hoặc `$$...$$`.
- **Chọn ảnh** hỗ trợ PNG, JPEG, BMP, GIF; **Dán ảnh** hoặc Ctrl+V trong ô nội dung nhận ảnh từ clipboard. Có thể thêm nhiều ảnh, xem ảnh, sửa chú thích và xóa ảnh đang chọn. Ảnh mới được chuyển thành PNG.
- Tab **Đáp án**: mỗi đáp án trắc nghiệm/ý đúng-sai có phần chữ, công thức và ảnh riêng. Chọn đáp án đúng ở đầu tab hoặc Đúng/Sai cho từng ý. Câu trả lời ngắn cần đáp số bằng chữ/số để chấm bài.
- Tab **Xem trước**: hiển thị nội dung đang nhập, ảnh, công thức, đáp án đúng và giải thích; chưa cần lưu. Công thức không hợp lệ/không được hỗ trợ hiển thị màu đỏ.
- Bấm **Lưu câu hỏi** để lưu; **Hủy** bỏ thay đổi. Câu đã dùng trong đề thi/bài làm vẫn được bảo vệ khỏi chỉnh sửa.

## Cấu hình

`HinhAnhRoot` trong `appsettings.json` của WinForms phải trỏ đến cùng thư mục ảnh với API; tài khoản chạy WinForms cần quyền ghi và API cần quyền đọc. Nếu chạy trên hai máy, dùng thư mục chia sẻ mà cả hai máy truy cập được. Database chỉ lưu đường dẫn tương đối `winforms/<GUID>.png`.

Ảnh mới chỉ được ghi khi lưu; lỗi lưu sẽ rollback database và dọn file mới của lần lưu đó. Xóa ảnh khỏi câu hỏi chỉ gỡ liên kết database, không xóa file cũ có thể đang được sử dụng ở nơi khác. Giới hạn ảnh nhập là 15 MB và 25 triệu điểm ảnh. GIF được lấy khung ảnh hiện tại, không giữ hoạt ảnh.

Máy cần Microsoft Edge WebView2 Runtime để xem trước. KaTeX/font được đóng gói cùng ứng dụng; xem trước không cần Internet. Ảnh WebP cũ xem được trong tab Xem trước; bộ chọn ảnh mới dùng các định dạng nêu trên.

## Import Excel/TXT/ZIP

1. Trong **Ngân hàng câu hỏi**, bấm **Tạo file mẫu**, chọn ZIP có ảnh mẫu, Excel hoặc TXT. Chủ đề/môn mặc định lấy từ bộ lọc hiện tại. Các câu trong mẫu chỉ minh họa định dạng, cần thay trước khi nhập.
2. Công thức trong các ô nội dung, đáp án, giải thích dùng LaTeX `$...$`. Không dùng công thức tính toán Excel bắt đầu bằng `=` hay đối tượng Equation. Ảnh nhúng trong sheet Excel sẽ được báo lỗi, không bị âm thầm bỏ qua.
3. Với ảnh, chuẩn bị ZIP gồm **đúng một file dữ liệu** `.xlsx`, `.txt` hoặc `.csv` và thư mục `images`. Ví dụ:

   ```text
   cau_hoi.xlsx
   images/cau1.png
   images/dapan_a.png
   ```

4. Điền các cột `AnhCauHoi`, `AnhA`, `AnhB`, `AnhC`, `AnhD` bằng đường dẫn tương đối so với file dữ liệu, ví dụ `images/cau1.png`. Nhiều ảnh phân cách bằng dấu `;`. Không dùng đường dẫn tuyệt đối hoặc `..`. Các cột ảnh có thể để trống; Excel/TXT cũ 16 cột vẫn được hỗ trợ.
5. `ChuDe` phải đúng tên chủ đề hiện có. Nếu các môn có chủ đề trùng tên, điền thêm `MonHoc`. Trắc nghiệm cần `DapAnDung` là A/B/C/D. Đúng-sai cần đủ 4 ý và các cột `Y_a_Dung` đến `Y_d_Dung` là D/S. Trả lời ngắn cần `DapSo` bằng chữ/số, chỉ gắn ảnh ở `AnhCauHoi`.
6. Bấm **Import Excel/TXT/ZIP**, chọn file, chọn từng dòng để xem trước. Dòng lỗi hiển thị màu đỏ. Bỏ dấu chọn ở các dòng chưa muốn nhập, rồi bấm **Nhập các dòng đã chọn**. Không có dữ liệu nào được lưu chỉ bằng việc mở bản xem trước.

TXT mẫu dùng TAB và đặt các trường trong dấu ngoặc kép. Bộ đọc cũng hỗ trợ TXT dấu `|`, CSV dấu phẩy/chấm phẩy. Khi nội dung chứa dấu phân cách hoặc xuống dòng, phải đặt toàn bộ trường trong dấu ngoặc kép; dấu ngoặc kép bên trong viết hai lần. Nhờ đó các công thức như `$|x|$` vẫn được giữ nguyên.

Giới hạn mỗi lần: 500 câu hỏi; file Excel/TXT tối đa 20 MB; ZIP tối đa 100 MB, 2.000 mục và 200 MB sau giải nén. Ảnh được kiểm tra trước khi nhập, lưu theo cơ chế giao dịch của form soạn câu hỏi. Nếu một dòng lưu thất bại, dòng đó được báo lỗi và hoàn tác; các dòng khác đã nhập thành công vẫn được giữ. Không nhập lặp các dòng thành công trong cùng màn hình; mở lại cùng file là một lần nhập mới.

KaTeX hiển thị công thức lỗi/không hỗ trợ bằng màu đỏ trong bản xem trước. Kiểm tra dữ liệu theo dòng không phải công cụ xác minh tính đúng của nội dung toán học.
