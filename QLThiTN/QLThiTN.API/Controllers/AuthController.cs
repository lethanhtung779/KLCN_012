using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThiTN.API.Common;
using QLThiTN.API.Data;
using QLThiTN.API.DTOs;
using QLThiTN.API.Entities;

namespace QLThiTN.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly QLThiTNDbContext _db;

    public AuthController(QLThiTNDbContext db) => _db = db;

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

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        if (await _db.TaiKhoans.AnyAsync(t => t.TenDangNhap == request.UserName))
            return Conflict(new { message = "Ten dang nhap da ton tai" });

        if (await _db.TaiKhoans.AnyAsync(t => t.Email == request.Email))
            return Conflict(new { message = "Email da duoc su dung" });

        var vaiTro = request.VaiTro switch
        {
            "GiaoVien" => "GiaoVien",
            "QuanTriVien" => "QuanTriVien",
            _ => "HocVien"
        };

        var tk = new TaiKhoan
        {
            TenDangNhap = request.UserName,
            MatKhau = PasswordHelper.Hash(request.Password),
            Email = request.Email,
            VaiTro = vaiTro,
            TrangThai = "HoatDong",
            NgayTao = DateTime.Now
        };
        _db.TaiKhoans.Add(tk);
        await _db.SaveChangesAsync();

        if (vaiTro == "GiaoVien")
        {
            _db.GiaoViens.Add(new GiaoVien { TaiKhoanID = tk.TaiKhoanID, HoTen = request.FullName });
        }
        else
        {
            _db.HocViens.Add(new HocVien
            {
                TaiKhoanID = tk.TaiKhoanID,
                HoTen = request.FullName,
                LoaiHocVien = "HocSinh",
                NgayDangKy = DateTime.Now
            });
        }
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Login), await BuildResponse(tk));
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
            GiaoVienID = gv?.GiaoVienID
        };
    }
}