using System.ComponentModel.DataAnnotations;

namespace QLThiTN.API.DTOs;

public class LoginRequestDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequestDto
{
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string VaiTro { get; set; } = "HocVien";
}

public class AuthResponseDto
{
    public int TaiKhoanID { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string VaiTro { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public int? HocVienID { get; set; }
    public int? GiaoVienID { get; set; }
}

public class UpdateProfileRequestDto
{
    [Required]
    public int TaiKhoanID { get; set; }

    [Required]
    public string HoTen { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public DateTime? NgaySinh { get; set; }

    public string? SoDienThoai { get; set; }
}

public class ProfileResponseDto
{
    public int TaiKhoanID { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string VaiTro { get; set; } = string.Empty;
    public DateTime? NgaySinh { get; set; }
    public string? SoDienThoai { get; set; }
    public DateTime? NgayDangKy { get; set; }
    public int? HocVienID { get; set; }
}

public class ChangePasswordRequestDto
{
    [Required]
    public int TaiKhoanID { get; set; }

    [Required]
    public string MatKhauCu { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string MatKhauMoi { get; set; } = string.Empty;
}