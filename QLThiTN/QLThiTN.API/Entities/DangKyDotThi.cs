using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("DangKyDotThi")]
public class DangKyDotThi
{
    [Key]
    public int DangKyID { get; set; }

    public int HocVienID { get; set; }

    public int DotThiID { get; set; }

    public DateTime NgayDangKy { get; set; } = DateTime.Now;

    [Required, MaxLength(20)]
    public string TrangThai { get; set; } = string.Empty;

    public HocVien? HocVien { get; set; }

    public DotThi? DotThi { get; set; }
}