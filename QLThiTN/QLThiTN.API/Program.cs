using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QLThiTN.API.Data;
using QLThiTN.API.Services;

var builder = WebApplication.CreateBuilder(args);
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
app.UseHttpsRedirection();
app.UseAuthentication();
// Re-check active status and password on authenticated requests, cached in memory to avoid repetitive DB hits
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var accountId = context.User.AccountId();
        var cache = context.RequestServices.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
        var cacheKey = $"Auth_Account_Stamp_{accountId}";

        if (!cache.TryGetValue(cacheKey, out (string TrangThai, string VaiTro, string Stamp) cachedAuth))
        {
            var db = context.RequestServices.GetRequiredService<QLThiTNDbContext>();
            var account = await db.TaiKhoans.AsNoTracking()
                .FirstOrDefaultAsync(t => t.TaiKhoanID == accountId);
            var stamp = account is null ? "" : Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(account.MatKhau)));
            cachedAuth = (account?.TrangThai ?? "", account?.VaiTro ?? "", stamp);
            cache.Set(cacheKey, cachedAuth, TimeSpan.FromSeconds(60));
        }

        if (string.IsNullOrEmpty(cachedAuth.TrangThai) || cachedAuth.TrangThai != "HoatDong"
            || !context.User.IsInRole(cachedAuth.VaiTro)
            || context.User.FindFirst("PasswordStamp")?.Value != cachedAuth.Stamp)
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
