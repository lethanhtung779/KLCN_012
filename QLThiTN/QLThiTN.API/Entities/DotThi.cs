using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("DotThi")]
public class DotThi
{
    [Key]
    public int DotThiID { get; set; }

    public int DeThiID { get; set; }

    [Required, MaxLength(200)]
    public string TenDotThi { get; set; } = string.Empty;

    public DateTime ThoiGianMoCong { get; set; }

    public DateTime ThoiGianDongCong { get; set; }

    public int ThoiLuongLamBai { get; set; }

    public int? GioiHanSoLuong { get; set; }

    public bool CongBoDiemSom { get; set; }

    [Required, MaxLength(20)]
    public string TrangThai { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string PhamVi { get; set; } = string.Empty;

    public int SoLanThiToiDa { get; set; } = 1;

    public DeThi? DeThi { get; set; }
}