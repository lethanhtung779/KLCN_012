namespace QLThiTN.Web.ViewModels;

public class LearningScheduleViewModel
{
    public List<MyClassViewModel> MyClasses { get; set; } = new();
    public List<LearningSessionViewModel> Sessions { get; set; } = new();
    public List<PhanCongGiangDayViewModel> PhanCongGiangDay { get; set; } = new();
    /// <summary>null = tat ca lop dang ky.</summary>
    public int? LocLopHocId { get; set; }
    public DateTime? LocTuNgay { get; set; }
    public DateTime? LocDenNgay { get; set; }
    public string? ApiError { get; set; }
}

public class MyClassViewModel
{
    public int LopHocId { get; set; }
    public string TenLop { get; set; } = string.Empty;
    public string TenKhoaHoc { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public string TrangThaiText { get; set; } = string.Empty;
}

public class LearningSessionViewModel
{
    public int LichHocId { get; set; }
    public string TenLop { get; set; } = string.Empty;
    public string TenKhoaHoc { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? DiaDiem { get; set; }
    public string? TenGiaoVien { get; set; }
    public string? PhanCongTomTat { get; set; }
    public DateTime ThoiGianBatDau { get; set; }
    public DateTime ThoiGianKetThuc { get; set; }

    public bool IsPast => ThoiGianKetThuc < DateTime.Now;
    public bool IsOngoing => ThoiGianBatDau <= DateTime.Now && ThoiGianKetThuc >= DateTime.Now;
    public bool IsUpcoming => ThoiGianBatDau > DateTime.Now;
}

public class PhanCongGiangDayViewModel
{
    public int LopHocId { get; set; }
    public string TenMon { get; set; } = string.Empty;
    public string TenGiaoVien { get; set; } = string.Empty;
}
