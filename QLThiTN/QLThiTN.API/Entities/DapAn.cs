using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("DapAn")]
public class DapAn
{
    [Key]
    public int DapAnID { get; set; }

    public int CauHoiID { get; set; }

    public int ThuTu { get; set; }

    [Required]
    public string NoiDung { get; set; } = string.Empty;

    public bool LaDapAnDung { get; set; }

    public string? GiaiThich { get; set; }

    public CauHoi? CauHoi { get; set; }

    public List<HinhAnh> HinhAnhs { get; set; } = new();
}