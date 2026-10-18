using Microsoft.EntityFrameworkCore;
using QLThiTN.API.Data;
using QLThiTN.API.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Email.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers();
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
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();