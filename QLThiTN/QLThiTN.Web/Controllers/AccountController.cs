using Microsoft.AspNetCore.Mvc;
using QLThiTN.Web.ViewModels;

namespace QLThiTN.Web.Controllers;

public class AccountController : Controller
{
    private const string SessionUser = "UserFullName";
    private const string SessionUsername = "Username";

    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetString(SessionUser) != null)
            return RedirectToAction("Index", "Home");
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // TODO: Gọi API /api/auth/login để xác thực người dùng
        if (model.Username.ToLower() == "student" && model.Password == "123456")
        {
            HttpContext.Session.SetString(SessionUser, "Nguyễn Văn An");
            HttpContext.Session.SetString(SessionUsername, model.Username);
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
        return View(model);
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
    public IActionResult Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // TODO: Gọi API /api/users để tạo tài khoản
        TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
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
            Id = 1,
            FullName = fullName,
            Username = HttpContext.Session.GetString(SessionUsername) ?? "student",
            Email = "nguyenvanan@gmail.com",
            Phone = "0987654321"
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