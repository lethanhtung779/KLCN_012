using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("DotThi_LopHoc")]
public class DotThi_LopHoc
{
    public int DotThiID { get; set; }

    public int LopHocID { get; set; }

    public DotThi? DotThi { get; set; }

    public LopHoc? LopHoc { get; set; }
}