using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QLThiTN.API.Common;
using QLThiTN.API.Data;
using QLThiTN.API.Entities;
using QLThiTN.API.Services;
using System.Text.Json;
using System.Security.Cryptography;

namespace QLThiTN.API.Controllers;

[ApiController, Route("api/vip-payment"), Authorize(Roles = "HocVien")]
public class VipPaymentController(QLThiTNDbContext db, IOptions<VipPaymentSettings> options, PayOsGateway gateway) : ControllerBase
{
    private readonly VipPaymentSettings settings = options.Value;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var student = await db.HocViens.SingleOrDefaultAsync(x => x.TaiKhoanID == User.AccountId());
        if (student?.NguonTaiKhoan != GoiThi.VangLai) return Forbid();
        var orders = await db.Set<VipPayment>().AsNoTracking().Where(x => x.HocVienID == student.HocVienID)
            .OrderByDescending(x => x.CreatedUtc).Take(20)
            .Select(x => new { x.Id, x.Amount, x.PlanId, x.Status, x.CreatedUtc, x.ExpiresUtc, x.PaidUtc, x.GrantedUntilUtc }).ToListAsync();
        return Ok(new { Ready = settings.Ready, Plans = VipPaymentGateway.Plans, IsVip = VipPaymentGateway.HasVip(student), student.VipExpiresUtc, Orders = orders });
    }

    public sealed record CheckoutRequest(string PlanId);

    [HttpPost]
    public async Task<IActionResult> Create(CheckoutRequest request)
    {
        // Lock the student row to serialize double clicks and payment notifications.
        await using var tx = await db.Database.BeginTransactionAsync();
        var students = await db.HocViens.FromSqlInterpolated($"SELECT * FROM HocVien WITH (UPDLOCK, ROWLOCK) WHERE TaiKhoanID={User.AccountId()}").ToListAsync();
        var student = students.SingleOrDefault();
        if (student?.NguonTaiKhoan != GoiThi.VangLai) return Forbid();
        if (!VipPaymentGateway.CanBuy(student)) return BadRequest(new { message = "Bạn đã có VIP cá nhân." });
        var plan = VipPaymentGateway.Plans.SingleOrDefault(x => x.Id == request.PlanId);
        if (plan is null) return BadRequest(new { message = "Gói VIP không hợp lệ." });
        if (!settings.Ready) return StatusCode(503, new { message = "Thanh toán chưa được cấu hình. Vui lòng quay lại sau." });
        var now = DateTime.UtcNow;
        var order = await db.Set<VipPayment>().FirstOrDefaultAsync(x => x.HocVienID == student.HocVienID && x.Provider == "payOS" && x.Status == "Pending" && x.ExpiresUtc > now);
        if (order is not null && order.PlanId != plan.Id)
            return Conflict(new { message = "Bạn có đơn của gói khác đang chờ thanh toán. Hãy hoàn tất đơn đó hoặc chờ hết hạn 15 phút trước khi đổi gói." });
        if (order is null)
        {
            order = new VipPayment { HocVienID = student.HocVienID, PlanId = plan.Id, Amount = plan.PriceVnd, CreatedUtc = now, ExpiresUtc = now.AddMinutes(15),
                OrderCode = (BitConverter.ToInt64(RandomNumberGenerator.GetBytes(8)) & 0x000fffffffffffffL) + 1000 };
            db.Add(order);
            await db.SaveChangesAsync();
        }
        await tx.CommitAsync();
        if (string.IsNullOrEmpty(order.PaymentUrl))
        {
            // Persist the order BEFORE external I/O so a timeout can be retried with the same order code.
            try
            {
                var link = await gateway.CreateAsync(order);
                // Update only link fields: a webhook may already have marked this order Paid.
                await db.Set<VipPayment>().Where(x => x.Id == order.Id).ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.PaymentLinkId, link.Id).SetProperty(x => x.PaymentUrl, link.Url));
                order.PaymentUrl = link.Url;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException or FormatException or KeyNotFoundException)
            {
                return StatusCode(502, new { message = "Chưa tạo được liên kết payOS. Kiểm tra cấu hình hoặc thử lại cùng gói; hệ thống giữ nguyên mã đơn để tránh tạo trùng." });
            }
        }
        return Ok(new { order.PaymentUrl });
    }

    public sealed record PayOsWebhook(string? Code, bool Success, JsonElement Data, string? Signature);

    [AllowAnonymous, HttpPost("webhook"), RequestSizeLimit(32768)]
    public async Task<IActionResult> Webhook([FromBody] PayOsWebhook payload)
    {
        if (!VipPaymentGateway.Verify(payload.Data, payload.Signature, settings.ChecksumKey))
            return BadRequest(new { message = "Invalid signature" });
        var data = payload.Data;
        if (!data.TryGetProperty("orderCode", out var code) || code.ValueKind != JsonValueKind.Number || !code.TryGetInt64(out var orderCode)
            || !data.TryGetProperty("amount", out var amountField) || amountField.ValueKind != JsonValueKind.Number || !amountField.TryGetInt64(out var amount))
            return BadRequest(new { message = "Invalid payment data" });
        // Only the signed data determines success; outer flags are not covered by the signature.
        if (PayOsGateway.Text(data, "code") != "00") return Ok(new { message = "Ignored unsuccessful payment" });
        var snapshot = await db.VipPayments.AsNoTracking().SingleOrDefaultAsync(x => x.Provider == "payOS" && x.OrderCode == orderCode);
        // payOS sends a signed sample while registering the webhook. Acknowledge unknown orders without granting anything.
        if (snapshot is null) return Ok(new { message = "No matching order" });
        if (amount != snapshot.Amount || PayOsGateway.Text(data, "currency") != "VND")
            return BadRequest(new { message = "Invalid amount or currency" });
        var linkId = PayOsGateway.Text(data, "paymentLinkId");
        if (!Guid.TryParseExact(linkId, "N", out _) || (snapshot.PaymentLinkId is not null && snapshot.PaymentLinkId != linkId))
            return BadRequest(new { message = "Invalid payment link" });
        await using var tx = await db.Database.BeginTransactionAsync();
        var students = await db.HocViens.FromSqlInterpolated($"SELECT * FROM HocVien WITH (UPDLOCK, ROWLOCK) WHERE HocVienID={snapshot.HocVienID}").ToListAsync();
        var student = students.SingleOrDefault();
        if (student?.NguonTaiKhoan != GoiThi.VangLai) return BadRequest(new { message = "Ineligible account" });
        var order = await db.VipPayments.SingleAsync(x => x.Id == snapshot.Id);
        if (order.Status == "Paid") return Ok(new { message = "Already confirmed" });
        if (order.Status != "Pending" || !VipPaymentGateway.Plans.Any(x => x.Id == order.PlanId))
            return BadRequest(new { message = "Invalid order state" });
        var reference = PayOsGateway.Text(data, "reference");
        if (string.IsNullOrWhiteSpace(reference) || reference.Length > 90)
            return BadRequest(new { message = "Invalid transaction" });
        var transaction = "payOS:" + reference;
        if (await db.VipPayments.AnyAsync(x => x.TransactionNo == transaction))
            return BadRequest(new { message = "Transaction already used" });
        order.TransactionNo = transaction;
        order.PaymentLinkId = linkId;
        order.PaidUtc = DateTime.UtcNow;
        var start = student.VipExpiresUtc > order.PaidUtc ? student.VipExpiresUtc.Value : order.PaidUtc.Value;
        order.GrantedUntilUtc = VipPaymentGateway.Expiry(order.PlanId, start);
        if (!VipPaymentGateway.HasVip(student) || student.VipExpiresUtc is not null)
            student.VipExpiresUtc = order.GrantedUntilUtc;
        student.LoaiGoi = GoiThi.DongPhi;
        order.Status = "Paid";
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new { message = "Confirmed" });
    }
}
