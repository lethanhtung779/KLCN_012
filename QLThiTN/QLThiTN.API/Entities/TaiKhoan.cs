using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("TaiKhoan")]
public class TaiKhoan
{
    [Key]
    public int TaiKhoanID { get; set; }

    [Required, MaxLength(50)]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string MatKhau { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string VaiTro { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string TrangThai { get; set; } = string.Empty;

    public DateTime NgayTao { get; set; } = DateTime.Now;
}