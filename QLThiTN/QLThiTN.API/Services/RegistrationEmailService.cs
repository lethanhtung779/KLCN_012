using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QLThiTN.API.Common;
using QLThiTN.API.Data;
using QLThiTN.API.DTOs;
using QLThiTN.API.Entities;

namespace QLThiTN.API.Services;

public class AppUrlSettings
{
    public string WebBaseUrl { get; set; } = "http://localhost:7245";
}

public class RegistrationEmailService(
    QLThiTNDbContext db,
    IEmailSender emailSender,
    IOptions<AppUrlSettings> appUrls,
    IOptions<EmailSettings> emailSettings,
    IWebHostEnvironment env,
    ILogger<RegistrationEmailService> logger)
{
    private readonly AppUrlSettings _app = appUrls.Value;
    private readonly EmailSettings _email = emailSettings.Value;

    public async Task<(bool Ok, string Message, string? ConfirmUrlDev)> RequestRegistrationAsync(RegisterRequestDto request)
    {
        var userName = request.UserName.Trim();
        var email = request.Email.Trim();
        var fullName = request.FullName.Trim();

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName))
            return (false, "Vui lòng điền đầy đủ thông tin.", null);

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return (false, "Mật khẩu phải có ít nhất 6 ký tự.", null);

        if (!env.IsDevelopment() && (string.IsNullOrWhiteSpace(_email.SmtpHost) || string.IsNullOrWhiteSpace(_email.SmtpPassword)))
            return (false, "Hệ thống chưa cấu hình dịch vụ gửi email (SMTP). Vui lòng liên hệ quản trị viên.", null);

        if (await db.TaiKhoans.AnyAsync(t => t.TenDangNhap == userName))
            return (false, "Tên đăng nhập đã tồn tại.", null);

        if (await db.TaiKhoans.AnyAsync(t => t.Email == email))
            return (false, "Email đã được sử dụng.", null);

        var pending = await db.DangKyXacMinhEmails
            .Where(x => x.Email == email && !x.DaSuDung && x.HetHan > DateTime.UtcNow)
            .ToListAsync();
        if (pending.Count > 0)
            db.DangKyXacMinhEmails.RemoveRange(pending);

        var token = GenerateToken();
        var row = new DangKyXacMinhEmail
        {
            Email = email,
            TenDangNhap = userName,
            MatKhauHash = PasswordHelper.Hash(request.Password),
            HoTen = fullName,
            Token = token,
            HetHan = DateTime.UtcNow.AddHours(24),
            DaSuDung = false,
            NgayTao = DateTime.UtcNow
        };
        db.DangKyXacMinhEmails.Add(row);
        await db.SaveChangesAsync();

        var confirmUrl = BuildConfirmUrl(token);
        var html = $@"
<h2>Xác minh email đăng ký tài khoản</h2>
<p>Xin chào <strong>{WebUtilityEncode(fullName)}</strong>,</p>
<p>Bạn vừa đăng ký tài khoản học viên trên Luyện Thi 247. Đây là email <strong>xác minh địa chỉ email</strong> — tài khoản chưa được tạo cho đến khi bạn bấm liên kết bên dưới.</p>
<p style=""margin:16px 0;padding:12px;background:#f4f6fa;border-radius:6px;font-size:14px"">
  <strong>Lưu ý:</strong> Email này <em>không</em> phải thông báo nâng VIP hay cấp quyền đóng phí. Sau khi xác minh, tài khoản mới là <strong>gói miễn phí</strong> (chỉ làm các đề miễn phí).
</p>
<p style=""margin:24px 0"">
  <a href=""{confirmUrl}"" style=""background:#1b2a4e;color:#fff;padding:12px 20px;text-decoration:none;border-radius:6px"">Xác minh email của tôi</a>
</p>
<p>Hoặc mở liên kết (hiệu lực 24 giờ):<br/><a href=""{confirmUrl}"">{confirmUrl}</a></p>
<p>Nếu bạn không đăng ký, hãy bỏ qua email này.</p>";

        try
        {
            await emailSender.SendAsync(email, "[Luyện Thi 247] Vui lòng xác minh email đăng ký", html);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Gui email xac minh that bai toi {Email}", email);
            var hint = ex.Message.Contains("Authentication", StringComparison.OrdinalIgnoreCase)
                ? " Sai SMTP key hoặc SmtpUser — vào Brevo → SMTP & API, copy đúng SMTP login và tạo key mới."
                : "";
            return (false, "Không gửi được email xác minh. Kiểm tra cấu hình SMTP hoặc thử lại sau." + hint, null);
        }

        // Dev: luon tra link xac minh de test khi email vao spam / Brevo tre.
        var devUrl = env.IsDevelopment() ? confirmUrl : null;
        return (true,
            "Chúng tôi đã gửi email xác minh (không phải email VIP). Mở hộp thư, bấm liên kết xác minh để hoàn tất đăng ký (24 giờ).",
            devUrl);
    }

    public async Task<(bool Ok, string Message, AuthResponseDto? Auth)> ConfirmAsync(string token)
    {
        token = token.Trim();
        if (string.IsNullOrEmpty(token))
            return (false, "Liên kết xác minh không hợp lệ.", null);

        var row = await db.DangKyXacMinhEmails.FirstOrDefaultAsync(x => x.Token == token);
        if (row is null)
            return (false,
                "Liên kết không còn hiệu lực. Thường do bạn đã đăng ký lại (email mới thay link cũ) — hãy mở email mới nhất hoặc sau khi đăng ký bấm «Xác minh ngay» trên trang kiểm tra email. Nếu đã xác minh rồi, thử đăng nhập.",
                null);
        if (row.DaSuDung)
            return (false, "Liên kết xác minh đã được sử dụng. Hãy đăng nhập hoặc đăng ký lại.", null);
        if (row.HetHan < DateTime.UtcNow)
            return (false, "Liên kết xác minh đã hết hạn. Vui lòng đăng ký lại.", null);

        if (await db.TaiKhoans.AnyAsync(t => t.TenDangNhap == row.TenDangNhap))
            return (false, "Tên đăng nhập đã tồn tại. Vui lòng đăng ký với tên khác.", null);
        if (await db.TaiKhoans.AnyAsync(t => t.Email == row.Email))
            return (false, "Email đã được sử dụng.", null);

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var tk = new TaiKhoan
            {
                TenDangNhap = row.TenDangNhap,
                MatKhau = row.MatKhauHash,
                Email = row.Email,
                VaiTro = "HocVien",
                TrangThai = "HoatDong",
                NgayTao = DateTime.Now
            };
            db.TaiKhoans.Add(tk);
            row.DaSuDung = true;
            await db.SaveChangesAsync();

            db.HocViens.Add(new HocVien
            {
                TaiKhoanID = tk.TaiKhoanID,
                HoTen = row.HoTen,
                LoaiHocVien = "HocSinh",
                LoaiGoi = GoiThi.MienPhi,
                NgayDangKy = DateTime.Now
            });
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            var auth = await BuildResponseAsync(tk);
            return (true, "Xác minh email thành công! Bạn có thể đăng nhập.", auth);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            logger.LogError(ex, "Lỗi khi kích hoạt tài khoản xác minh email {Email}", row.Email);
            return (false, "Có lỗi xảy ra khi tạo tài khoản. Vui lòng thử lại sau.", null);
        }
    }

    private string BuildConfirmUrl(string token)
    {
        var baseUrl = _app.WebBaseUrl.TrimEnd('/');
        return $"{baseUrl}/Account/ConfirmEmail?token={Uri.EscapeDataString(token)}";
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string WebUtilityEncode(string s) =>
        System.Net.WebUtility.HtmlEncode(s);

    private async Task<AuthResponseDto> BuildResponseAsync(TaiKhoan tk)
    {
        var hv = await db.HocViens.FirstOrDefaultAsync(h => h.TaiKhoanID == tk.TaiKhoanID);
        return new AuthResponseDto
        {
            TaiKhoanID = tk.TaiKhoanID,
            TenDangNhap = tk.TenDangNhap,
            Email = tk.Email,
            VaiTro = tk.VaiTro,
            HoTen = hv?.HoTen ?? tk.TenDangNhap,
            HocVienID = hv?.HocVienID
        };
    }
}
