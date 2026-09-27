using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("HocVien_Lop")]
public class HocVien_Lop
{
    public int HocVienID { get; set; }

    public int LopHocID { get; set; }

    public DateTime NgayThamGia { get; set; } = DateTime.Now;

    [Required, MaxLength(20)]
    public string TrangThai { get; set; } = string.Empty;

    public HocVien? HocVien { get; set; }

    public LopHoc? LopHoc { get; set; }
}