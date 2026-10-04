using QLThiTN.Web.Services;

namespace QLThiTN.Web.ViewModels;

/// <summary>
/// Du lieu trang "Kho de" — duyet de thi theo mon hoc, thiet ke tham khao
/// pattern trang mon hoc cua DOL THPT (chip mon + thong ke cong dong/ca nhan).
/// </summary>
public class KhoDeViewModel
{
    public List<ApiMonHoc> MonHocs { get; set; } = new();

    public int? MonHocId { get; set; }
    public string TenMon { get; set; } = string.Empty;

    public int SoDe { get; set; }
    public int SoCau { get; set; }

    // Thong ke cong dong cua mon (tu bai lam that trong DB)
    public int LuotLamBai { get; set; }
    public int BaiHoanThanh { get; set; }
    public decimal? DiemTrungBinh { get; set; }

    // Thong ke ca nhan (khi da dang nhap)
    public bool DaDangNhap { get; set; }
    public int LuotThiCuaBan { get; set; }
    public decimal? DiemTBCuaBan { get; set; }

    public List<KhoDeExamItem> DeThis { get; set; } = new();
}

/// <summary>The de cua kho de: ke thua ExamScheduleViewModel de tai dung
/// logic CTA (Vao thi / Dang ky / Xem chi tiet), them thong ke va nam.</summary>
public class KhoDeExamItem : ExamScheduleViewModel
{
    public int SoLuotThi { get; set; }
    public decimal? DiemTrungBinh { get; set; }

    /// <summary>Nam de thi (lay tu thoi gian mo cong).</summary>
    public int Nam { get; set; }
}
