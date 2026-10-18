# Audit quy trình học viên, giáo viên và admin

Đây là harness chẩn đoán: chạy lại baseline API/Web, tái hiện các lỗi nghiệp vụ, gọi service WinForms trên STA thread và chụp giao diện bằng Edge. Dòng `AUDIT` ghi nhận hành vi, không đồng nghĩa test pass. Báo cáo kết luận nằm tại `audit/2026-10-18/REPORT.md`.

Yêu cầu: Windows, .NET 10, SQL Server có quyền tạo/xóa database tạm, Edge và Node.js. Cài Playwright cho harness:

```powershell
npm install --prefix Tests/RoleWorkflowAudit
dotnet run --project Tests/RoleWorkflowAudit/RoleWorkflowAudit.csproj -- .
dotnet run --project Tests/RoleWorkflowAudit/RoleWorkflowAudit.csproj -- . --media-only
node --test Tests/ExamSecurityChecks/autosave.test.cjs
```

Harness sao chép baseline của `ExamSecurityChecks` để chạy chẩn đoán trên target Windows. Khi baseline thay đổi cần đồng bộ phần tương ứng; không chỉnh các probe thành test pass bằng cách mặc định chấp nhận hành vi lỗi.

Database dùng tên GUID riêng, xóa trong `finally`. API dùng content root tạm để không đọc SMTP thật. Không gọi BackupService restore vì implementation hiện cố định tên database. Payload HTML thử nghiệm chỉ gán biến trong trang của fixture cục bộ, không gửi dữ liệu ra ngoài.

Ảnh và observations được ghi vào thư mục audit. `--media-only` chạy bộ QuestionMediaSmoke hiện có trên fixture riêng thay vì để nó đọc database mặc định chưa cấu hình.
