namespace QLThiTN.WinForms.Models;

/// <summary>Tai khoan dang nhap trong bang TaiKhoan.</summary>
public class TaiKhoan
{
    public int TaiKhoanID { get; set; }
    public string TenDangNhap { get; set; } = "";
    public string Email { get; set; } = "";
    public string VaiTro { get; set; } = "";
    public string TrangThai { get; set; } = "";
    public DateTime NgayTao { get; set; }

    // Thong tin ho so gan voi tai khoan (GiaoVien / HocVien / QuanTriVien)
    public int? ProfileID { get; set; }
    public string HoTen { get; set; } = "";
}

/// <summary>Mon hoc.</summary>
public class MonHoc
{
    public int MonHocID { get; set; }
    public string TenMon { get; set; } = "";
}

/// <summary>Khoa hoc (chuong trinh theo mon).</summary>
public class KhoaHoc
{
    public int KhoaHocID { get; set; }
    public string TenKhoaHoc { get; set; } = "";
    public int MonHocID { get; set; }
    public string TenMon { get; set; } = "";
    public DateTime? NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public string TrangThai { get; set; } = "HoatDong";
}

/// <summary>Lop hoc thuoc mot khoa hoc.</summary>
public class LopHoc
{
    public int LopHocID { get; set; }
    public string TenLop { get; set; } = "";
    public int KhoaHocID { get; set; }
    public string TenKhoaHoc { get; set; } = "";
    public string TenMon { get; set; } = "";
}

/// <summary>Mo ta hoc vien trong cay tong hop dao tao.</summary>
public class TongHopHocVienLop
{
    public string HoTen { get; set; } = "";
    public string TenDangNhap { get; set; } = "";
    public string TrangThai { get; set; } = "";
}

/// <summary>Mo ta giao vien phan cong trong cay tong hop.</summary>
public class TongHopGiaoVienLop
{
    public string TenMon { get; set; } = "";
    public string TenGiaoVien { get; set; } = "";
}

/// <summary>Lop hoc trong cay tong hop (hoc vien + giao vien).</summary>
public class TongHopLopHoc
{
    public int LopHocID { get; set; }
    public string TenLop { get; set; } = "";
    public List<TongHopGiaoVienLop> GiaoViens { get; set; } = [];
    public List<TongHopHocVienLop> HocViens { get; set; } = [];
}

/// <summary>Khoa hoc va cac lop con trong cay tong hop.</summary>
public class TongHopKhoaHoc
{
    public int KhoaHocID { get; set; }
    public string TenKhoaHoc { get; set; } = "";
    public string TenMon { get; set; } = "";
    public string TrangThai { get; set; } = "";
    public List<TongHopLopHoc> LopHocs { get; set; } = [];
}

/// <summary>Trang thai khoa hoc (khop CHECK KhoaHoc.TrangThai).</summary>
public static class TrangThaiKhoaHoc
{
    public const string HoatDong = "HoatDong";
    public const string TamDung = "TamDung";
    public const string DaKetThuc = "DaKetThuc";

    public static readonly (string Code, string Display)[] All =
    [
        (HoatDong, "Hoạt động"),
        (TamDung, "Tạm dừng"),
        (DaKetThuc, "Đã kết thúc")
    ];

    public static string Display(string code) =>
        All.FirstOrDefault(x => x.Code == code).Display ?? code;
}

/// <summary>Trang thai hoc vien trong lop (khop CHECK HocVien_Lop.TrangThai).</summary>
public static class TrangThaiHocVienLop
{
    public const string DangHoc = "DangHoc";
    public const string DaKetThuc = "DaKetThuc";
    public const string ThoiHoc = "ThoiHoc";

    public static readonly (string Code, string Display)[] All =
    [
        (DangHoc, "Đang học"),
        (DaKetThuc, "Đã kết thúc"),
        (ThoiHoc, "Thôi học")
    ];

    public static string Display(string code) =>
        All.FirstOrDefault(x => x.Code == code).Display ?? code;
}

/// <summary>Chu de thuoc mon hoc.</summary>
public class ChuDe
{
    public int ChuDeID { get; set; }
    public int MonHocID { get; set; }
    public string TenChuDe { get; set; } = "";
}

/// <summary>Loai cau hoi (khop CHECK constraint CauHoi.LoaiCauHoi).</summary>
public static class LoaiCauHoi
{
    public const string TracNghiem = "TracNghiem";
    public const string DungSai = "DungSai";
    public const string TraLoiNgan = "TraLoiNgan";

    public static string Display(string loai) => loai switch
    {
        TracNghiem => "Trắc nghiệm",
        DungSai => "Đúng/Sai",
        TraLoiNgan => "Trả lời ngắn",
        _ => loai
    };
}

/// <summary>Muc do kho (khop CHECK constraint CauHoi.MucDoKho).</summary>
public static class MucDoKho
{
    public const string ChuaXacDinh = "ChuaXacDinh";
    public const string NhanBiet = "NhanBiet";
    public const string ThongHieu = "ThongHieu";
    public const string VanDung = "VanDung";
    public const string VanDungCao = "VanDungCao";

    public static readonly (string Code, string Display)[] All =
    [
        (ChuaXacDinh, "Chưa xác định"),
        (NhanBiet, "Nhận biết"),
        (ThongHieu, "Thông hiểu"),
        (VanDung, "Vận dụng"),
        (VanDungCao, "Vận dụng cao")
    ];

    public static string Display(string code) =>
        All.FirstOrDefault(x => x.Code == code).Display ?? code;
}

/// <summary>Cau hoi + danh sach dap an.</summary>
public class CauHoi
{
    public int CauHoiID { get; set; }
    public int ChuDeID { get; set; }
    public int GiaoVienID { get; set; }
    public string NoiDung { get; set; } = "";
    public string LoaiCauHoi { get; set; } = "TracNghiem";
    public string MucDoKho { get; set; } = "ChuaXacDinh";
    public decimal Diem { get; set; } = 0.25m;
    public string TrangThai { get; set; } = "HoatDong";
    public DateTime NgayTao { get; set; }
    public string? GiaiThichDapAn { get; set; }

    // Thong tin hien thi
    public string TenMon { get; set; } = "";
    public string TenChuDe { get; set; } = "";
    public string TenGiaoVien { get; set; } = "";

    public List<DapAn> DapAns { get; set; } = new();
    public List<QuestionImage>? HinhAnhs { get; set; }
}

public class DapAn
{
    public List<QuestionImage>? HinhAnhs { get; set; }
    public int DapAnID { get; set; }
    public int CauHoiID { get; set; }
    public int ThuTu { get; set; }
    public string NoiDung { get; set; } = "";
    public bool LaDapAnDung { get; set; }
}

/// <summary>De thi.</summary>
public class DeThi
{
    public int DeThiID { get; set; }
    public int MonHocID { get; set; }
    public int GiaoVienID { get; set; }
    public string TenDe { get; set; } = "";
    public string LoaiDe { get; set; } = "ThiThu";
    public int SoLuongCauHoi { get; set; }
    public string HinhThucTao { get; set; } = "ThuCong";
    public string TenMon { get; set; } = "";
    public int SoDotThi { get; set; }
}

/// <summary>Dot thi — lich to chuc cua mot de thi.</summary>
public class DotThi
{
    public int DotThiID { get; set; }
    public int DeThiID { get; set; }
    public string TenDotThi { get; set; } = "";
    public DateTime ThoiGianMoCong { get; set; }
    public DateTime ThoiGianDongCong { get; set; }
    public int ThoiLuongLamBai { get; set; }
    public int? GioiHanSoLuong { get; set; }
    public bool CongBoDiemSom { get; set; }
    public string TrangThai { get; set; } = "DangMo";
    public string PhamVi { get; set; } = "ToanTruong";
    public int SoLanThiToiDa { get; set; } = 1;
    public string TenDe { get; set; } = "";
    public int SoDangKy { get; set; }
    public int SoBaiDaNop { get; set; }
}

/// <summary>Ket qua thi cua mot hoc vien trong mot dot thi (cho man hinh thong ke).</summary>
public class KetQuaThi
{
    public int BaiLamID { get; set; }
    public string HoTen { get; set; } = "";
    public string TenDangNhap { get; set; } = "";
    public int LanThi { get; set; }
    public decimal? TongDiem { get; set; }
    public DateTime ThoiGianBatDau { get; set; }
    public DateTime? ThoiGianNop { get; set; }
    public string TrangThai { get; set; } = "";

    public int PhutLamBai => ThoiGianNop.HasValue
        ? (int)Math.Round((ThoiGianNop.Value - ThoiGianBatDau).TotalMinutes)
        : 0;

    public string XepLoai => TongDiem switch
    {
        null => "—",
        >= 8 => "Giỏi",
        >= 6.5m => "Khá",
        >= 5 => "Trung bình",
        _ => "Yếu"
    };
}

/// <summary>Ti le dung/sai cua mot cau hoi trong cac bai thi (cho thong ke chat luong).</summary>
public class ThongKeCauHoi
{
    public int CauHoiID { get; set; }
    public int ThuTu { get; set; }
    public string NoiDung { get; set; } = "";
    public int SoLanTraLoi { get; set; }
    public int SoLanDung { get; set; }

    public double TiLeDung => SoLanTraLoi > 0 ? Math.Round(SoLanDung * 100.0 / SoLanTraLoi, 1) : 0;
    public string NoiDungNgan => NoiDung.Length <= 60 ? NoiDung : NoiDung[..60] + "…";
}

/// <summary>Ảnh đã lưu hoặc ảnh mới đang chờ người dùng bấm Lưu.</summary>
public class QuestionImage
{
    public int HinhAnhID { get; set; }
    public string DuongDan { get; set; } = "";
    public string? MoTa { get; set; }
    public byte[]? PendingPng { get; set; }
    public override string ToString() => string.IsNullOrWhiteSpace(MoTa) ? "Ảnh minh họa" : MoTa;
}
