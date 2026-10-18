using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace QLThiTN.API.Services;

public class EmailSettings
{
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string FromAddress { get; set; } = "noreply@qlthitn.local";
    public string FromName { get; set; } = "QLThiTN";
    public bool UseSsl { get; set; } = true;
}

public class SmtpEmailSender(IOptions<EmailSettings> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailSettings _settings = options.Value;

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost) || string.IsNullOrWhiteSpace(_settings.SmtpPassword))
        {
            logger.LogWarning(
                "Chua cau hinh SMTP (Email:SmtpHost). Email toi {Email} — tieu de: {Subject}. Noi dung HTML da ghi log.",
                toEmail, subject);
            logger.LogInformation("EMAIL BODY -> {To}: {Body}", toEmail, htmlBody);
            return;
        }

        var from = string.IsNullOrWhiteSpace(_settings.FromAddress) ? _settings.SmtpUser : _settings.FromAddress;
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, from));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        var secure = _settings.SmtpPort == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, secure, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_settings.SmtpUser))
            await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPassword, cancellationToken);

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
