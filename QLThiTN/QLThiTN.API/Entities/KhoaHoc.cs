using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("KhoaHoc")]
public class KhoaHoc
{
    [Key]
    public int KhoaHocID { get; set; }

    [Required, MaxLength(100)]
    public string TenKhoaHoc { get; set; } = string.Empty;

    public int MonHocID { get; set; }

    public DateTime? NgayBatDau { get; set; }

    public DateTime? NgayKetThuc { get; set; }

    [Required, MaxLength(20)]
    public string TrangThai { get; set; } = string.Empty;

    public MonHoc? MonHoc { get; set; }
}