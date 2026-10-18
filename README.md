# KLCN_012 — Hệ thống quản lý và thi trắc nghiệm (QLThiTN)

## Chạy nhanh (một lệnh terminal)

Yêu cầu: **.NET 10 SDK**, **SQL Server LocalDB** (có sẵn khi cài Visual Studio / SQL Express).

Đặt file backup `ThiTracNghiem_20261009_235247.bak` ở **thư mục gốc** dự án (cùng cấp `run.ps1`).

```powershell
cd "e:\hoc tap\hoc\KLCN\KLCN_012-main"
.\run.ps1
```

Hoặc double-click / CMD:

```cmd
run.cmd
```

Script sẽ:

1. Bật LocalDB và **restore** database từ file `.bak`
2. Cập nhật connection string + thư mục ảnh `QLThiTN\tai_nguyen`
3. Build và chạy **API** (5264) + **Web** (7245)

Mở trình duyệt: **http://localhost:7245**

### Tùy chọn

| Lệnh | Ý nghĩa |
|------|---------|
| `.\run.ps1 -SkipRestore` | Không restore lại DB (chạy nhanh hơn) |
| `.\run.ps1 -WinForms` | Mở thêm app desktop WinForms |
| `.\run.ps1 -SkipBuild` | Bỏ qua build (đã build sẵn) |

Nếu dùng SQL Server khác (không phải LocalDB):

```powershell
.\run.ps1 -SqlServer ".\SQLEXPRESS"
```

## Email xác minh đăng ký (Brevo — không cần bật 2 bước Gmail)

API gửi mail qua SMTP. Dùng **Brevo** (miễn phí): đăng ký tài khoản, xác minh email người gửi, tạo **SMTP key**.

```powershell
cd "e:\hoc tap\hoc\KLCN\KLCN_012-main"
.\setup-email-brevo.ps1
```

Sửa `QLThiTN\QLThiTN.API\appsettings.Email.json` (file này không commit git):

| Trường | Giá trị |
|--------|---------|
| `SmtpHost` | `smtp-relay.brevo.com` |
| `SmtpPort` | `587` |
| `SmtpUser` | Email đăng nhập Brevo (xem trang SMTP) |
| `SmtpPassword` | SMTP key từ Brevo |
| `FromAddress` | Email đã **xác minh** trong Brevo → Senders |

Trong `appsettings.json` giữ `App:WebBaseUrl` = `http://localhost:7245` để link trong email đúng. **Khởi động lại API** sau khi cấu hình.

Chưa có `appsettings.Email.json` → dev vẫn hiện link xác minh trên web thay vì gửi mail.
