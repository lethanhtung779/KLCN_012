using Microsoft.AspNetCore.Mvc;
using QLThiTN.Web.Services;

namespace QLThiTN.Web.Controllers;

public class VipController(ApiClient api) : Controller
{
    public async Task<IActionResult> Index(bool returned = false)
    {
        if (HttpContext.Session.GetInt32("UserId") is null)
            return RedirectToAction("Login", "Account", new { returnUrl = "/Vip" });
        if (HttpContext.Session.GetString("UserRole") != "HocVien") return StatusCode(403);
        var model = await api.GetVipPaymentAsync();
        if (model is null) return StatusCode(403);
        await GoiSession.RefreshAsync(HttpContext, api);
        ViewBag.Returned = returned;
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(string planId)
    {
        if (HttpContext.Session.GetInt32("UserId") is null) return RedirectToAction("Login", "Account");
        if (HttpContext.Session.GetString("UserRole") != "HocVien") return StatusCode(403);
        var (url, error) = await api.CreateVipPaymentAsync(planId);
        if (url is not null && Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == "https") return Redirect(url);
        TempData["ErrorMessage"] = error ?? "Chưa tạo được liên kết thanh toán.";
        return RedirectToAction(nameof(Index));
    }

    // Browser redirects never activate VIP. Only the API's verified webhook can do that.
    [HttpGet]
    public IActionResult Return() => RedirectToAction(nameof(Index), new { returned = true });

    [HttpGet]
    public IActionResult Cancel()
    {
        TempData["ErrorMessage"] = "Bạn đã quay lại từ trang thanh toán. Nếu chưa chuyển khoản, đơn sẽ hết hạn sau 15 phút. Nếu đã chuyển khoản, hãy chờ xác nhận.";
        return RedirectToAction(nameof(Index));
    }
}
