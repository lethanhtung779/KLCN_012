using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("GiaoVien")]
public class GiaoVien
{
    [Key]
    public int GiaoVienID { get; set; }

    public int TaiKhoanID { get; set; }

    [Required, MaxLength(100)]
    public string HoTen { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? SoDienThoai { get; set; }

    public TaiKhoan? TaiKhoan { get; set; }

    public List<CauHoi> CauHois { get; set; } = new();
}