using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("DangKyXacMinhEmail")]
public class DangKyXacMinhEmail
{
    [Key]
    public int DangKyID { get; set; }

    [Required, MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string MatKhauHash { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string HoTen { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string Token { get; set; } = string.Empty;

    public DateTime HetHan { get; set; }

    public bool DaSuDung { get; set; }

    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
}
