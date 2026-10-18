using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThiTN.API.Common;
using QLThiTN.API.Data;
using QLThiTN.API.DTOs;
using QLThiTN.API.Entities;
using QLThiTN.API.Services;

namespace QLThiTN.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly QLThiTNDbContext _db;
    private readonly RegistrationEmailService _registrationEmail;

    public AuthController(QLThiTNDbContext db, RegistrationEmailService registrationEmail)
    {
        _db = db;
        _registrationEmail = registrationEmail;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var tk = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.TenDangNhap == request.Username);
        if (tk is null || !PasswordHelper.Verify(request.Password, tk.MatKhau))
            return Unauthorized(new { message = "Ten dang nhap hoac mat khau khong dung" });

        if (tk.TrangThai != "HoatDong")
            return Unauthorized(new { message = "Tai khoan da bi khoa" });

        return Ok(await BuildResponse(tk));
    }

    /// <summary>Gui email xac minh; tai khoan chi duoc tao sau khi bam lien ket.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var (ok, message, devUrl) = await _registrationEmail.RequestRegistrationAsync(request);
        if (!ok)
            return Conflict(new { message });

        return Ok(new RegisterPendingResponseDto
        {
            Message = message,
            RequiresEmailConfirmation = true,
            ConfirmationUrl = devUrl
        });
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequestDto request)
    {
        var (ok, message, auth) = await _registrationEmail.ConfirmAsync(request.Token);
        if (!ok)
            return BadRequest(new { message });
        return Ok(new { message, user = auth });
    }

    [HttpGet("profile/{taiKhoanId:int}")]
    public async Task<IActionResult> GetProfile(int taiKhoanId)
    {
        var tk = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.TaiKhoanID == taiKhoanId);
        if (tk is null) return NotFound();

        var hv = await _db.HocViens.FirstOrDefaultAsync(h => h.TaiKhoanID == taiKhoanId);
        var gv = await _db.GiaoViens.FirstOrDefaultAsync(g => g.TaiKhoanID == taiKhoanId);

        return Ok(new ProfileResponseDto
        {
            TaiKhoanID = tk.TaiKhoanID,
            TenDangNhap = tk.TenDangNhap,
            HoTen = gv?.HoTen ?? hv?.HoTen ?? tk.TenDangNhap,
            Email = tk.Email,
            VaiTro = tk.VaiTro,
            NgaySinh = hv?.NgaySinh,
            SoDienThoai = hv?.SoDienThoai,
            NgayDangKy = hv?.NgayDangKy ?? tk.NgayTao,
            HocVienID = hv?.HocVienID,
            LoaiGoi = hv?.LoaiGoi ?? string.Empty
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        var tk = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.TaiKhoanID == request.TaiKhoanID);
        if (tk is null) return NotFound(new { message = "Khong tim thay tai khoan" });

        if (await _db.TaiKhoans.AnyAsync(t => t.Email == request.Email && t.TaiKhoanID != request.TaiKhoanID))
            return Conflict(new { message = "Email da duoc su dung" });

        tk.Email = request.Email;

        var hv = await _db.HocViens.FirstOrDefaultAsync(h => h.TaiKhoanID == request.TaiKhoanID);
        if (hv is not null)
        {
            hv.HoTen = request.HoTen;
            hv.NgaySinh = request.NgaySinh;
            hv.SoDienThoai = request.SoDienThoai;
        }
        else
        {
            var gv = await _db.GiaoViens.FirstOrDefaultAsync(g => g.TaiKhoanID == request.TaiKhoanID);
            if (gv is not null) gv.HoTen = request.HoTen;
        }

        await _db.SaveChangesAsync();
        return Ok(await BuildResponse(tk));
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        var tk = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.TaiKhoanID == request.TaiKhoanID);
        if (tk is null) return NotFound(new { message = "Khong tim thay tai khoan" });

        if (!PasswordHelper.Verify(request.MatKhauCu, tk.MatKhau))
            return BadRequest(new { message = "Mat khau hien tai khong dung" });

        tk.MatKhau = PasswordHelper.Hash(request.MatKhauMoi);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Doi mat khau thanh cong" });
    }

    private async Task<AuthResponseDto> BuildResponse(TaiKhoan tk)
    {
        var gv = await _db.GiaoViens.FirstOrDefaultAsync(g => g.TaiKhoanID == tk.TaiKhoanID);
        var hv = await _db.HocViens.FirstOrDefaultAsync(h => h.TaiKhoanID == tk.TaiKhoanID);

        return new AuthResponseDto
        {
            TaiKhoanID = tk.TaiKhoanID,
            TenDangNhap = tk.TenDangNhap,
            Email = tk.Email,
            VaiTro = tk.VaiTro,
            HoTen = gv?.HoTen ?? hv?.HoTen ?? tk.TenDangNhap,
            HocVienID = hv?.HocVienID,
            GiaoVienID = gv?.GiaoVienID,
            LoaiGoi = hv?.LoaiGoi ?? string.Empty
        };
    }
}