namespace QLThiTN.API.DTOs;

public class DotThiDto
{
    public int DotThiID { get; set; }
    public int DeThiID { get; set; }
    public string TenDotThi { get; set; } = string.Empty;
    public string TenDe { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public DateTime ThoiGianMoCong { get; set; }
    public DateTime ThoiGianDongCong { get; set; }
    public int ThoiLuongLamBai { get; set; }
    public int? GioiHanSoLuong { get; set; }
    public bool CongBoDiemSom { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public string PhamVi { get; set; } = string.Empty;
    public int SoLanThiToiDa { get; set; }
}