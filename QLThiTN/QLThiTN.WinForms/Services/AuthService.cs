using QLThiTN.WinForms.Models;

namespace QLThiTN.WinForms.Services;

/// <summary>Tai khoan dang nhap hien tai cua phan mem.</summary>
public static class Session
{
    public static TaiKhoan? Current { get; private set; }

    public static void Set(TaiKhoan tk) => Current = tk;
    public static void Clear() => Current = null;

    public static bool IsQuanTriVien => Current?.VaiTro == "QuanTriVien";
    public static bool IsGiaoVien => Current?.VaiTro == "GiaoVien";

    /// <summary>GiaoVienID cua nguoi dang nhap (dung khi tao cau hoi / de thi).</summary>
    public static int GiaoVienID => Current?.ProfileID ?? 0;

    public static string HoTen => Current?.HoTen ?? Current?.TenDangNhap ?? "";
}

/// <summary>Dang nhap, doi mat khau cho phan he Quan tri & Giao vien.</summary>
public class AuthService
{
    /// <summary>Dang nhap bang ten dang nhap + mat khau. Chi nhan GiaoVien / QuanTriVien
    /// (thi sinh dung website, khong dang nhap phan mem nay).</summary>
    public (TaiKhoan? User, string Error) Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (null, "Vui lòng nhập tên đăng nhập và mật khẩu.");

        var tk = Database.ReadList(
            @"SELECT TaiKhoanID, TenDangNhap, Email, VaiTro, TrangThai, NgayTao
              FROM TaiKhoan WHERE TenDangNhap = @u",
            r => new TaiKhoan
            {
                TaiKhoanID = r.GetInt32(0),
                TenDangNhap = r.GetString(1),
                Email = r.GetString(2),
                VaiTro = r.GetString(3),
                TrangThai = r.GetString(4),
                NgayTao = r.GetDateTime(5)
            },
            p => p.AddWithValue("@u", username.Trim())
        ).FirstOrDefault();

        if (tk == null || !PasswordHelper.Verify(password, GetMatKhau(tk.TaiKhoanID)))
            return (null, "Tên đăng nhập hoặc mật khẩu không chính xác.");

        if (tk.TrangThai != "HoatDong")
            return (null, "Tài khoản đã bị khóa, vui lòng liên hệ quản trị viên.");

        if (tk.VaiTro != "GiaoVien" && tk.VaiTro != "QuanTriVien")
            return (null, "Phần mềm này dành cho giáo viên và quản trị viên. Thí sinh vui lòng dùng website.");

        // Doc ho so tuong ung de lay HoTen + ProfileID
        var profile = Database.ReadList(
            tk.VaiTro == "GiaoVien"
                ? "SELECT GiaoVienID, HoTen FROM GiaoVien WHERE TaiKhoanID = @id"
                : tk.VaiTro == "QuanTriVien"
                    ? "SELECT QuanTriVienID, HoTen FROM QuanTriVien WHERE TaiKhoanID = @id"
                    : "SELECT HocVienID, HoTen FROM HocVien WHERE TaiKhoanID = @id",
            r => (ProfileID: r.GetInt32(0), HoTen: r.GetString(1)),
            p => p.AddWithValue("@id", tk.TaiKhoanID)
        ).FirstOrDefault();

        tk.ProfileID = profile.ProfileID;
        tk.HoTen = string.IsNullOrWhiteSpace(profile.HoTen) ? tk.TenDangNhap : profile.HoTen;

        Session.Set(tk);
        return (tk, "");
    }

    private string GetMatKhau(int taiKhoanId) =>
        Database.ReadList(
            "SELECT MatKhau FROM TaiKhoan WHERE TaiKhoanID = @id",
            r => r.GetString(0),
            p => p.AddWithValue("@id", taiKhoanId)
        ).FirstOrDefault() ?? "";

    /// <summary>Doi mat khau cua nguoi dang nhap (yeu cau mat khau cu dung).</summary>
    public (bool Ok, string Error) DoiMatKhau(string matKhauCu, string matKhauMoi, string xacNhan)
    {
        if (Session.Current == null) return (false, "Chưa đăng nhập.");
        if (string.IsNullOrWhiteSpace(matKhauMoi) || matKhauMoi.Length < 6)
            return (false, "Mật khẩu mới tối thiểu 6 ký tự.");
        if (matKhauMoi != xacNhan)
            return (false, "Xác nhận mật khẩu mới không khớp.");

        if (!PasswordHelper.Verify(matKhauCu, GetMatKhau(Session.Current.TaiKhoanID)))
            return (false, "Mật khẩu hiện tại không đúng.");

        Database.Execute(
            "UPDATE TaiKhoan SET MatKhau = @p WHERE TaiKhoanID = @id",
            p => { p.AddWithValue("@p", PasswordHelper.Hash(matKhauMoi)); p.AddWithValue("@id", Session.Current.TaiKhoanID); });
        return (true, "");
    }
}
