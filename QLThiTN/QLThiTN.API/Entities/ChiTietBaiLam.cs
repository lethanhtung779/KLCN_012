using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("ChiTietBaiLam")]
public class ChiTietBaiLam
{
    [Key]
    public int ChiTietID { get; set; }

    public int BaiLamID { get; set; }

    public int CauHoiID { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? DiemDatDuoc { get; set; }

    public BaiLam? BaiLam { get; set; }

    public CauHoi? CauHoi { get; set; }

    public List<LuaChonBaiLam> LuaChonBaiLams { get; set; } = new();
}