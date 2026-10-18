using System.Text.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

var cfgPath = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "QLThiTN", "QLThiTN.API", "appsettings.Email.json");
cfgPath = Path.GetFullPath(cfgPath);
if (!File.Exists(cfgPath))
{
    Console.WriteLine("Khong tim thay: " + cfgPath);
    return 1;
}

var json = await File.ReadAllTextAsync(cfgPath);
using var doc = JsonDocument.Parse(json);
var e = doc.RootElement.GetProperty("Email");
var host = e.GetProperty("SmtpHost").GetString()!;
var port = e.GetProperty("SmtpPort").GetInt32();
var user = e.GetProperty("SmtpUser").GetString()!;
var pass = e.GetProperty("SmtpPassword").GetString()!;
var from = e.GetProperty("FromAddress").GetString()!;

Console.WriteLine("--- Kiem tra cau hinh ---");
Console.WriteLine("Host: " + host + "  Port: " + port);
Console.WriteLine("SmtpUser: " + user);
Console.WriteLine("FromAddress: " + from);
Console.WriteLine("Key length: " + pass.Length + "  Prefix: " + (pass.Length >= 8 ? pass[..8] : "( ngan )"));

if (pass.StartsWith("xkeysib-", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("LOI: Day la API key (xkeysib), KHONG phai SMTP key. Tao key o muc SMTP tren Brevo.");
    return 1;
}

if (!pass.StartsWith("xsmtpsib-", StringComparison.OrdinalIgnoreCase))
    Console.WriteLine("CANH BAO: SMTP key Brevo thuong bat dau bang xsmtpsib-");

if (user.Contains("smtp-relay", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("LOI: SmtpUser khong duoc la smtp-relay.brevo.com — copy truong Login tren Brevo.");
    return 1;
}

if (!user.Contains('@'))
{
    Console.WriteLine("LOI: SmtpUser phai la Login tren Brevo (email hoac xxx@smtp-brevo.com).");
    return 1;
}

Console.WriteLine();
Console.WriteLine("Neu van 535: vao Brevo -> Settings -> SMTP & API -> tab SMTP -> copy CHINH XAC o 'Login' vao SmtpUser.");
Console.WriteLine("     (Co the la xxx@smtp-brevo.com, KHONG phai Gmail dang ky sender.)");
Console.WriteLine();

try
{
    var message = new MimeMessage();
    message.From.Add(new MailboxAddress("QLThiTN", from));
    var to = args.Length > 1 ? args[1] : from;
    message.To.Add(MailboxAddress.Parse(to));
    message.Subject = "QLThiTN SMTP test";
    message.Body = new TextPart("plain") { Text = "OK" };

    using var client = new SmtpClient();
    await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
    await client.AuthenticateAsync(user, pass);
    await client.SendAsync(message);
    await client.DisconnectAsync(true);
    Console.WriteLine("SMTP OK — da gui test toi " + to);
    return 0;
}
catch (AuthenticationException ex)
{
    Console.WriteLine("FAIL: " + ex.Message);
    Console.WriteLine("=> Sua SmtpUser = Login tren Brevo; SmtpPassword = SMTP key MOI (tab SMTP, khong phai API key).");
    return 1;
}
