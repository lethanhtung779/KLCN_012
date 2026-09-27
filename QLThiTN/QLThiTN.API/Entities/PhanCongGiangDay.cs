using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("PhanCongGiangDay")]
public class PhanCongGiangDay
{
    public int LopHocID { get; set; }

    public int MonHocID { get; set; }

    public int GiaoVienID { get; set; }

    public LopHoc? LopHoc { get; set; }

    public MonHoc? MonHoc { get; set; }

    public GiaoVien? GiaoVien { get; set; }
}