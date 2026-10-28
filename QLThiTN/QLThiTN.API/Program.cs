using Microsoft.EntityFrameworkCore;
using QLThiTN.API.Data;
using QLThiTN.API.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Email.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers();
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
    await db.Database.ExecuteSqlRawAsync(QLThiTN.Shared.WorkflowSchema.Sql);
    await db.Database.ExecuteSqlRawAsync(QLThiTN.API.Controllers.ExamActivityController.SchemaSql);
}

app.UseHttpsRedirection();
app.UseAuthentication();
// Re-check active status and password on every authenticated request, so account
// locking and password changes revoke previously issued access tokens.
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var db = context.RequestServices.GetRequiredService<QLThiTNDbContext>();
        var account = await db.TaiKhoans.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TaiKhoanID == context.User.AccountId());
        var stamp = account is null ? "" : Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(account.MatKhau)));
        if (account is null || account.TrangThai != "HoatDong"
            || !context.User.IsInRole(account.VaiTro)
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
