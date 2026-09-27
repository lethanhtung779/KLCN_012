using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("GiaoVien_MonHoc")]
public class GiaoVien_MonHoc
{
    public int GiaoVienID { get; set; }

    public int MonHocID { get; set; }

    public GiaoVien? GiaoVien { get; set; }

    public MonHoc? MonHoc { get; set; }
}