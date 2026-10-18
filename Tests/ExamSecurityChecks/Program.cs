using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QLThiTN.API.Common;
using QLThiTN.API.Data;
using QLThiTN.API.DTOs;
using QLThiTN.API.Entities;

var repo = Path.GetFullPath(args.FirstOrDefault() ?? ".");
var apiRoot = Path.Combine(repo, "QLThiTN", "QLThiTN.API");
var config = new ConfigurationBuilder().SetBasePath(apiRoot).AddJsonFile("appsettings.json").Build();
var databaseName = "QLThiTN_SecurityChecks_" + Guid.NewGuid().ToString("N");
var connection = new SqlConnectionStringBuilder(config.GetConnectionString("ThiTracNghiem"))
{ InitialCatalog = databaseName, ConnectTimeout = 15 };
var options = new DbContextOptionsBuilder<QLThiTNDbContext>().UseSqlServer(connection.ConnectionString).Options;
await using var db = new TestDb(options);
Process? api = null;
Process? web = null;
int passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    Console.WriteLine("PASS: " + name);
    passed++;
}
TaiKhoan Account(string name, string role) => new()
{
    TenDangNhap = name, Email = name + "@example.test", MatKhau = PasswordHelper.Hash("Test-password-123!"),
    VaiTro = role, TrangThai = "HoatDong"
};
try
{
    await db.Database.EnsureCreatedAsync();
    var student = new HocVien { HoTen = "Student A", LoaiHocVien = "TuDo", TaiKhoan = Account("student-a", "HocVien") };
    var other = new HocVien { HoTen = "Student B", LoaiHocVien = "TuDo", TaiKhoan = Account("student-b", "HocVien") };
    var teacher = new GiaoVien { HoTen = "Teacher", TaiKhoan = Account("teacher", "GiaoVien") };
    var subject = new MonHoc { TenMon = "Test subject" };
    var topic = new ChuDe { TenChuDe = "Test topic", MonHoc = subject };
    var question = new CauHoi
    {
        ChuDe = topic, GiaoVien = teacher, NoiDung = "2 + 2?", LoaiCauHoi = "TracNghiem",
        MucDoKho = "De", TrangThai = "HoatDong", Diem = 1,
        DapAns = [new() { NoiDung = "4", LaDapAnDung = true, ThuTu = 0 }, new() { NoiDung = "5", ThuTu = 1 }]
    };
    var exam = new DeThi
    {
        TenDe = "Security test", MonHoc = subject, GiaoVien = teacher, LoaiDe = "ThiThu",
        HinhThucTao = "ThuCong", SoLuongCauHoi = 1, LaMienPhi = true,
        DeThi_CauHois = [new() { CauHoi = question, ThuTu = 1 }],
        MaDeThis = [new() { TenMaDe = "101" }]
    };
    var session = new DotThi
    {
        DeThi = exam, TenDotThi = "Test session", ThoiGianMoCong = DateTime.Now.AddHours(-1),
        ThoiGianDongCong = DateTime.Now.AddHours(2), ThoiLuongLamBai = 45,
        TrangThai = "DangMo", PhamVi = "ToanTruong", SoLanThiToiDa = 5, CongBoDiemSom = false
    };
    db.AddRange(student, other, session);
    await db.SaveChangesAsync();
    var portProbe = new TcpListener(IPAddress.Loopback, 0);
    portProbe.Start();
    var port = ((IPEndPoint)portProbe.LocalEndpoint).Port;
    portProbe.Stop();
    var start = new ProcessStartInfo("dotnet") { WorkingDirectory = apiRoot, UseShellExecute = false,
        CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
    start.ArgumentList.Add(Path.Combine(apiRoot, "bin", "Debug", "net10.0", "QLThiTN.API.dll"));
    start.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{port}";
    start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
    start.Environment["ConnectionStrings__ThiTracNghiem"] = connection.ConnectionString;
    start.Environment["Logging__LogLevel__Default"] = "Error";
    api = Process.Start(start)!;
    var output = api.StandardOutput.ReadToEndAsync();
    var errors = api.StandardError.ReadToEndAsync();
    using var anonymous = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}"), Timeout = TimeSpan.FromSeconds(20) };
    bool ready = false;
    for (int i = 0; i < 80 && !api.HasExited; i++)
    {
        try { ready = (await anonymous.GetAsync("/api/monhoc")).IsSuccessStatusCode; }
        catch (HttpRequestException) { }
        if (ready) break;
        await Task.Delay(250);
    }
    if (!ready) throw new Exception("API did not start." + (api.HasExited ? await errors : ""));
    async Task<HttpClient> Login(string name)
    {
        var result = await anonymous.PostAsJsonAsync("/api/auth/login", new { Username = name, Password = "Test-password-123!" });
        result.EnsureSuccessStatusCode();
        var user = (await result.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        Check(!string.IsNullOrEmpty(user.AccessToken), "Login issues access token: " + name);
        var client = new HttpClient { BaseAddress = anonymous.BaseAddress };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
        return client;
    }
    using var a = await Login("student-a");
    using var b = await Login("student-b");
    Check((await anonymous.GetAsync($"/api/bailam/history/{student.HocVienID}")).StatusCode == HttpStatusCode.Unauthorized, "Anonymous history denied");
    Check((await a.GetAsync($"/api/auth/profile/{other.TaiKhoanID}")).StatusCode == HttpStatusCode.Forbidden, "Other profile denied");
    Check((await a.PutAsJsonAsync("/api/auth/profile", new { TaiKhoanID = other.TaiKhoanID, HoTen = "Changed", Email = "changed@example.test" })).StatusCode == HttpStatusCode.Forbidden, "Other profile update denied");
    Check((await a.GetAsync($"/api/bailam/history/{other.HocVienID}")).StatusCode == HttpStatusCode.Forbidden, "Other history denied");
    Check((await a.GetAsync($"/api/lichhoc/hocvien/{other.HocVienID}")).StatusCode == HttpStatusCode.Forbidden, "Other learning schedule denied");
    Check((await a.GetAsync("/api/cauhoi")).StatusCode == HttpStatusCode.Forbidden, "Student cannot read answer bank");
    Check((await a.PostAsJsonAsync("/api/monhoc", "Unauthorized subject")).StatusCode == HttpStatusCode.Forbidden, "Student cannot mutate administration data");
    var prefix = $"/api/dethi/{exam.DeThiID}";
    Check((await a.PostAsJsonAsync(prefix + "/batdau", new { HocVienID = other.HocVienID })).StatusCode == HttpStatusCode.Forbidden, "Forged student identity denied");
    Check((await a.PostAsJsonAsync(prefix + "/nopbai", new SubmitExamRequestDto { HocVienID = student.HocVienID, HetGio = true })).StatusCode == HttpStatusCode.BadRequest, "Direct submission without attempt denied");
    async Task<BatDauResponseDto> Begin()
    {
        using var response = await a.PostAsJsonAsync(prefix + "/batdau", new { HocVienID = student.HocVienID });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BatDauResponseDto>())!;
    }
    var begins = await Task.WhenAll(Begin(), Begin());
    var attempt = begins[0];
    Check(begins[0].BaiLamID == begins[1].BaiLamID, "Concurrent starts share one attempt");
    Check((await a.GetAsync(prefix + $"/cauhoi?maDeId={attempt.MaDeID + 999}")).StatusCode == HttpStatusCode.Forbidden, "Unassigned exam variant denied");
    Check((await a.GetAsync(prefix + $"/cauhoi?maDeId={attempt.MaDeID}")).IsSuccessStatusCode, "Assigned questions available");
    List<SubmitAnswerDto> Correct() => [new() { CauHoiID = question.CauHoiID, DapAnID = question.DapAns[0].DapAnID }];
    List<SubmitAnswerDto> Wrong() => [new() { CauHoiID = question.CauHoiID, DapAnID = question.DapAns[1].DapAnID }];
    async Task<HttpResponseMessage> Save(List<SubmitAnswerDto> answers) => await a.PostAsJsonAsync(prefix + "/luutam", new LuuTamRequestDto
        { BaiLamID = attempt.BaiLamID, HocVienID = student.HocVienID, Answers = answers });
    Check((await Save(Correct())).IsSuccessStatusCode, "Save answers before deadline");
    Check((await Begin()).SavedAnswers.Single().DapAnID == question.DapAns[0].DapAnID, "Resume restores saved answer");
    var wrongRoute = await a.PostAsJsonAsync($"/api/dethi/{exam.DeThiID + 999}/luutam", new LuuTamRequestDto
        { BaiLamID = attempt.BaiLamID, HocVienID = student.HocVienID, Answers = Wrong() });
    Check(wrongRoute.StatusCode == HttpStatusCode.BadRequest, "Autosave rejects mismatched exam route");
    await db.BaiLams.Where(x => x.BaiLamID == attempt.BaiLamID).ExecuteUpdateAsync(s => s.SetProperty(x => x.ThoiGianBatDau, DateTime.Now.AddHours(-1)));
    Check((await Save(Wrong())).StatusCode == HttpStatusCode.Conflict, "Late autosave denied");
    async Task<SubmitExamResultDto> Submit() => await (await a.PostAsJsonAsync(prefix + "/nopbai", new SubmitExamRequestDto
    { BaiLamID = attempt.BaiLamID, HocVienID = student.HocVienID, Answers = Wrong(), HetGio = false })).Content.ReadFromJsonAsync<SubmitExamResultDto>() ?? throw new Exception("No result");
    var submitted = await Task.WhenAll(Submit(), Submit());
    Check(submitted.All(x => x.BaiLamID == attempt.BaiLamID), "Concurrent submissions return same attempt");
    Check(submitted.All(x => !x.ScorePublished && x.Score == 0 && x.Results.Count == 0), "Submission does not leak unpublished marks");
    var saved = await db.BaiLams.AsNoTracking().SingleAsync(x => x.BaiLamID == attempt.BaiLamID);
    Check(saved.TongDiem == 10 && saved.TrangThai == "HetGio", "Late submit grades saved snapshot, ignores new answers and client timeout flag");
    Check((await b.GetAsync($"/api/bailam/{attempt.BaiLamID}")).StatusCode == HttpStatusCode.NotFound, "Other student's attempt denied");
    var detail = await a.GetFromJsonAsync<SubmitExamResultDto>($"/api/bailam/{attempt.BaiLamID}");
    Check(detail!.Score == 0 && detail.Results.Count == 0 && !detail.ScorePublished, "Detail hides unpublished breakdown");
    var stats = await a.GetFromJsonAsync<StudentStatsDto>($"/api/bailam/stats/{student.HocVienID}");
    Check(stats!.AverageScore == 0 && stats.HighestScore == 0, "Personal aggregates hide unpublished scores");
    var history = await a.GetFromJsonAsync<List<BaiLamHistoryItemDto>>($"/api/bailam/history/{student.HocVienID}");
    Check(history!.Single().TongDiem == 0 && !history!.Single().ScorePublished, "History hides unpublished score");
    var publicStats = await anonymous.GetFromJsonAsync<ExamStatsDto>("/api/dethi/stats");
    Check(publicStats!.TheoDe.Single().DiemTrungBinh == null, "Public aggregates hide unpublished scores");
    await db.DotThis.Where(x => x.DotThiID == session.DotThiID).ExecuteUpdateAsync(s => s.SetProperty(x => x.CongBoDiemSom, true));
    detail = await a.GetFromJsonAsync<SubmitExamResultDto>($"/api/bailam/{attempt.BaiLamID}");
    Check(detail!.ScorePublished && detail.Score == 10 && detail.Results.Single().IsCorrect, "Published result reveals correct marks");

    // Exercise the actual MVC/session/HTTP-client integration, including CSRF.
    portProbe.Start();
    var webPort = ((IPEndPoint)portProbe.LocalEndpoint).Port;
    portProbe.Stop();
    var webRoot = Path.Combine(repo, "QLThiTN", "QLThiTN.Web");
    var webStart = new ProcessStartInfo("dotnet") { WorkingDirectory = webRoot, UseShellExecute = false,
        CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
    webStart.ArgumentList.Add(Path.Combine(webRoot, "bin", "Debug", "net10.0", "QLThiTN.Web.dll"));
    webStart.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{webPort}";
    webStart.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
    webStart.Environment["ApiSettings__BaseUrl"] = anonymous.BaseAddress!.ToString();
    webStart.Environment["Logging__LogLevel__Default"] = "Error";
    web = Process.Start(webStart)!;
    var webOutput = web.StandardOutput.ReadToEndAsync();
    var webErrors = web.StandardError.ReadToEndAsync();
    using var browser = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() })
        { BaseAddress = new Uri($"http://127.0.0.1:{webPort}") };
    ready = false;
    for (int i = 0; i < 80 && !web.HasExited; i++)
    {
        try { ready = (await browser.GetAsync("/Account/Login")).IsSuccessStatusCode; }
        catch (HttpRequestException) { }
        if (ready) break;
        await Task.Delay(250);
    }
    if (!ready) throw new Exception("Web did not start.");
    static string Csrf(string html)
    {
        var match = System.Text.RegularExpressions.Regex.Match(html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        if (!match.Success) throw new Exception("Antiforgery token missing.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
    var loginHtml = await browser.GetStringAsync("/Account/Login");
    var loginResponse = await browser.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["Username"] = "student-a", ["Password"] = "Test-password-123!", ["__RequestVerificationToken"] = Csrf(loginHtml)
    }));
    Check(loginResponse.StatusCode == HttpStatusCode.Redirect, "Web login creates authenticated session");
    Check((await browser.GetAsync("/Account/Profile")).IsSuccessStatusCode, "Web forwards access token for profile");
    Check((await browser.GetAsync("/Exam/History")).IsSuccessStatusCode, "Web loads authenticated exam history");
    var takingResponse = await browser.GetAsync($"/Exam/Taking/{exam.DeThiID}");
    var takingHtml = await takingResponse.Content.ReadAsStringAsync();
    Check(takingResponse.IsSuccessStatusCode && takingHtml.Contains("exam-autosave.js"), "Web opens exam with autosave client");
    var attemptMatch = System.Text.RegularExpressions.Regex.Match(takingHtml, "name=\"BaiLamID\" value=\"(\\d+)\"");
    var webAttemptId = int.Parse(attemptMatch.Groups[1].Value);
    var payload = new { ExamId = exam.DeThiID, BaiLamID = webAttemptId,
        Answers = new[] { new { QuestionId = question.CauHoiID, SelectedOptionId = question.DapAns[0].DapAnID } } };
    Check((await browser.PostAsJsonAsync("/Exam/LuuTam", payload)).StatusCode == HttpStatusCode.BadRequest, "Web autosave rejects missing antiforgery token");
    using var saveRequest = new HttpRequestMessage(HttpMethod.Post, "/Exam/LuuTam") { Content = JsonContent.Create(payload) };
    saveRequest.Headers.Add("RequestVerificationToken", Csrf(takingHtml));
    using var savedResponse = await browser.SendAsync(saveRequest);
    var savedJson = await savedResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
    Check(savedResponse.IsSuccessStatusCode && savedJson.GetProperty("success").GetBoolean(), "Web autosave accepts session plus antiforgery token");
    await db.BaiLams.Where(x => x.BaiLamID == webAttemptId).ExecuteUpdateAsync(s => s.SetProperty(x => x.ThoiGianBatDau, DateTime.Now.AddHours(-1)));
    var resumedResponse = await browser.GetAsync($"/Exam/Taking/{exam.DeThiID}");
    Check(resumedResponse.StatusCode == HttpStatusCode.Redirect && resumedResponse.Headers.Location!.ToString().Contains("Result"), "Web finalizes expired resumed attempt");

    await db.TaiKhoans.Where(x => x.TaiKhoanID == student.TaiKhoanID).ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, "BiKhoa"));
    Check((await a.GetAsync($"/api/auth/profile/{student.TaiKhoanID}")).StatusCode == HttpStatusCode.Unauthorized, "Locking account revokes existing token");
    Console.WriteLine($"All {passed} security/integrity checks passed.");
}
finally
{
    if (web is { HasExited: false }) { web.Kill(entireProcessTree: true); await web.WaitForExitAsync(); }
    web?.Dispose();
    if (api is { HasExited: false }) { api.Kill(entireProcessTree: true); await api.WaitForExitAsync(); }
    api?.Dispose();
    // Only the unique database created by this test may be deleted.
    if (new SqlConnectionStringBuilder(db.Database.GetConnectionString()).InitialCatalog != databaseName)
        throw new InvalidOperationException("Refusing to clean up unexpected database.");
    await db.Database.EnsureDeletedAsync();
}

sealed class TestDb(DbContextOptions<QLThiTNDbContext> options) : QLThiTNDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Existing SQL deployment scripts use explicit FK deletion rules. Avoid SQL
        // Server multiple-cascade-path limitations when generating a fresh test schema.
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}
