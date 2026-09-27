using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("HocVien")]
public class HocVien
{
    [Key]
    public int HocVienID { get; set; }

    public int TaiKhoanID { get; set; }

    [Required, MaxLength(100)]
    public string HoTen { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string LoaiHocVien { get; set; } = string.Empty;

    public DateTime? NgaySinh { get; set; }

    public DateTime NgayDangKy { get; set; } = DateTime.Now;

    [MaxLength(20)]
    public string? SoDienThoai { get; set; }

    public TaiKhoan? TaiKhoan { get; set; }
}