namespace QLThiTN.Web.Services;

public class ApiDeThi
{
    public int DeThiID { get; set; }
    public string TenDe { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public string LoaiDe { get; set; } = string.Empty;
    public string LoaiDeText { get; set; } = string.Empty;
    public int SoLuongCauHoi { get; set; }
    public string HinhThucTao { get; set; } = string.Empty;
    public int? DotThiID { get; set; }
    public string TenDotThi { get; set; } = string.Empty;
    public DateTime? ThoiGianMoCong { get; set; }
    public DateTime? ThoiGianDongCong { get; set; }
    public int? ThoiLuongLamBai { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public class ApiHinhAnh
{
    public int HinhAnhID { get; set; }
    public int ThuTu { get; set; }
    public string Url { get; set; } = string.Empty;
    public string MoTa { get; set; } = string.Empty;
}

public class ApiCauHoi
{
    public int CauHoiID { get; set; }
    public int OrderIndex { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public string LoaiCauHoi { get; set; } = string.Empty;
    public string LoaiCauHoiText { get; set; } = string.Empty;
    public List<ApiHinhAnh> HinhAnh { get; set; } = new();
    public List<ApiDapAnBaiLam> Options { get; set; } = new();
}

public class ApiDapAnBaiLam
{
    public int DapAnID { get; set; }
    public int ThuTu { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public List<ApiHinhAnh> HinhAnh { get; set; } = new();
}

public class ApiNopBaiRequest
{
    public int DeThiID { get; set; }
    public int? HocVienID { get; set; }

    /// <summary>ID bai lam DangLam tao boi batdau; nopbai se cap nhat thay vi tao moi.</summary>
    public int? BaiLamID { get; set; }

    /// <summary>true = tu dong nop khi het gio (TrangThai = HetGio).</summary>
    public bool HetGio { get; set; }

    public DateTime? ThoiGianBatDau { get; set; }
    public List<ApiCauTraLoi> Answers { get; set; } = new();
}

public class ApiCauTraLoi
{
    public int CauHoiID { get; set; }
    public int? DapAnID { get; set; }
    public string? NoiDungTraLoi { get; set; }
    public List<ApiYChon> YChoices { get; set; } = new();
}

/// <summary>Chon Dung/Sai cua thi sinh cho mot y cua cau DungSai.</summary>
public class ApiYChon
{
    public int DapAnID { get; set; }
    public bool? LaDung { get; set; }
}

public class ApiKetQua
{
    public int? BaiLamID { get; set; }
    public int DeThiID { get; set; }
    public string TenDe { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public int Total { get; set; }
    public int Correct { get; set; }
    public int Wrong { get; set; }
    public DateTime ThoiGianBatDau { get; set; }
    public DateTime ThoiGianNop { get; set; }
    public int DurationUsed { get; set; }
    public bool ScorePublished { get; set; } = true;
    public List<ApiKetQuaCau> Results { get; set; } = new();
}

// ---------------------------------------------------------------------------
// Bat dau / luu tam bai thi
// ---------------------------------------------------------------------------

public class ApiBatDauResult
{
    public int BaiLamID { get; set; }
    public int DotThiID { get; set; }
    public int LanThi { get; set; }
    public bool IsResume { get; set; }
    public int RemainingSeconds { get; set; }
    public DateTime ThoiGianBatDau { get; set; }
    public DateTime ThoiGianDongCong { get; set; }
    public int ThoiLuongLamBai { get; set; }
    public List<ApiSavedAnswer> SavedAnswers { get; set; } = new();
}

public class ApiSavedAnswer
{
    public int CauHoiID { get; set; }
    public int? DapAnID { get; set; }
    public string? NoiDungTraLoi { get; set; }
    public List<ApiYChon> YChoices { get; set; } = new();
}

public class ApiLuuTamRequest
{
    public int BaiLamID { get; set; }
    public int HocVienID { get; set; }
    public List<ApiCauTraLoi> Answers { get; set; } = new();
}

public class ApiLuuTamResult
{
    public DateTime LuuLuc { get; set; }
    public int SoCauDaLuu { get; set; }
}

// ---------------------------------------------------------------------------
// Dot thi & dang ky dot thi
// ---------------------------------------------------------------------------

public class ApiDotThi
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
    public int SoDangKy { get; set; }
    public int? SoChoConLai { get; set; }
    public bool DaDangKy { get; set; }
}

// ---------------------------------------------------------------------------
// Ho so ca nhan
// ---------------------------------------------------------------------------

public class ApiProfile
{
    public int TaiKhoanID { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string VaiTro { get; set; } = string.Empty;
    public DateTime? NgaySinh { get; set; }
    public string? SoDienThoai { get; set; }
    public DateTime? NgayDangKy { get; set; }
    public int? HocVienID { get; set; }
}

public class ApiUpdateProfileRequest
{
    public int TaiKhoanID { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? NgaySinh { get; set; }
    public string? SoDienThoai { get; set; }
}

public class ApiChangePasswordRequest
{
    public int TaiKhoanID { get; set; }
    public string MatKhauCu { get; set; } = string.Empty;
    public string MatKhauMoi { get; set; } = string.Empty;
}

public class ApiHistoryItem
{
    public int BaiLamID { get; set; }
    public int DeThiID { get; set; }
    public int? DotThiID { get; set; }
    public string TenDe { get; set; } = string.Empty;
    public string TenMon { get; set; } = string.Empty;
    public string LoaiDe { get; set; } = string.Empty;
    public string LoaiDeText { get; set; } = string.Empty;
    public int LanThi { get; set; }
    public DateTime ThoiGianNop { get; set; }
    public decimal TongDiem { get; set; }
    public int ThoiLuongLamBai { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public bool ScorePublished { get; set; } = true;
}

public class ApiStudentStats
{
    public int TotalExams { get; set; }
    public decimal AverageScore { get; set; }
    public decimal HighestScore { get; set; }
}

public class ApiKetQuaCau
{
    public int OrderIndex { get; set; }
    public int CauHoiID { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public string LoaiCauHoi { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int? SelectedDapAnID { get; set; }
    public int? CorrectDapAnID { get; set; }
    public string GiaiThich { get; set; } = string.Empty;
    public List<ApiHinhAnh> HinhAnh { get; set; } = new();
    public List<ApiKetQuaOption> Options { get; set; } = new();
    public List<ApiKetQuaY> YResults { get; set; } = new();
    public string? NoiDungTraLoi { get; set; }
    public string? DapAnTraLoiNgan { get; set; }
    public decimal DiemDat { get; set; }
    public decimal DiemToiDa { get; set; }
}

public class ApiKetQuaY
{
    public int DapAnID { get; set; }
    public int ThuTu { get; set; }
    public string Label { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;
    public bool DapAnDung { get; set; }
    public bool? ClientLaDung { get; set; }
    public bool IsCorrect { get; set; }
    public List<ApiHinhAnh> HinhAnh { get; set; } = new();
}

public class ApiKetQuaOption
{
    public int DapAnID { get; set; }
    public int ThuTu { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public bool IsSelected { get; set; }
    public List<ApiHinhAnh> HinhAnh { get; set; } = new();
}

public class ApiLoginResult
{
    public int TaiKhoanID { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string VaiTro { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public int? HocVienID { get; set; }
    public int? GiaoVienID { get; set; }
}

public class ApiLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ApiRegisterRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string VaiTro { get; set; } = "HocVien";
}

public class ApiMonHoc
{
    public int MonHocID { get; set; }
    public string TenMon { get; set; } = string.Empty;
    public string MoTa { get; set; } = string.Empty;
    public string HinhAnh { get; set; } = string.Empty;
}