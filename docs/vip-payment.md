# Thanh toán VIP cá nhân qua payOS

Chỉ học viên có vai trò HocVien và NguonTaiKhoan = VangLai được mua. Học viên trung tâm không có mục thanh toán và API từ chối cả truy cập trực tiếp. Thanh toán không đổi nguồn tài khoản hoặc xếp lớp.

## Gói VIP

- VIP 30 ngày: 300.000đ, có hiệu lực đúng 30 ngày từ lúc máy chủ xác nhận.
- VIP 1 năm: 1.500.000đ, tính theo AddYears(1), có xử lý ngày nhuận.
- Không tự động gia hạn. VIP còn hạn không được mua lại; hết hạn có thể mua gói mới.
- VIP thủ công cũ không có ngày hết hạn vẫn giữ nguyên quyền.

## Thiết lập payOS

1. Tạo tài khoản, xác thực và tạo kênh thanh toán tại https://my.payos.vn theo hướng dẫn https://payos.vn/docs/.
2. Lấy Client ID, API Key và Checksum Key của cùng một kênh. Không dùng TmnCode/HashSecret VNPay.
3. Dừng API cũ bằng Ctrl+C. Trong terminal PowerShell ở thư mục gốc dự án, chạy từng dòng sau. Giữ nguyên câu nhắc Read-Host rồi nhập giá trị khi terminal hỏi (không thay câu nhắc bằng khóa).

```powershell
$env:VipPayment__Enabled = "true"
$env:VipPayment__ClientId = Read-Host "Nhap Client ID payOS"
$env:VipPayment__ApiKey = [System.Net.NetworkCredential]::new("", (Read-Host "Nhap API Key payOS" -AsSecureString)).Password
$env:VipPayment__ChecksumKey = [System.Net.NetworkCredential]::new("", (Read-Host "Nhap Checksum Key payOS" -AsSecureString)).Password
$env:VipPayment__ReturnUrl = "http://localhost:7245/Vip/Return"
$env:VipPayment__CancelUrl = "http://localhost:7245/Vip/Cancel"
dotnet run --project .\QLThiTN\QLThiTN.API --launch-profile http
```

Các biến chỉ có hiệu lực trong terminal này và tiến trình khởi động từ đó. Không nén/gửi khóa bí mật hoặc ghi chúng vào Git. File appsettings.Payment.example.json chỉ là mẫu, không tự nạp. Cấu hình VNPay cũ không còn được đọc.

4. Chạy web trong terminal khác:

```powershell
dotnet run --project .\QLThiTN\QLThiTN.Web --launch-profile http
```

5. Giữ ngrok trỏ đến API (chỉ một phiên sử dụng cùng endpoint):

```powershell
ngrok http 5264
```

6. Trong kênh thanh toán payOS, cấu hình Webhook URL thành:

```text
https://TEN-MIEN-NGROK-CUA-BAN/api/vip-payment/webhook
```

Đây là POST webhook; không dùng đường /ipn cũ. payOS gửi dữ liệu mẫu có chữ ký khi đăng ký webhook; hệ thống xác minh và trả 200 cho mã đơn không tồn tại, không cấp VIP. ReturnUrl và CancelUrl là trang web, dùng localhost khi thử trên chính máy chạy web; thử từ điện thoại/máy khác cần URL công khai cho web. Ngrok đổi tên miền thì cập nhật webhook.

7. Mở http://localhost:7245 → đăng nhập học viên vãng lai → Hồ sơ → Nâng cấp VIP cá nhân. Sau khi nhận webhook hợp lệ, bấm Cập nhật trạng thái để thấy VIP và ngày hết hạn.

## Môi trường thanh toán

Theo tài liệu https://payos.vn/docs/moi-truong-test/, payOS không cung cấp sandbox riêng. Chuyển khoản qua kênh thật là giao dịch tiền thật. Hai giá gói ở trên là giá thật được gửi lên payOS. Bộ kiểm thử tự động của dự án giả lập HTTP payOS, không chuyển tiền và không cần khóa thật; đây không phải một chế độ thanh toán demo trên giao diện.

## Tính toàn vẹn giao dịch

- Máy chủ chọn giá/thời hạn từ PlanId, không nhận số tiền do trình duyệt cung cấp.
- Lưu đơn và mã orderCode trước khi gọi payOS. Khi mất phản hồi, thử lại cùng mã đơn; nếu payOS đã tạo, truy vấn lại đơn đó.
- Bấm cùng gói trong 15 phút dùng lại đơn. Đổi gói khi còn đơn đang chờ bị chặn.
- Xác minh chữ ký HMAC-SHA256 trên data; kiểm tra mã đơn, số tiền VND, paymentLinkId và mã thành công trong phần data đã ký.
- Thông báo lặp không cấp VIP thêm. Đơn và quyền được cập nhật trong cùng transaction, có khóa hàng học viên và chỉ mục giao dịch duy nhất.
- Return/Cancel URL không tự cấp hoặc thu hồi VIP. Nút quay lại/hủy trên trình duyệt không được coi là bằng chứng ngân hàng đã hủy giao dịch; đơn chưa trả tiền hết hiệu lực sau 15 phút.
- Webhook hợp lệ đến muộn vẫn được ghi nhận. Chưa có chức năng hoàn tiền hoặc đối soát định kỳ; khoản trả trùng khác đơn cần đối soát thủ công.

## Chuyển đổi từ VNPay

Khởi động API mới tự thêm Provider, OrderCode, PaymentLinkId vào bảng VipPayment. Dữ liệu cũ được đánh dấu VNPay; lịch sử và quyền VIP đã cấp được giữ nguyên. Đơn mới dùng payOS, không tái sử dụng link VNPay cũ. Endpoint VNPay /ipn được gỡ bỏ; không tiếp tục thanh toán đơn VNPay còn chờ. Nếu có giao dịch VNPay phát sinh khi chuyển đổi, đối chiếu và xử lý thủ công trước khi hoàn tất bàn giao.

## Kiểm thử

```powershell
dotnet run --project .\Tests\VipPaymentChecks -c Release -- --integration
```

Cần SQL Server theo cấu hình API. Kiểm thử tạo và tự xóa database riêng QLThiTN_VipChecks_<GUID>, không sửa dữ liệu nghiệp vụ, không gọi payOS thật. Kiểm tra phân quyền, hai mức giá, hết hạn, chữ ký chuẩn payOS, thông báo giả mạo/lặp, dữ liệu VNPay cũ và khôi phục đơn khi mất phản hồi.

Tài liệu giao thức: https://payos.vn/docs/api/ và https://payos.vn/docs/tich-hop-webhook/kiem-tra-du-lieu-voi-signature/.