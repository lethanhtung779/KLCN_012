using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("ChuDe")]
public class ChuDe
{
    [Key]
    public int ChuDeID { get; set; }

    public int MonHocID { get; set; }

    [Required, MaxLength(200)]
    public string TenChuDe { get; set; } = string.Empty;

    public MonHoc? MonHoc { get; set; }

    public List<CauHoi> CauHois { get; set; } = new();
}