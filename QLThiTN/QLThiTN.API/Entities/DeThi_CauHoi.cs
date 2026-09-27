using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("DeThi_CauHoi")]
public class DeThi_CauHoi
{
    public int DeThiID { get; set; }

    public int CauHoiID { get; set; }

    public int ThuTu { get; set; }

    public DeThi? DeThi { get; set; }

    public CauHoi? CauHoi { get; set; }
}