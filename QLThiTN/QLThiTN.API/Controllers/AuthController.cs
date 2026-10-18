using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.Extensions.Options;
using System.Security.Claims;
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

        var response = await BuildResponse(tk);
        AttachBearerToken(response, tk);
        return Ok(response);
    }

    /// <summary>Dang nhap hoac tu dong tao tai khoan hoc vien vang lai (TuDo) bang Google.</summary>
    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Email không được để trống" });

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var tk = await _db.TaiKhoans.FirstOrDefaultAsync(t => t.Email.ToLower() == normalizedEmail);

        if (tk is not null)
        {
            if (tk.TrangThai != "HoatDong")
                return Unauthorized(new { message = "Tài khoản đã bị khóa" });
        }
        else
        {
            var baseUsername = normalizedEmail.Split('@')[0];
            baseUsername = System.Text.RegularExpressions.Regex.Replace(baseUsername, @"[^a-zA-Z0-9_]", "");
            if (string.IsNullOrWhiteSpace(baseUsername))
                baseUsername = "hocvien";
            if (baseUsername.Length > 40)
                baseUsername = baseUsername[..40];

            var username = baseUsername;
            var counter = 1;
            while (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == username))
            {
                username = $"{baseUsername}_{counter++}";
            }

            var randomPassword = Guid.NewGuid().ToString("N");
            tk = new TaiKhoan
            {
                TenDangNhap = username,
                MatKhau = PasswordHelper.Hash(randomPassword),
                Email = normalizedEmail,
                VaiTro = "HocVien",
                TrangThai = "HoatDong",
                NgayTao = DateTime.Now
            };

            _db.TaiKhoans.Add(tk);
            await _db.SaveChangesAsync();

            var fullName = string.IsNullOrWhiteSpace(request.FullName) ? username : request.FullName.Trim();
            var hv = new HocVien
            {
                TaiKhoanID = tk.TaiKhoanID,
                HoTen = fullName,
                LoaiHocVien = "TuDo",
                LoaiGoi = GoiThi.MienPhi,
                NgayDangKy = DateTime.Now
            };

            _db.HocViens.Add(hv);
            await _db.SaveChangesAsync();
        }

        var response = await BuildResponse(tk);
        AttachBearerToken(response, tk);
        return Ok(response);
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

    [Authorize]
    [HttpGet("profile/{taiKhoanId:int}")]
    public async Task<IActionResult> GetProfile(int taiKhoanId)
    {
        if (taiKhoanId != User.AccountId()) return Forbid();
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

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        if (request.TaiKhoanID != User.AccountId()) return Forbid();
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

    [Authorize]
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        if (request.TaiKhoanID != User.AccountId()) return Forbid();
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

    private void AttachBearerToken(AuthResponseDto response, TaiKhoan tk)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, tk.TaiKhoanID.ToString()),
            new(ClaimTypes.Role, tk.VaiTro),
            new("PasswordStamp", Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(tk.MatKhau))))
        };
        if (response.HocVienID.HasValue) claims.Add(new("HocVienID", response.HocVienID.Value.ToString()));
        if (response.GiaoVienID.HasValue) claims.Add(new("GiaoVienID", response.GiaoVienID.Value.ToString()));
        var options = HttpContext.RequestServices.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>().Get("Bearer");
        response.TokenExpiresAt = DateTimeOffset.UtcNow.Add(options.BearerTokenExpiration);
        response.AccessToken = options.BearerTokenProtector.Protect(new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer")),
            new AuthenticationProperties { ExpiresUtc = response.TokenExpiresAt }, "Bearer"));
    }
}