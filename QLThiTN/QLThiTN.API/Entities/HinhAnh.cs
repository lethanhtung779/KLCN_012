using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("HinhAnh")]
public class HinhAnh
{
    [Key]
    public int HinhAnhID { get; set; }

    public int? CauHoiID { get; set; }

    public int? DapAnID { get; set; }

    [Required, MaxLength(500)]
    public string DuongDan { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? MoTa { get; set; }

    public int ThuTu { get; set; }

    public CauHoi? CauHoi { get; set; }

    public DapAn? DapAn { get; set; }
}