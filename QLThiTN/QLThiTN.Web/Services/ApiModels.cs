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
    public List<ApiCauTraLoi> Answers { get; set; } = new();
}

public class ApiCauTraLoi
{
    public int CauHoiID { get; set; }
    public int? DapAnID { get; set; }
}

public class ApiKetQua
{
    public int DeThiID { get; set; }
    public decimal Score { get; set; }
    public int Total { get; set; }
    public int Correct { get; set; }
    public int Wrong { get; set; }
    public List<ApiKetQuaCau> Results { get; set; } = new();
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