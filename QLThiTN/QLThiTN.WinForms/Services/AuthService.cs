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

    /// <summary>Quan tri vien (Phong Dao tao / Ban Giam hieu) co quyen quan ly khoa hoc & lop hoc.</summary>
    public static bool CanQuanLyDaoTao => IsQuanTriVien;

    /// <summary>GiaoVienID cua nguoi dang nhap (dung khi tao cau hoi / de thi).</summary>
    public static int GiaoVienID => IsGiaoVien ? Current?.ProfileID ?? 0 : 0;

    public static string HoTen => Current?.HoTen ?? Current?.TenDangNhap ?? "";

    public static bool IsAccountActive()
    {
        var current = Current;
        if (current == null || current.TrangThai != "HoatDong") return false;
        try
        {
            var connectionString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(Database.ConnectionString)
                { ConnectTimeout = 5 };
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString.ConnectionString);
            connection.Open();
            using var command = new Microsoft.Data.SqlClient.SqlCommand(
                "SELECT TrangThai, VaiTro FROM TaiKhoan WHERE TaiKhoanID = @id", connection) { CommandTimeout = 5 };
            command.Parameters.AddWithValue("@id", current.TaiKhoanID);
            using var reader = command.ExecuteReader();
            var active = reader.Read() && reader.GetString(0) == "HoatDong" && reader.GetString(1) == current.VaiTro;
            if (!ReferenceEquals(Current, current)) return false;
            if (!active) Clear();
            return active;
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            // Stop the protected operation, but do not mislabel a temporary
            // connection failure as an expired login or destroy the session.
            throw new InvalidOperationException(
                "Không thể xác minh quyền vì không kết nối hoặc truy vấn được cơ sở dữ liệu. Vui lòng thử lại khi kết nối phục hồi.", ex);
        }
    }

    public static void YeuCauDangNhap()
    {
        if (Current == null || !IsAccountActive())
            throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn hoặc tài khoản không hoạt động.");
    }

    public static void YeuCauQuanTriVienHoacGiaoVien()
    {
        if ((!IsQuanTriVien && !IsGiaoVien) || !IsAccountActive())
            throw new UnauthorizedAccessException("Chức năng chỉ dành cho Quản trị viên hoặc Giáo viên có tài khoản đang hoạt động.");
    }

    public static void YeuCauQuanTriVien()
    {
        if (!IsQuanTriVien || !IsAccountActive())
            throw new UnauthorizedAccessException("Chức năng chỉ dành cho quản trị viên hệ thống có tài khoản đang hoạt động.");
    }
}

/// <summary>Dang nhap, doi mat khau cho phan he Quan tri & Giao vien.</summary>
public class AuthService
{
    private const string LoginSql = @"
        SELECT tk.TaiKhoanID, tk.TenDangNhap, tk.MatKhau, tk.Email, tk.VaiTro,
               tk.TrangThai, tk.NgayTao,
               COALESCE(gv.GiaoVienID, qtv.QuanTriVienID) AS ProfileID,
               COALESCE(gv.HoTen, qtv.HoTen, tk.TenDangNhap) AS HoTen
        FROM TaiKhoan tk
        LEFT JOIN GiaoVien gv ON gv.TaiKhoanID = tk.TaiKhoanID
        LEFT JOIN QuanTriVien qtv ON qtv.TaiKhoanID = tk.TaiKhoanID
        WHERE tk.TenDangNhap = @u";

    private sealed record LoginRow(TaiKhoan Account, string Password);

    /// <summary>Dang nhap bang ten dang nhap + mat khau. Chi nhan GiaoVien / QuanTriVien
    /// (thi sinh dung website, khong dang nhap phan mem nay).</summary>
    public (TaiKhoan? User, string Error) Login(string username, string password)
    {
        try
        {
            return LoginCore(username, password);
        }
        catch (Microsoft.Data.SqlClient.SqlException)
        {
            Session.Clear();
            return (null, "Không kết nối hoặc truy vấn được cơ sở dữ liệu. Kiểm tra SQL Server và cấu hình appsettings.json.");
        }
    }

    /// <summary>Dang nhap khong khoa UI thread.</summary>
    public async Task<(TaiKhoan? User, string Error)> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await LoginCoreAsync(username, password, cancellationToken);
        }
        catch (Microsoft.Data.SqlClient.SqlException)
        {
            Session.Clear();
            return (null, "Không kết nối hoặc truy vấn được cơ sở dữ liệu. Kiểm tra SQL Server và cấu hình appsettings.json.");
        }
    }

    private (TaiKhoan? User, string Error) LoginCore(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (null, "Vui lòng nhập tên đăng nhập và mật khẩu.");

        var row = Database.ReadList(LoginSql, MapLoginRow,
            p => p.AddWithValue("@u", username.Trim())).FirstOrDefault();

        if (row == null || !PasswordHelper.Verify(password, row.Password))
            return (null, "Tên đăng nhập hoặc mật khẩu không chính xác.");

        var tk = row.Account;

        if (tk.TrangThai != "HoatDong")
            return (null, "Tài khoản đã bị khóa, vui lòng liên hệ quản trị viên.");

        if (tk.VaiTro != "GiaoVien" && tk.VaiTro != "QuanTriVien")
            return (null, "Phần mềm này dành cho giáo viên và quản trị viên. Thí sinh vui lòng dùng website.");

        Session.Set(tk);
        return (tk, "");
    }

    private async Task<(TaiKhoan? User, string Error)> LoginCoreAsync(
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (null, "Vui lòng nhập tên đăng nhập và mật khẩu.");

        var row = (await Database.ReadListAsync(LoginSql, MapLoginRow,
            p => p.AddWithValue("@u", username.Trim()), cancellationToken)).FirstOrDefault();

        if (row == null || !PasswordHelper.Verify(password, row.Password))
            return (null, "Tên đăng nhập hoặc mật khẩu không chính xác.");

        var tk = row.Account;
        if (tk.TrangThai != "HoatDong")
            return (null, "Tài khoản đã bị khóa, vui lòng liên hệ quản trị viên.");
        if (tk.VaiTro != "GiaoVien" && tk.VaiTro != "QuanTriVien")
            return (null, "Phần mềm này dành cho giáo viên và quản trị viên. Thí sinh vui lòng dùng website.");

        Session.Set(tk);
        return (tk, "");
    }

    private static LoginRow MapLoginRow(Microsoft.Data.SqlClient.SqlDataReader r)
    {
        var tk = new TaiKhoan
        {
            TaiKhoanID = r.GetInt32(0),
            TenDangNhap = r.GetString(1),
            Email = r.GetString(3),
            VaiTro = r.GetString(4),
            TrangThai = r.GetString(5),
            NgayTao = r.GetDateTime(6),
            ProfileID = r.IsDBNull(7) ? 0 : r.GetInt32(7),
            HoTen = r.IsDBNull(8) ? r.GetString(1) : r.GetString(8)
        };
        return new LoginRow(tk, r.GetString(2));
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
