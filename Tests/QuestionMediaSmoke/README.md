# Kiểm tra câu hỏi có ảnh và công thức

Chạy trên Windows có .NET 10 SDK, Microsoft Edge WebView2 Runtime và SQL Server được cấu hình trong WinForms:

```powershell
dotnet run --project Tests/QuestionMediaSmoke/QuestionMediaSmoke.csproj -p:UseAppHost=false
```

Kiểm tra bố cục form, xem trước công thức/ảnh bằng WebView2 thực, mã HTML trong nội dung không được thực thi, thêm/sửa/xóa ảnh cho ba loại câu hỏi, giữ ID ảnh, bảo vệ câu hỏi đã dùng và hoàn tác khi lưu thất bại.

Phần import kiểm tra tạo/đọc lại mẫu XLSX/TXT/ZIP, TSV/CSV có dấu ngoặc kép/xuống dòng, ảnh bị thiếu và đường dẫn vượt thư mục, nhiều file dữ liệu trong ZIP, ảnh nhúng/công thức Excel, chủ đề trùng tên, dòng không được chọn và chống nhập lặp trong cùng màn hình. Kiểm tra WebView2 thực khi đổi nhanh dòng và ghi gói ZIP vào database thử riêng.

Nếu WinForms đang chạy trong Visual Studio và khóa file build, dùng output riêng:

```powershell
dotnet build Tests/QuestionMediaSmoke/QuestionMediaSmoke.csproj -p:UseAppHost=false -o ../test/import-checks
dotnet ../test/import-checks/QuestionMediaSmoke.dll
```

Bài kiểm tra đọc danh mục hiện tại để mở form. Phần ghi dữ liệu chỉ chạy trong database tạm có tên `QLThiTN_MediaSmoke_<GUID>` và thư mục ảnh tạm riêng, rồi dọn chúng trong `finally`. Tài khoản SQL cần quyền tạo/xóa database thử nghiệm. Các ảnh chụp giao diện nằm trong thư mục output của project kiểm tra.
