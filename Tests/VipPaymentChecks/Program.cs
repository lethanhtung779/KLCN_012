using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using QLThiTN.API.Controllers;
using QLThiTN.API.Data;
using QLThiTN.API.Entities;
using QLThiTN.API.Services;

int passed = 0;
void Check(bool value, string name)
{
    if (!value) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name); passed++;
}
var settings = new VipPaymentSettings { Enabled = true, ClientId = "test-client", ApiKey = "test-api", ChecksumKey = "test-only-secret", ReturnUrl = "https://example.test/Vip/Return", CancelUrl = "https://example.test/Vip/Cancel" };
var monthly = new VipPaymentController.CheckoutRequest("30days");
var yearly = new VipPaymentController.CheckoutRequest("1year");
Check(!new VipPaymentSettings().Ready, "Payments disabled until configured");
Check(settings.Ready, "Complete payOS configuration");
Check(!VipPaymentGateway.CanBuy(new HocVien { NguonTaiKhoan = "TrungTam" }), "Center student cannot purchase");
Check(!VipPaymentGateway.CanBuy(new HocVien { NguonTaiKhoan = "VangLai", LoaiGoi = "DongPhi" }), "Existing VIP cannot repurchase");
Check(VipPaymentGateway.CanBuy(new HocVien { NguonTaiKhoan = "VangLai" }), "Free walk-in student can purchase");
Check(VipPaymentGateway.CanBuy(new HocVien { NguonTaiKhoan = "VangLai", LoaiGoi = "DongPhi", VipExpiresUtc = DateTime.UtcNow.AddSeconds(-1) }), "Expired VIP can purchase again");
Check(!VipPaymentGateway.CanBuy(new HocVien { NguonTaiKhoan = "VangLai", LoaiGoi = "DongPhi", VipExpiresUtc = DateTime.UtcNow.AddDays(1) }), "Active subscription cannot repurchase");
Check(VipPaymentGateway.Expiry("30days", new DateTime(2026, 1, 31)) == new DateTime(2026, 3, 2), "30-day plan is exactly 30 days");
Check(VipPaymentGateway.Expiry("1year", new DateTime(2028, 2, 29)) == new DateTime(2029, 2, 28), "Year plan handles leap day");
var signedData = JsonSerializer.SerializeToElement(new { amount = 300000, orderCode = 123L, code = "00", description = "Thanh toán" });
var signature = VipPaymentGateway.Sign(signedData, settings.ChecksumKey);
Check(VipPaymentGateway.Verify(signedData, signature, settings.ChecksumKey), "Valid payOS signature");
Check(!VipPaymentGateway.Verify(signedData, signature, "wrong-key"), "Wrong checksum key rejected");
Check(!VipPaymentGateway.Verify(signedData, "bad-signature", settings.ChecksumKey), "Malformed signature rejected");
var tampered = JsonSerializer.SerializeToElement(new { amount = 1, orderCode = 123L, code = "00", description = "Thanh toán" });
Check(!VipPaymentGateway.Verify(tampered, signature, settings.ChecksumKey), "Tampered amount rejected");
using var duplicate = JsonDocument.Parse("{\"amount\":1,\"amount\":2}");
Check(!VipPaymentGateway.Verify(duplicate.RootElement, signature, settings.ChecksumKey), "Duplicate fields rejected");
var handler = new FakePayOs(settings);
var gateway = new PayOsGateway(new HttpClient(handler) { BaseAddress = new Uri("https://api-merchant.payos.vn/") }, Options.Create(settings));
var referenceData = JsonSerializer.SerializeToElement(new {
    orderCode = 123, amount = 3000, description = "VQRIO123", accountNumber = "12345678", reference = "TF230204212323",
    transactionDateTime = "2023-02-04 18:25:00", currency = "VND", paymentLinkId = "124c33293c43417ab7879e14c8d9eb18",
    code = "00", desc = "Thành công", counterAccountBankId = "", counterAccountBankName = "", counterAccountName = "",
    counterAccountNumber = "", virtualAccountName = "", virtualAccountNumber = ""
});
// Public test vector from the official payOS signature documentation, not a merchant credential.
Check(VipPaymentGateway.Verify(referenceData, "412e915d2871504ed31be63c8f62a149a4410d34c4c42affc9006ef9917eaa03",
    "1a54716c8f0efb2744fb28b6e38b25da7f67a925d98bc1c18bd8faaecadd7675"), "Official payOS signature vector");
if (args.Contains("--integration"))
{
    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
    var config = new ConfigurationBuilder().SetBasePath(Path.Combine(root, "QLThiTN/QLThiTN.API")).AddJsonFile("appsettings.json").Build();
    var databaseName = "QLThiTN_VipChecks_" + Guid.NewGuid().ToString("N");
    var connection = new SqlConnectionStringBuilder(config.GetConnectionString("ThiTracNghiem")) { InitialCatalog = databaseName, ConnectTimeout = 5 };
    var options = new DbContextOptionsBuilder<QLThiTNDbContext>().UseSqlServer(connection.ConnectionString).Options;
    await using var db = new TestDb(options);
    bool created = false;
    try
    {
        await db.Database.EnsureCreatedAsync(); created = true;
        // Exercise actual production DDL, including unique transaction index.
        await db.Database.ExecuteSqlRawAsync("DROP TABLE VipPayment");
        await db.Database.ExecuteSqlRawAsync(VipPaymentGateway.SchemaSql);
        await db.Database.ExecuteSqlRawAsync(VipPaymentGateway.SchemaSql);
        HocVien Student(string source) => new() { HoTen = source, LoaiHocVien = "TuDo", NguonTaiKhoan = source,
            TaiKhoan = new TaiKhoan { TenDangNhap = Guid.NewGuid().ToString("N"), Email = Guid.NewGuid().ToString("N") + "@example.test", MatKhau = "unused", VaiTro = "HocVien", TrangThai = "HoatDong" } };
        var walkin = Student("VangLai"); var center = Student("TrungTam"); var failed = Student("VangLai");
        db.AddRange(walkin, center, failed); await db.SaveChangesAsync();
        VipPaymentController Controller(int accountId, string? callback = null)
        {
            db.ChangeTracker.Clear();
            var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, accountId.ToString()), new Claim(ClaimTypes.Role, "HocVien")], "test")) };
            if (callback is not null) http.Request.QueryString = new QueryString(callback);
            return new VipPaymentController(db, Options.Create(settings), gateway) { ControllerContext = new ControllerContext { HttpContext = http } };
        }
        string Value(IActionResult result, string name) => JsonSerializer.SerializeToElement(((OkObjectResult)result).Value).GetProperty(name).GetString()!;
        VipPaymentController.PayOsWebhook Callback(VipPayment payment, string result = "00", long? amount = null, string transaction = "1234567")
        {
            var data = JsonSerializer.SerializeToElement(new { orderCode = payment.OrderCode, amount = amount ?? payment.Amount,
                code = result, reference = transaction, currency = "VND", paymentLinkId = payment.PaymentLinkId });
            return new("00", true, data, VipPaymentGateway.Sign(data, settings.ChecksumKey));
        }
        Check(await Controller(center.TaiKhoanID).Index() is ForbidResult, "API denies center history");
        Check(await Controller(center.TaiKhoanID).Create(monthly) is ForbidResult, "API denies center checkout");
        Check(await Controller(0).Create(monthly) is ForbidResult, "API denies missing student");
        Check(await Controller(walkin.TaiKhoanID).Create(new("invalid")) is BadRequestObjectResult, "Unknown plan rejected");
        var old = new VipPayment { HocVienID = walkin.HocVienID, Provider = "VNPay", Amount = 300000, ExpiresUtc = DateTime.UtcNow.AddMinutes(15), PaymentUrl = "https://sandbox.vnpayment.vn/old" };
        db.Add(old); await db.SaveChangesAsync();
        var first = Value(await Controller(walkin.TaiKhoanID).Create(monthly), "PaymentUrl");
        Check(first.StartsWith("https://pay.payos.vn/"), "Old VNPay link is never reused");
        Check(first == Value(await Controller(walkin.TaiKhoanID).Create(monthly), "PaymentUrl"), "Double click reuses pending order");
        Check(await Controller(walkin.TaiKhoanID).Create(yearly) is ConflictObjectResult, "Pending order prevents simultaneous second plan");
        var pending = await db.VipPayments.AsNoTracking().SingleAsync(x => x.HocVienID == walkin.HocVienID && x.Provider == "payOS");
        Check(pending.Amount == 2000 && pending.PlanId == "30days", "Monthly price fixed by server");
        Check(await Controller(0).Webhook(Callback(pending) with { Signature = "bad" }) is BadRequestObjectResult, "Invalid webhook signature rejected");
        Check(await Controller(0).Webhook(Callback(pending, amount: 1)) is BadRequestObjectResult, "Signed wrong amount rejected");
        var wrongLinkData = JsonSerializer.SerializeToElement(new { orderCode = pending.OrderCode, amount = pending.Amount, code = "00", reference = "fake", currency = "VND", paymentLinkId = Guid.NewGuid().ToString("N") });
        Check(await Controller(0).Webhook(new("00", true, wrongLinkData, VipPaymentGateway.Sign(wrongLinkData, settings.ChecksumKey))) is BadRequestObjectResult, "Wrong payment link rejected");
        var sampleData = JsonSerializer.SerializeToElement(new { orderCode = 123, amount = 3000, code = "00" });
        Check(await Controller(0).Webhook(new("00", true, sampleData, VipPaymentGateway.Sign(sampleData, settings.ChecksumKey))) is OkObjectResult, "Signed registration sample acknowledged without VIP");
        Check((await db.HocViens.AsNoTracking().SingleAsync(x => x.HocVienID == walkin.HocVienID)).LoaiGoi == "MienPhi", "Invalid IPN does not grant VIP");
        Check(await Controller(0).Webhook(Callback(pending)) is OkObjectResult, "Valid webhook accepted");
        var upgraded = await db.HocViens.AsNoTracking().SingleAsync(x => x.HocVienID == walkin.HocVienID);
        Check(upgraded.LoaiGoi == "DongPhi" && upgraded.NguonTaiKhoan == "VangLai", "VIP granted without changing account source");
        var paid = await db.VipPayments.AsNoTracking().SingleAsync(x => x.Id == pending.Id);
        Check(upgraded.VipExpiresUtc == paid.PaidUtc!.Value.AddDays(30), "Monthly entitlement expires in 30 days");
        Check(Value(await Controller(0).Webhook(Callback(pending)), "message") == "Already confirmed", "Repeated webhook idempotent");
        Check(await Controller(walkin.TaiKhoanID).Create(monthly) is BadRequestObjectResult, "API rejects VIP repurchase");
        await db.HocViens.Where(x => x.HocVienID == walkin.HocVienID).ExecuteUpdateAsync(s => s.SetProperty(x => x.VipExpiresUtc, DateTime.UtcNow.AddSeconds(-1)));
        Check((await GoiThi.LayQuyenAsync(db, walkin.HocVienID))?.LaVip == false, "Expired subscriber loses exam entitlement");
        Check((await GoiThi.LayQuyenAsync(db, center.HocVienID))?.LaVip == true, "Center VIP unaffected by subscription expiry");
        await Controller(walkin.TaiKhoanID).Create(yearly);
        var annual = await db.VipPayments.AsNoTracking().SingleAsync(x => x.HocVienID == walkin.HocVienID && x.Status == "Pending" && x.Provider == "payOS");
        Check(annual.Amount == 3000 && annual.PlanId == "1year", "Annual price fixed by server");
        Check(await Controller(0).Webhook(Callback(annual, transaction: "7654321")) is OkObjectResult, "Expired subscriber can buy annual VIP");
        var annualPaid = await db.VipPayments.AsNoTracking().SingleAsync(x => x.Id == annual.Id);
        Check(annualPaid.GrantedUntilUtc == annualPaid.PaidUtc!.Value.AddYears(1), "Annual entitlement lasts one calendar year");
        await Controller(failed.TaiKhoanID).Create(monthly);
        var cancelled = await db.VipPayments.AsNoTracking().SingleAsync(x => x.HocVienID == failed.HocVienID);
        Check(await Controller(0).Webhook(Callback(cancelled, "24")) is OkObjectResult, "Unsuccessful event ignored");
        Check((await db.HocViens.AsNoTracking().SingleAsync(x => x.HocVienID == failed.HocVienID)).LoaiGoi == "MienPhi", "Failed payment preserves free tier");
        Check((await db.VipPayments.AsNoTracking().SingleAsync(x => x.Id == cancelled.Id)).Status == "Pending", "Unsigned outer failure cannot finalize pending order");
        var retryStudent = Student("VangLai"); db.Add(retryStudent); await db.SaveChangesAsync();
        handler.LoseNextResponse = true;
        Check(await Controller(retryStudent.TaiKhoanID).Create(monthly) is ObjectResult { StatusCode: 502 }, "Timeout returns recoverable error");
        var retained = await db.VipPayments.AsNoTracking().SingleAsync(x => x.HocVienID == retryStudent.HocVienID);
        Check(string.IsNullOrEmpty(retained.PaymentUrl) && retained.OrderCode > 0, "Timeout retains order before external call");
        Check(await Controller(retryStudent.TaiKhoanID).Create(monthly) is OkObjectResult, "Retry recovers existing payOS link");
        Check(await db.VipPayments.CountAsync(x => x.HocVienID == retryStudent.HocVienID) == 1, "Retry does not create second order");
    }
    finally
    {
        if (created && new SqlConnectionStringBuilder(db.Database.GetConnectionString()).InitialCatalog == databaseName)
            await db.Database.EnsureDeletedAsync();
    }
}
Console.WriteLine($"All {passed} checks passed.");

sealed class TestDb(DbContextOptions<QLThiTNDbContext> options) : QLThiTNDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        foreach (var fk in model.Model.GetEntityTypes().SelectMany(x => x.GetForeignKeys())) fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}

sealed class FakePayOs(VipPaymentSettings settings) : HttpMessageHandler
{
    public bool LoseNextResponse { get; set; }
    private readonly Dictionary<long, (string Id, long Amount)> links = new();
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.GetValues("x-client-id").Single() != settings.ClientId || request.Headers.GetValues("x-api-key").Single() != settings.ApiKey)
            throw new Exception("Missing authentication headers");
        if (request.Method == HttpMethod.Get)
        {
            var orderCode = long.Parse(request.RequestUri!.Segments.Last());
            var link = links[orderCode];
            return new(System.Net.HttpStatusCode.OK) { Content = System.Net.Http.Json.JsonContent.Create(new { code = "00", data = new { id = link.Id, orderCode, amount = link.Amount, status = "PENDING" } }) };
        }
        using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
        var b = body.RootElement;
        var fields = JsonSerializer.SerializeToElement(new { amount = b.GetProperty("amount").GetInt64(), cancelUrl = b.GetProperty("cancelUrl").GetString(),
            description = b.GetProperty("description").GetString(), orderCode = b.GetProperty("orderCode").GetInt64(), returnUrl = b.GetProperty("returnUrl").GetString() });
        if (!VipPaymentGateway.Verify(fields, b.GetProperty("signature").GetString(), settings.ChecksumKey)) throw new Exception("Invalid create request signature");
        if (b.GetProperty("description").GetString()!.Length > 9) throw new Exception("Description too long");
        var orderNumber = b.GetProperty("orderCode").GetInt64();
        if (links.ContainsKey(orderNumber)) return new(System.Net.HttpStatusCode.OK) { Content = System.Net.Http.Json.JsonContent.Create(new { code = "231", desc = "Already exists" }) };
        var id = Guid.NewGuid().ToString("N");
        links.Add(orderNumber, (id, b.GetProperty("amount").GetInt64()));
        if (LoseNextResponse) { LoseNextResponse = false; throw new TaskCanceledException("Simulated lost response"); }
        var data = JsonSerializer.SerializeToElement(new { amount = b.GetProperty("amount").GetInt64(), orderCode = b.GetProperty("orderCode").GetInt64(),
            paymentLinkId = id, checkoutUrl = "https://pay.payos.vn/web/" + id, currency = "VND", status = "PENDING" });
        return new(System.Net.HttpStatusCode.OK) { Content = System.Net.Http.Json.JsonContent.Create(new { code = "00", data, signature = VipPaymentGateway.Sign(data, settings.ChecksumKey) }) };
    }
}
