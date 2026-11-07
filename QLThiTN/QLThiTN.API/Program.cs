using Microsoft.EntityFrameworkCore;

using QLThiTN.API.Data;
using QLThiTN.API.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Google.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.Email.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.Payment.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});
builder.Services.Configure<VipPaymentSettings>(builder.Configuration.GetSection("VipPayment"));
builder.Services.AddHttpClient<PayOsGateway>(client => {
    client.BaseAddress = new Uri("https://api-merchant.payos.vn/");
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHostedService<ExamExpiryWorker>();
builder.Services.AddAuthentication("Bearer").AddBearerToken("Bearer", options =>
    options.BearerTokenExpiration = TimeSpan.FromHours(8));
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<AppUrlSettings>(builder.Configuration.GetSection("App"));
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<RegistrationEmailService>();

builder.Services.AddDbContext<QLThiTNDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ThiTracNghiem")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<QLThiTNDbContext>();
    await db.Database.ExecuteSqlRawAsync(GoiThi.SchemaSql);
    await db.Database.ExecuteSqlRawAsync(VipPaymentGateway.SchemaSql);
    await db.Database.ExecuteSqlRawAsync(QLThiTN.Shared.WorkflowSchema.Sql);
    await db.Database.ExecuteSqlRawAsync(QLThiTN.API.Controllers.ExamActivityController.SchemaSql);
}

app.UseResponseCompression();
if (!app.Environment.IsDevelopment()) { app.UseHttpsRedirection(); }
app.UseAuthentication();
// Check persisted state on every request: Web, WinForms and SQL administration
// can all change accounts, so a process-local TTL cannot guarantee revocation.
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var db = context.RequestServices.GetRequiredService<QLThiTNDbContext>();
        QLThiTN.API.Entities.TaiKhoan? account;
        try
        {
            account = await db.TaiKhoans.AsNoTracking().FirstOrDefaultAsync(
                t => t.TaiKhoanID == context.User.AccountId(), context.RequestAborted);
        }
        catch (Exception ex) when (ex is Microsoft.Data.SqlClient.SqlException
            || ex.InnerException is Microsoft.Data.SqlClient.SqlException)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new { message = "Không xác minh được phiên do lỗi kết nối dữ liệu. Vui lòng thử lại." });
            return;
        }
        var stamp = account is null ? "" : Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(account.MatKhau)));
        if (account?.TrangThai != "HoatDong" || !context.User.IsInRole(account.VaiTro)
            || context.User.FindFirst("PasswordStamp")?.Value != stamp)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
    }
    await next(context);
});
app.UseAuthorization();
app.MapControllers();

app.Run();
