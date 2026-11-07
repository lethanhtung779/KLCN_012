using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QLThiTN.API.Common;
using QLThiTN.API.Data;
using QLThiTN.API.DTOs;
using QLThiTN.API.Entities;
using Desktop = QLThiTN.WinForms.Services;

var repo = Path.GetFullPath(args[0]);
var apiRoot = Path.Combine(repo, "QLThiTN", "QLThiTN.API");
var configuration = new ConfigurationBuilder().SetBasePath(apiRoot).AddJsonFile("appsettings.json").Build();
var name = "QLThiTN_AuthRegression_" + Guid.NewGuid().ToString("N");
var cs = new SqlConnectionStringBuilder(configuration.GetConnectionString("ThiTracNghiem")) { InitialCatalog = name, ConnectTimeout = 3 };
var options = new DbContextOptionsBuilder<QLThiTNDbContext>().UseSqlServer(cs.ConnectionString).Options;
await using var db = new TestDb(options);
var temporary = Path.Combine(Path.GetTempPath(), name); Directory.CreateDirectory(temporary);
Process? api = null; bool created = false, offline = false; int passed = 0;
void Check(bool condition, string label) { if (!condition) throw new Exception("FAIL: " + label); Console.WriteLine("PASS: " + label); passed++; }
void SetDesktopConnection(string value) {
    File.WriteAllText(Path.Combine(temporary, "appsettings.json"), JsonSerializer.Serialize(new { ConnectionStrings = new { ThiTracNghiem = value } }));
    Desktop.Database.LoadConfig(temporary);
}
async Task SetOffline(bool value) {
    if (!name.StartsWith("QLThiTN_AuthRegression_") || cs.InitialCatalog != name) throw new Exception("Unsafe database");
    var master = new SqlConnectionStringBuilder(cs.ConnectionString) { InitialCatalog = "master" };
    await using var con = new SqlConnection(master.ConnectionString); await con.OpenAsync();
    await using var cmd = con.CreateCommand();
    cmd.CommandText = $"ALTER DATABASE [{name}] SET " + (value ? "OFFLINE WITH ROLLBACK IMMEDIATE" : "ONLINE");
    await cmd.ExecuteNonQueryAsync(); offline = value;
}
try {
    await db.Database.EnsureCreatedAsync(); created = true;
    // Adapt EF-generated defaults to the existing deployment migration, in this
    // disposable fixture only. Production schema is never modified by the test.
    await db.Database.ExecuteSqlRawAsync("""
        DECLARE @s nvarchar(max)=N'';
        SELECT @s+=N'ALTER TABLE '+QUOTENAME(OBJECT_NAME(d.parent_object_id))+N' DROP CONSTRAINT '+QUOTENAME(d.name)+N';'
        FROM sys.default_constraints d JOIN sys.columns c ON c.object_id=d.parent_object_id AND c.column_id=d.parent_column_id
        WHERE OBJECT_NAME(d.parent_object_id) IN ('HocVien','GiaoVien') AND c.name IN ('TinhTrangCha','TinhTrangMe');
        EXEC sp_executesql @s;
        """);
    var account = new TaiKhoan { TenDangNhap="auth-regression",Email="auth-regression@example.test",MatKhau=PasswordHelper.Hash("Original-123!"),VaiTro="QuanTriVien",TrangThai="HoatDong" };
    db.Add(account); await db.SaveChangesAsync();
    var probe = new TcpListener(IPAddress.Loopback,0);probe.Start();var port=((IPEndPoint)probe.LocalEndpoint).Port;probe.Stop();
    var start = new ProcessStartInfo("dotnet") { WorkingDirectory=apiRoot,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true };
    start.ArgumentList.Add(Path.Combine(apiRoot,"bin","Debug","net10.0","QLThiTN.API.dll"));
    start.Environment["ASPNETCORE_URLS"]=$"http://127.0.0.1:{port}";
    start.Environment["ASPNETCORE_ENVIRONMENT"]="Development";
    start.Environment["ConnectionStrings__ThiTracNghiem"]=cs.ConnectionString;
    start.Environment["Logging__LogLevel__Default"]="Error";
    api=Process.Start(start)!;var stdout=api.StandardOutput.ReadToEndAsync();var stderr=api.StandardError.ReadToEndAsync();
    using var http = new HttpClient { BaseAddress=new Uri($"http://127.0.0.1:{port}"),Timeout=TimeSpan.FromSeconds(20) };
    var ready=false;
    for (var i=0;i<100 && !api.HasExited;i++) {try {ready=(await http.GetAsync("/api/monhoc")).IsSuccessStatusCode;}catch(HttpRequestException){} if(ready)break;await Task.Delay(200);}
    if(!ready) throw new Exception("API did not start: "+(api.HasExited ? await stdout+await stderr : "timeout"));
    var before=await db.TaiKhoans.CountAsync();
    Check((await http.PostAsJsonAsync("/api/auth/google",new {Email=account.Email,FullName="Forged",GoogleId="fake"})).StatusCode==HttpStatusCode.BadRequest,"KT089: email-only impersonation rejected over HTTP");
    Check((await http.PostAsJsonAsync("/api/auth/google",new {IdToken="not-a-google-token"})).StatusCode==HttpStatusCode.Unauthorized,"KT089: malformed ID token rejected");
    Check(await db.TaiKhoans.CountAsync()==before,"KT089: rejected requests create no accounts");
    async Task<HttpClient> Login(string password) {
        var response=await http.PostAsJsonAsync("/api/auth/login",new {Username=account.TenDangNhap,Password=password});response.EnsureSuccessStatusCode();
        var data=(await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        var client=new HttpClient {BaseAddress=http.BaseAddress,Timeout=TimeSpan.FromSeconds(20)};
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",data.AccessToken);return client;
    }
    var profile=$"/api/auth/profile/{account.TaiKhoanID}";
    using var old=await Login("Original-123!");
    Check((await old.GetAsync(profile)).IsSuccessStatusCode,"KT011: original token works before lock");
    await db.TaiKhoans.Where(x=>x.TaiKhoanID==account.TaiKhoanID).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.TrangThai,"BiKhoa"));
    Check((await old.GetAsync(profile)).StatusCode==HttpStatusCode.Unauthorized,"KT011: previously used token rejected immediately after SQL lock");
    Check((await http.PostAsJsonAsync("/api/auth/login",new {Username=account.TenDangNhap,Password="Original-123!"})).StatusCode==HttpStatusCode.Unauthorized,"KT011: locked account cannot log in");
    await db.TaiKhoans.Where(x=>x.TaiKhoanID==account.TaiKhoanID).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.TrangThai,"HoatDong"));
    using var passwordClient=await Login("Original-123!");
    Check((await passwordClient.PutAsJsonAsync("/api/auth/password",new {TaiKhoanID=account.TaiKhoanID,MatKhauCu="Original-123!",MatKhauMoi="Changed-123!"})).IsSuccessStatusCode,"KT011: password change succeeds");
    Check((await passwordClient.GetAsync(profile)).StatusCode==HttpStatusCode.Unauthorized,"KT011: password change immediately revokes current token");
    Check((await old.GetAsync(profile)).StatusCode==HttpStatusCode.Unauthorized,"KT011: password change revokes other existing token");
    using var fresh=await Login("Changed-123!");
    Check((await fresh.GetAsync(profile)).IsSuccessStatusCode,"KT011: new password issues usable token");
    SetDesktopConnection(cs.ConnectionString);
    var desktopAuth=new Desktop.AuthService();
    Check(desktopAuth.Login(account.TenDangNhap,"Changed-123!").User!=null,"KT096: WinForms login against isolated SQL database");
    Check(Desktop.Session.IsAccountActive(),"KT096: active SQL account passes permission check");
    await SetOffline(true);
    Check((await fresh.GetAsync(profile)).StatusCode==HttpStatusCode.ServiceUnavailable,"KT096: API denies protected request with 503 while test database offline");
    var blocked=false;
    try {Desktop.Session.YeuCauQuanTriVien();}catch(InvalidOperationException ex) {blocked=ex.Message.Contains("Không thể xác minh quyền");}
    Check(blocked,"KT096: WinForms protected operation blocked with connection-specific error");
    Check(Desktop.Session.Current!=null,"KT096: transient outage does not destroy login state");
    await SetOffline(false);SqlConnection.ClearAllPools();
    Check(Desktop.Session.IsAccountActive(),"KT096: permission recovers after SQL reconnect");
    // SqlClient briefly blocks a pool after a login failure; users can retry
    // without logging in again once that cooldown expires.
    var recovered = false;
    for (var i = 0; i < 15; i++)
    {
        var retry = await fresh.GetAsync(profile);
        if (retry.IsSuccessStatusCode) { recovered = true; break; }
        if (retry.StatusCode != HttpStatusCode.ServiceUnavailable)
            throw new Exception("Outage retry did not remain fail-closed");
        await Task.Delay(1000);
    }
    Check(recovered,"KT096: API recovers after SQL reconnect without a new login");
    await db.TaiKhoans.Where(x=>x.TaiKhoanID==account.TaiKhoanID).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.VaiTro,"GiaoVien"));
    Check((await fresh.GetAsync(profile)).StatusCode==HttpStatusCode.Unauthorized,"Role change invalidates API role claims");
    Check(!Desktop.Session.IsAccountActive() && Desktop.Session.Current==null,"Role change invalidates WinForms cached role");
    Console.WriteLine($"All {passed} authentication regression checks passed. Database: {name}");
} finally {
    Desktop.Session.Clear();
    if(api is {HasExited:false}) {api.Kill(true);await api.WaitForExitAsync();}api?.Dispose();
    if(offline)await SetOffline(false);
    if(created && new SqlConnectionStringBuilder(db.Database.GetConnectionString()).InitialCatalog==name)await db.Database.EnsureDeletedAsync();
    File.Delete(Path.Combine(temporary,"appsettings.json"));Directory.Delete(temporary);
}
sealed class TestDb(DbContextOptions<QLThiTNDbContext> options):QLThiTNDbContext(options) {
 protected override void OnModelCreating(ModelBuilder m) {base.OnModelCreating(m);foreach(var fk in m.Model.GetEntityTypes().SelectMany(t=>t.GetForeignKeys()))fk.DeleteBehavior=DeleteBehavior.Restrict;}
}
