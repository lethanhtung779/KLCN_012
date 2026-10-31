using System.Net;
using System.Text.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace QLThiTN.WinForms.Services;

/// <summary>Gui email khi admin nang tai khoan hoc vien len goi dong phi (VIP).</summary>
public static class VipEmailSender
{
    public static (bool Sent, string Error) GuiNangVip(string toEmail, string hoTen)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            return (false, "Tài khoản chưa có email để gửi thông báo.");

        var cfg = DocCauHinh();
        if (cfg is null)
            return (false, "Không tìm thấy cấu hình SMTP (appsettings.Email.json).");

        if (string.IsNullOrWhiteSpace(cfg.SmtpHost) || string.IsNullOrWhiteSpace(cfg.SmtpPassword))
            return (false, "SMTP chưa được cấu hình đủ host và mật khẩu.");

        var ten = string.IsNullOrWhiteSpace(hoTen) ? toEmail : hoTen.Trim();
        var html = $@"
<h2>Thông báo nâng cấp tài khoản VIP</h2>
<p>Xin chào <strong>{WebUtility.HtmlEncode(ten)}</strong>,</p>
<p>Tài khoản của bạn đã được nâng lên vip.</p>
<p>Bạn có thể đăng nhập và làm <strong>toàn bộ</strong> các đề thi trên hệ thống (kể cả đề dành cho học viên đóng phí).</p>
<p style=""margin-top:16px;font-size:13px;color:#666"">
  Email này do quản trị viên gửi khi cấp quyền đóng phí. Đây không phải email xác minh khi đăng ký tài khoản mới.
</p>";

        try
        {
            var fromAddress = string.IsNullOrWhiteSpace(cfg.FromAddress) ? cfg.SmtpUser : cfg.FromAddress;
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                string.IsNullOrWhiteSpace(cfg.FromName) ? "Luyện Thi 247" : cfg.FromName,
                fromAddress));
            message.To.Add(MailboxAddress.Parse(toEmail.Trim()));
            message.Subject = "[Luyện Thi 247] Tài khoản của bạn đã được nâng lên VIP";
            message.Body = new TextPart("html") { Text = html };

            var secure = cfg.SmtpPort == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            using var client = new SmtpClient();
            client.Connect(cfg.SmtpHost, cfg.SmtpPort <= 0 ? 587 : cfg.SmtpPort, secure);
            if (!string.IsNullOrWhiteSpace(cfg.SmtpUser))
                client.Authenticate(cfg.SmtpUser, cfg.SmtpPassword);
            client.Send(message);
            client.Disconnect(true);
            return (true, "");
        }
        catch (Exception ex)
        {
            return (false, "Không gửi được email: " + ex.Message);
        }
    }

    private static EmailConfig? DocCauHinh()
    {
        foreach (var path in TimFile())
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (!doc.RootElement.TryGetProperty("Email", out var email))
                    continue;
                return new EmailConfig
                {
                    SmtpHost = ReadString(email, "SmtpHost"),
                    SmtpPort = email.TryGetProperty("SmtpPort", out var port) && port.TryGetInt32(out var p) ? p : 587,
                    SmtpUser = ReadString(email, "SmtpUser"),
                    SmtpPassword = ReadString(email, "SmtpPassword"),
                    FromAddress = ReadString(email, "FromAddress"),
                    FromName = ReadString(email, "FromName")
                };
            }
            catch
            {
                // thu file ke tiep
            }
        }
        return null;
    }

    private static IEnumerable<string> TimFile()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(dir.FullName, "appsettings.Email.json"),
                Path.Combine(dir.FullName, "QLThiTN.API", "appsettings.Email.json")
            })
            {
                if (seen.Add(candidate) && File.Exists(candidate))
                    yield return candidate;
            }
        }
    }

    private static string ReadString(JsonElement email, string name) =>
        email.TryGetProperty(name, out var value) ? value.GetString() ?? "" : "";

    private sealed class EmailConfig
    {
        public string SmtpHost { get; set; } = "";
        public int SmtpPort { get; set; } = 587;
        public string SmtpUser { get; set; } = "";
        public string SmtpPassword { get; set; } = "";
        public string FromAddress { get; set; } = "";
        public string FromName { get; set; } = "";
    }
}
