using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("LopHoc")]
public class LopHoc
{
    [Key]
    public int LopHocID { get; set; }

    [Required, MaxLength(100)]
    public string TenLop { get; set; } = string.Empty;

    public int KhoaHocID { get; set; }

    public KhoaHoc? KhoaHoc { get; set; }
}