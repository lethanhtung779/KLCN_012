using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("BaiLam")]
public class BaiLam
{
    [Key]
    public int BaiLamID { get; set; }

    public int HocVienID { get; set; }

    public int DotThiID { get; set; }

    public int MaDeID { get; set; }

    public int LanThi { get; set; }

    public DateTime ThoiGianBatDau { get; set; }

    public DateTime? ThoiGianNop { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? TongDiem { get; set; }

    [Required, MaxLength(20)]
    public string TrangThai { get; set; } = string.Empty;

    public HocVien? HocVien { get; set; }

    public DotThi? DotThi { get; set; }

    public MaDeThi? MaDeThi { get; set; }
}