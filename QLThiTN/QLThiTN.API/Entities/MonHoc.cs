using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("MonHoc")]
public class MonHoc
{
    [Key]
    public int MonHocID { get; set; }

    [Required, MaxLength(100)]
    public string TenMon { get; set; } = string.Empty;

    public List<ChuDe> ChuDes { get; set; } = new();
}