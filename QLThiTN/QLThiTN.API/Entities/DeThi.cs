using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("DeThi")]
public class DeThi
{
    [Key]
    public int DeThiID { get; set; }

    public int MonHocID { get; set; }

    public int GiaoVienID { get; set; }

    [Required, MaxLength(200)]
    public string TenDe { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string LoaiDe { get; set; } = string.Empty;

    public int SoLuongCauHoi { get; set; }

    [Required, MaxLength(50)]
    public string HinhThucTao { get; set; } = string.Empty;

    public MonHoc? MonHoc { get; set; }

    public GiaoVien? GiaoVien { get; set; }

    public List<DeThi_CauHoi> DeThi_CauHois { get; set; } = new();

    public List<MaDeThi> MaDeThis { get; set; } = new();

    public List<DotThi> DotThis { get; set; } = new();
}