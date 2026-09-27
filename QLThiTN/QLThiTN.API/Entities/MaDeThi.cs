using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("MaDeThi")]
public class MaDeThi
{
    [Key]
    public int MaDeID { get; set; }

    public int DeThiID { get; set; }

    [Required, MaxLength(50)]
    public string TenMaDe { get; set; } = string.Empty;

    public DeThi? DeThi { get; set; }

    public List<MaDe_CauHoi> MaDe_CauHois { get; set; } = new();
}