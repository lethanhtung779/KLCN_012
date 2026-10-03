using Microsoft.AspNetCore.Mvc;
using QLThiTN.Web.Services;
using QLThiTN.Web.ViewModels;

namespace QLThiTN.Web.Controllers;

public class AccountController : Controller
{
    private const string SessionUser = "UserFullName";
    private const string SessionUsername = "Username";
    private const string SessionUserId = "UserId";
    private const string SessionUserRole = "UserRole";
    private const string SessionEmail = "UserEmail";

    private readonly ApiClient _api;

    public AccountController(ApiClient api) => _api = api;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.GetString(SessionUser) != null)
            return RedirectToAction("Index", "Home");
        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _api.LoginAsync(model.Username, model.Password);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
            return View(model);
        }

        HttpContext.Session.SetString(SessionUser, user.HoTen);
        HttpContext.Session.SetString(SessionUsername, user.TenDangNhap);
        HttpContext.Session.SetInt32(SessionUserId, user.TaiKhoanID);
        HttpContext.Session.SetString(SessionUserRole, user.VaiTro);
        HttpContext.Session.SetString(SessionEmail, user.Email);
        if (user.HocVienID.HasValue)
        {
            HttpContext.Session.SetInt32("HocVienId", user.HocVienID.Value);
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.GetString(SessionUser) != null)
            return RedirectToAction("Index", "Home");
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, message) = await _api.RegisterAsync(new ApiRegisterRequest
        {
            UserName = model.Username,
            Email = model.Email,
            FullName = model.FullName,
            Password = model.Password,
            VaiTro = "HocVien"
        });

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction("Login");
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        if (HttpContext.Session.GetString(SessionUser) == null)
            return RedirectToAction("Login");

        var userId = HttpContext.Session.GetInt32(SessionUserId) ?? 0;

        // Lay thong tin day du tu API (ngay sinh, SĐT... khong co trong session)
        var profile = await _api.GetProfileAsync(userId);
        var hocVienId = HttpContext.Session.GetInt32("HocVienId");

        var model = new ProfileViewModel
        {
            Id = userId,
            FullName = profile?.HoTen ?? HttpContext.Session.GetString(SessionUser) ?? string.Empty,
            Username = profile?.TenDangNhap ?? HttpContext.Session.GetString(SessionUsername) ?? "student",
            Email = profile?.Email ?? HttpContext.Session.GetString(SessionEmail) ?? string.Empty,
            Phone = profile?.SoDienThoai,
            NgaySinh = profile?.NgaySinh,
            NgayDangKy = profile?.NgayDangKy
        };

        if (hocVienId.HasValue)
        {
            var stats = await _api.GetStudentStatsAsync(hocVienId.Value);
            if (stats != null)
            {
                model.TotalExams = stats.TotalExams;
                model.AverageScore = stats.AverageScore;
                model.HighestScore = stats.HighestScore;
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
            return await ReloadProfileStatsAsync(model);

        var userId = HttpContext.Session.GetInt32(SessionUserId) ?? model.Id;

        var (success, message) = await _api.UpdateProfileAsync(new ApiUpdateProfileRequest
        {
            TaiKhoanID = userId,
            HoTen = model.FullName,
            Email = model.Email ?? string.Empty,
            NgaySinh = model.NgaySinh,
            SoDienThoai = model.Phone
        });

        if (success)
        {
            // Cap nhat lai session theo thong tin moi
            HttpContext.Session.SetString(SessionUser, model.FullName);
            HttpContext.Session.SetString(SessionEmail, model.Email ?? string.Empty);
            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Profile));
        }

        ModelState.AddModelError(string.Empty, message);
        return await ReloadProfileStatsAsync(model);
    }

    /// <summary>Reload thong ke cau hinh lai model khi khong cap nhat thanh cong
    /// (statistic khong nam trong form nhung view can hien).</summary>
    private async Task<IActionResult> ReloadProfileStatsAsync(ProfileViewModel model)
    {
        var hocVienId = HttpContext.Session.GetInt32("HocVienId");
        if (hocVienId.HasValue)
        {
            var stats = await _api.GetStudentStatsAsync(hocVienId.Value);
            if (stats != null)
            {
                model.TotalExams = stats.TotalExams;
                model.AverageScore = stats.AverageScore;
                model.HighestScore = stats.HighestScore;
            }
        }
        model.NgayDangKy = (await _api.GetProfileAsync(HttpContext.Session.GetInt32(SessionUserId) ?? 0))?.NgayDangKy;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var userId = HttpContext.Session.GetInt32(SessionUserId);
        if (userId == null)
            return RedirectToAction("Login");

        if (!ModelState.IsValid)
        {
            // Hien loi doi mat khau tren trang ho so
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            TempData["ErrorMessage"] = string.Join(" ", errors);
            return RedirectToAction(nameof(Profile));
        }

        var (success, message) = await _api.ChangePasswordAsync(new ApiChangePasswordRequest
        {
            TaiKhoanID = userId.Value,
            MatKhauCu = model.MatKhauCu,
            MatKhauMoi = model.MatKhauMoi
        });

        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}
