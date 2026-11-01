using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLThiTN.API.Entities;

[Table("HocVien")]
public class HocVien
{
    [Key]
    public int HocVienID { get; set; }

    public int TaiKhoanID { get; set; }

    [Required, MaxLength(100)]
    public string HoTen { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string LoaiHocVien { get; set; } = string.Empty;

    /// <summary>Gói VIP cá nhân của tài khoản vãng lai; tài khoản trung tâm luôn có VIP trung tâm.</summary>
    [Required, MaxLength(20)]
    public string LoaiGoi { get; set; } = "MienPhi";

    /// <summary>
    /// Nguồn tài khoản cố định: VangLai do người dùng đăng ký Web, TrungTam do quản trị viên tạo.
    /// Việc xếp lớp không được phép thay đổi giá trị này.
    /// </summary>
    [Required, MaxLength(20)]
    public string NguonTaiKhoan { get; set; } = "VangLai";

    public DateTime? NgaySinh { get; set; }

    public DateTime NgayDangKy { get; set; } = DateTime.Now;

    [MaxLength(20)]
    public string? SoDienThoai { get; set; }

    public TaiKhoan? TaiKhoan { get; set; }
}
