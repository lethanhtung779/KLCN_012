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
    public IActionResult Login()
    {
        if (HttpContext.Session.GetString(SessionUser) != null)
            return RedirectToAction("Index", "Home");
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
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
    public IActionResult Profile()
    {
        var fullName = HttpContext.Session.GetString(SessionUser);
        if (fullName == null)
            return RedirectToAction("Login");

        var model = new ProfileViewModel
        {
            Id = HttpContext.Session.GetInt32(SessionUserId) ?? 1,
            FullName = fullName,
            Username = HttpContext.Session.GetString(SessionUsername) ?? "student",
            Email = HttpContext.Session.GetString(SessionEmail) ?? string.Empty
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // TODO: Gọi API /api/users/{id} để cập nhật thông tin
        HttpContext.Session.SetString(SessionUser, model.FullName);
        TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
        return RedirectToAction("Profile");
    }

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}