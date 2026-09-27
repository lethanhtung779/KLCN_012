using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("LichHoc")]
public class LichHoc
{
    [Key]
    public int LichHocID { get; set; }

    public int LopHocID { get; set; }

    [MaxLength(200)]
    public string? NoiDung { get; set; }

    public DateTime ThoiGianBatDau { get; set; }

    public DateTime ThoiGianKetThuc { get; set; }

    [MaxLength(200)]
    public string? DiaDiem { get; set; }

    public LopHoc? LopHoc { get; set; }
}