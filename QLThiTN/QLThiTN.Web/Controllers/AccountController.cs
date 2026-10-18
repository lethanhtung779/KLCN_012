using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
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
    private readonly IConfiguration _configuration;

    public AccountController(ApiClient api, IConfiguration configuration)
    {
        _api = api;
        _configuration = configuration;
    }

    private void SetUserSession(ApiLoginResult user)
    {
        HttpContext.Session.Clear();
        HttpContext.Session.SetString("ApiAccessToken", user.AccessToken);
        HttpContext.Session.SetString("ApiTokenExpiresAt", user.TokenExpiresAt.ToString("O"));
        HttpContext.Session.SetString(SessionUser, user.HoTen);
        HttpContext.Session.SetString(SessionUsername, user.TenDangNhap);
        HttpContext.Session.SetInt32(SessionUserId, user.TaiKhoanID);
        HttpContext.Session.SetString(SessionUserRole, user.VaiTro);
        HttpContext.Session.SetString(SessionEmail, user.Email);
        if (user.HocVienID.HasValue)
        {
            HttpContext.Session.SetInt32("HocVienId", user.HocVienID.Value);
            GoiSession.Luu(HttpContext.Session, user.LoaiGoi);
        }
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.GetString(SessionUser) != null)
            return RedirectToAction("Index", "Home");
        ViewBag.ReturnUrl = returnUrl;
        var model = new LoginViewModel();
        if (Request.Cookies.TryGetValue("RememberedUsername", out var remUser) && !string.IsNullOrWhiteSpace(remUser))
        {
            model.Username = remUser;
            model.RememberMe = true;
        }
        return View(model);
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

        SetUserSession(user);

        if (model.RememberMe)
        {
            Response.Cookies.Append("RememberedUsername", model.Username.Trim(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax
            });
        }
        else
        {
            Response.Cookies.Delete("RememberedUsername");
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult ExternalLogin(string provider = "Google", string? returnUrl = null)
    {
        var googleClientId = _configuration["Authentication:Google:ClientId"];
        var googleClientSecret = _configuration["Authentication:Google:ClientSecret"];
        var isConfigured = !string.IsNullOrWhiteSpace(googleClientId)
                           && !string.IsNullOrWhiteSpace(googleClientSecret)
                           && googleClientId != "YOUR_GOOGLE_CLIENT_ID";

        if (!isConfigured)
        {
            TempData["ReturnUrl"] = returnUrl;
            return RedirectToAction(nameof(GoogleConfigNotice));
        }

        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!result.Succeeded || result.Principal == null)
        {
            TempData["ErrorMessage"] = "Đăng nhập bằng Google không thành công hoặc bạn đã hủy xác thực.";
            return RedirectToAction(nameof(Login));
        }

        var email = result.Principal.FindFirstValue(ClaimTypes.Email);
        var name = result.Principal.FindFirstValue(ClaimTypes.Name)
                   ?? result.Principal.FindFirstValue(ClaimTypes.GivenName)
                   ?? email;
        var googleId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["ErrorMessage"] = "Không thể lấy thông tin email từ tài khoản Google của bạn.";
            return RedirectToAction(nameof(Login));
        }

        var user = await _api.LoginWithGoogleAsync(new ApiGoogleLoginRequest
        {
            Email = email,
            FullName = name ?? email,
            GoogleId = googleId
        });

        if (user == null)
        {
            TempData["ErrorMessage"] = "Đăng nhập Google thất bại hoặc tài khoản đã bị khóa.";
            return RedirectToAction(nameof(Login));
        }

        SetUserSession(user);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult GoogleConfigNotice()
    {
        ViewBag.ReturnUrl = TempData["ReturnUrl"] as string;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SimulateGoogleLogin(string email, string fullName, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError(string.Empty, "Vui lòng nhập địa chỉ email Google.");
            ViewBag.ReturnUrl = returnUrl;
            return View("GoogleConfigNotice");
        }

        var user = await _api.LoginWithGoogleAsync(new ApiGoogleLoginRequest
        {
            Email = email.Trim(),
            FullName = string.IsNullOrWhiteSpace(fullName) ? email.Split('@')[0] : fullName.Trim(),
            GoogleId = "simulated_" + Guid.NewGuid().ToString("N")[..8]
        });

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Đăng nhập thử nghiệm thất bại hoặc tài khoản đã bị khóa.");
            ViewBag.ReturnUrl = returnUrl;
            return View("GoogleConfigNotice");
        }

        SetUserSession(user);

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

        var (success, message, devUrl) = await _api.RegisterAsync(new ApiRegisterRequest
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

        TempData["RegisterEmail"] = model.Email;
        TempData["RegisterMessage"] = message;
        if (!string.IsNullOrWhiteSpace(devUrl))
            TempData["DevConfirmUrl"] = devUrl;

        return RedirectToAction(nameof(RegisterCheckEmail));
    }

    [HttpGet]
    public IActionResult RegisterCheckEmail()
    {
        var email = TempData["RegisterEmail"]?.ToString();
        if (string.IsNullOrEmpty(email))
            return RedirectToAction(nameof(Register));

        ViewBag.Email = email;
        ViewBag.Message = TempData["RegisterMessage"]?.ToString();
        ViewBag.DevConfirmUrl = TempData["DevConfirmUrl"]?.ToString();
        return View();
    }

    /// <summary>Email chi mo trang xac nhan; chi POST moi goi API (tranh link cu trong Gmail bi vo hieu sau dang ky lai).</summary>
    [HttpGet]
    public IActionResult ConfirmEmail(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            ViewBag.Mode = "result";
            ViewBag.Success = false;
            ViewBag.Message = "Liên kết xác minh không hợp lệ.";
            return View();
        }

        ViewBag.Mode = "confirm";
        ViewBag.Token = token.Trim();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("ConfirmEmail")]
    public async Task<IActionResult> ConfirmEmailSubmit(string token)
    {
        ViewBag.Mode = "result";
        if (string.IsNullOrWhiteSpace(token))
        {
            ViewBag.Success = false;
            ViewBag.Message = "Liên kết xác minh không hợp lệ.";
            return View("ConfirmEmail");
        }

        var (success, message) = await _api.ConfirmEmailAsync(token.Trim());
        ViewBag.Success = success;
        ViewBag.Message = message;
        return View("ConfirmEmail");
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
            NgayDangKy = profile?.NgayDangKy,
            LoaiGoi = string.IsNullOrWhiteSpace(profile?.LoaiGoi) ? "MienPhi" : profile!.LoaiGoi
        };
        if (hocVienId.HasValue)
            GoiSession.Luu(HttpContext.Session, model.LoaiGoi);

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

        var userId = HttpContext.Session.GetInt32(SessionUserId);
        if (!userId.HasValue) return RedirectToAction(nameof(Login));

        var (success, message) = await _api.UpdateProfileAsync(new ApiUpdateProfileRequest
        {
            TaiKhoanID = userId.Value,
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

        if (success)
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Đã đổi mật khẩu. Vui lòng đăng nhập lại.";
            return RedirectToAction(nameof(Login));
        }
        TempData["ErrorMessage"] = message;
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}
