using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("MaDe_CauHoi")]
public class MaDe_CauHoi
{
    public int MaDeID { get; set; }

    public int CauHoiID { get; set; }

    public int ThuTu { get; set; }

    public MaDeThi? MaDeThi { get; set; }

    public CauHoi? CauHoi { get; set; }
}