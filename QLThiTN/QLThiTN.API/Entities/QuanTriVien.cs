using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("QuanTriVien")]
public class QuanTriVien
{
    [Key]
    public int QuanTriVienID { get; set; }

    public int TaiKhoanID { get; set; }

    [Required, MaxLength(100)]
    public string HoTen { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? SoDienThoai { get; set; }

    public TaiKhoan? TaiKhoan { get; set; }
}