using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("LuaChonBaiLam")]
public class LuaChonBaiLam
{
    [Key]
    public int LuaChonID { get; set; }

    public int ChiTietID { get; set; }

    public int? DapAnID { get; set; }

    public string? NoiDungTraLoi { get; set; }

    public ChiTietBaiLam? ChiTietBaiLam { get; set; }

    public DapAn? DapAn { get; set; }
}