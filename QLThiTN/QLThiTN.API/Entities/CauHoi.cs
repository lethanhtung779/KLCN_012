using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("CauHoi")]
public class CauHoi
{
    [Key]
    public int CauHoiID { get; set; }

    public int ChuDeID { get; set; }

    public int GiaoVienID { get; set; }

    [Required]
    public string NoiDung { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string LoaiCauHoi { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string MucDoKho { get; set; } = string.Empty;

    [Column(TypeName = "decimal(5,2)")]
    public decimal Diem { get; set; } = 0.25m;

    [Required, MaxLength(20)]
    public string TrangThai { get; set; } = string.Empty;

    public DateTime NgayTao { get; set; } = DateTime.Now;

    public string? GiaiThichDapAn { get; set; }

    public ChuDe? ChuDe { get; set; }

    public GiaoVien? GiaoVien { get; set; }

    public List<DapAn> DapAns { get; set; } = new();

    public List<HinhAnh> HinhAnhs { get; set; } = new();
}