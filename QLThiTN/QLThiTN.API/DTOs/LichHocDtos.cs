namespace QLThiTN.API.DTOs;

public class LopHocSummaryDto
{
    public int LopHocID { get; set; }
    public string TenLop { get; set; } = string.Empty;
    public string TenKhoaHoc { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public string TrangThaiText { get; set; } = string.Empty;
}

public class LichHocItemDto
{
    public int LichHocID { get; set; }
    public int LopHocID { get; set; }
    public string TenLop { get; set; } = string.Empty;
    public string TenKhoaHoc { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public string? NoiDung { get; set; }
    public DateTime ThoiGianBatDau { get; set; }
    public DateTime ThoiGianKetThuc { get; set; }
    public string? DiaDiem { get; set; }
    /// <summary>GV day mon chinh cua khoa hoc (neu co phan cong).</summary>
    public string? TenGiaoVien { get; set; }
    /// <summary>Toan bo phan cong GV theo mon cua lop (hien tren tung buoi hoc).</summary>
    public string? PhanCongTomTat { get; set; }
}

public class PhanCongGiangDayItemDto
{
    public int LopHocID { get; set; }
    public int MonHocID { get; set; }
    public string TenMon { get; set; } = string.Empty;
    public string TenGiaoVien { get; set; } = string.Empty;
}

public class HocVienThoiKhoaBieuDto
{
    public List<LopHocSummaryDto> LopHocs { get; set; } = new();
    public List<LichHocItemDto> BuoiHoc { get; set; } = new();
    public List<PhanCongGiangDayItemDto> PhanCongGiangDay { get; set; } = new();
}
