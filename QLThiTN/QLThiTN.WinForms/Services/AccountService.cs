using System.Data;
using Microsoft.Data.SqlClient;
using QLThiTN.WinForms.Models;

namespace QLThiTN.WinForms.Services;

/// <summary>CRUD tai khoan (man hinh Quan tri vien): them/sua/xoa, khoa/mo.</summary>
public class AccountService
{
    public DataTable LayDanhSach(string? tuKhoa, string? vaiTro)
    {
        Session.YeuCauQuanTriVien();
        var sql = @"
            SELECT tk.TaiKhoanID, CONCAT('TK', FORMAT(tk.TaiKhoanID, '000000')) AS MaTaiKhoan,
                   tk.TenDangNhap, tk.Email, tk.VaiTro, tk.TrangThai,
                   COALESCE(gv.HoTen, hv.HoTen, qtv.HoTen, N'') AS HoTen,
                   COALESCE(hv.LoaiGoi, N'') AS LoaiGoi,
                   CASE
                       WHEN tk.VaiTro <> N'HocVien' THEN N'—'
                       WHEN hv.LoaiGoi = N'DongPhi' THEN N'Đóng phí'
                       ELSE N'Miễn phí'
                   END AS TrangThaiGoi,
                   tk.NgayTao
            FROM TaiKhoan tk
            LEFT JOIN GiaoVien gv ON gv.TaiKhoanID = tk.TaiKhoanID
            LEFT JOIN HocVien hv ON hv.TaiKhoanID = tk.TaiKhoanID
            LEFT JOIN QuanTriVien qtv ON qtv.TaiKhoanID = tk.TaiKhoanID
            WHERE (@tuKhoa = N'' OR tk.TenDangNhap LIKE '%' + @tuKhoa + N'%' OR tk.Email LIKE '%' + @tuKhoa + N'%'
                   OR COALESCE(gv.HoTen, hv.HoTen, qtv.HoTen, N'') LIKE '%' + @tuKhoa + N'%')
              AND (@vaiTro = N'' OR tk.VaiTro = @vaiTro)
              AND (@id = 0 OR tk.TaiKhoanID = @id)
            ORDER BY tk.TaiKhoanID DESC";
        return Database.Query(sql, p =>
        {
            var id = DirectoryRules.SearchId(tuKhoa, "TK");
            p.AddWithValue("@id", id);
            p.AddWithValue("@tuKhoa", id > 0 ? "" : tuKhoa?.Trim() ?? "");
            p.AddWithValue("@vaiTro", vaiTro ?? "");
        });
    }

    public bool TenDangNhapTonTai(string tenDangNhap, int? exceptId = null)
    {
        var sql = exceptId.HasValue
            ? "SELECT COUNT(1) FROM TaiKhoan WHERE TenDangNhap = @u AND TaiKhoanID <> @id"
            : "SELECT COUNT(1) FROM TaiKhoan WHERE TenDangNhap = @u";
        return Database.ExecuteScalarInt(sql, p =>
        {
            p.AddWithValue("@u", tenDangNhap.Trim());
            if (exceptId.HasValue) p.AddWithValue("@id", exceptId.Value);
        }) > 0;
    }

    public bool EmailTonTai(string email, int? exceptId = null)
    {
        var sql = exceptId.HasValue
            ? "SELECT COUNT(1) FROM TaiKhoan WHERE Email = @e AND TaiKhoanID <> @id"
            : "SELECT COUNT(1) FROM TaiKhoan WHERE Email = @e";
        return Database.ExecuteScalarInt(sql, p =>
        {
            p.AddWithValue("@e", email.Trim());
            if (exceptId.HasValue) p.AddWithValue("@id", exceptId.Value);
        }) > 0;
    }

    /// <summary>Them tai khoan moi + ho so tuong ung (GiaoVien / HocVien / QuanTriVien).</summary>
    public (bool Ok, string Error, int TaiKhoanID) Them(
        string tenDangNhap, string email, string hoTen, string vaiTro, string matKhau)
    {
        Session.YeuCauQuanTriVien();
        tenDangNhap = tenDangNhap.Trim();
        email = email.Trim();
        hoTen = hoTen.Trim();

        if (string.IsNullOrWhiteSpace(tenDangNhap)) return (false, "Chưa nhập tên đăng nhập.", 0);
        if (string.IsNullOrWhiteSpace(hoTen)) return (false, "Chưa nhập họ tên.", 0);
        if (hoTen.Length > 100) return (false, "Họ tên không được vượt quá 100 ký tự.", 0);
        if (!DirectoryRules.ValidEmail(email)) return (false, "Email chưa đầy đủ. Ví dụ: hoten@gmail.com (tối đa 100 ký tự).", 0);
        if (string.IsNullOrWhiteSpace(matKhau) || matKhau.Length < 6) return (false, "Mật khẩu tối thiểu 6 ký tự.", 0);
        if (vaiTro is not ("GiaoVien" or "HocVien" or "QuanTriVien")) return (false, "Vai trò không hợp lệ.", 0);
        if (TenDangNhapTonTai(tenDangNhap)) return (false, $"Tên đăng nhập '{tenDangNhap}' đã tồn tại.", 0);
        if (EmailTonTai(email)) return (false, $"Email '{email}' đã được sử dụng.", 0);

        var profileTable = vaiTro switch
        {
            "GiaoVien" => "GiaoVien",
            "QuanTriVien" => "QuanTriVien",
            _ => "HocVien"
        };
        var loaiHocVien = vaiTro == "HocVien" ? ", LoaiHocVien, LoaiGoi" : "";
        var giaTriHocVien = vaiTro == "HocVien" ? ", N'HocSinh', N'MienPhi'" : "";

        using var conn = Database.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            using var cmd1 = new SqlCommand(@"
                INSERT INTO TaiKhoan (TenDangNhap, MatKhau, Email, VaiTro, TrangThai, NgayTao)
                VALUES (@u, @p, @e, @v, N'HoatDong', SYSDATETIME());
                SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx);
            cmd1.Parameters.AddWithValue("@u", tenDangNhap);
            cmd1.Parameters.AddWithValue("@p", PasswordHelper.Hash(matKhau));
            cmd1.Parameters.AddWithValue("@e", email);
            cmd1.Parameters.AddWithValue("@v", vaiTro);
            var id = Convert.ToInt32(cmd1.ExecuteScalar() ?? 0);

            using var cmd2 = new SqlCommand(
                $"INSERT INTO {profileTable} (TaiKhoanID, HoTen{loaiHocVien}) VALUES (@id, @h{giaTriHocVien})", conn, tx);
            cmd2.Parameters.AddWithValue("@id", id);
            cmd2.Parameters.AddWithValue("@h", hoTen);
            cmd2.ExecuteNonQuery();

            tx.Commit();
            return (true, "", id);
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, "Lỗi khi tạo tài khoản: " + ex.Message, 0);
        }
    }

    /// <summary>Sua ho ten, email (khong doi ten dang nhap va vai tro de bao toan du lieu).</summary>
    public (bool Ok, string Error) Sua(int taiKhoanId, string email, string hoTen, string vaiTro)
    {
        Session.YeuCauQuanTriVien();
        email = email.Trim();
        hoTen = hoTen.Trim();
        if (string.IsNullOrWhiteSpace(hoTen)) return (false, "Chưa nhập họ tên.");
        if (hoTen.Length > 100) return (false, "Họ tên không được vượt quá 100 ký tự.");
        if (!DirectoryRules.ValidEmail(email)) return (false, "Email chưa đầy đủ. Ví dụ: hoten@gmail.com (tối đa 100 ký tự).");
        if (EmailTonTai(email, taiKhoanId)) return (false, $"Email '{email}' đã được sử dụng.");

        using var conn = Database.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            using var cmd1 = new SqlCommand("UPDATE TaiKhoan SET Email = @e WHERE TaiKhoanID = @id", conn, tx);
            cmd1.Parameters.AddWithValue("@e", email);
            cmd1.Parameters.AddWithValue("@id", taiKhoanId);
            cmd1.ExecuteNonQuery();

            var profileTable = vaiTro switch
            {
                "GiaoVien" => "GiaoVien",
                "QuanTriVien" => "QuanTriVien",
                _ => "HocVien"
            };
            using var cmd2 = new SqlCommand($"UPDATE {profileTable} SET HoTen = @h WHERE TaiKhoanID = @id", conn, tx);
            cmd2.Parameters.AddWithValue("@h", hoTen);
            cmd2.Parameters.AddWithValue("@id", taiKhoanId);
            cmd2.ExecuteNonQuery();

            tx.Commit();
            return (true, "");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, "Lỗi khi cập nhật tài khoản: " + ex.Message);
        }
    }

    /// <summary>Dat lai mat khau cho tai khoan (quan tri vien reset).</summary>
    public void DatLaiMatKhau(int taiKhoanId, string matKhauMoi)
    {
        Session.YeuCauQuanTriVien();
        Database.Execute("UPDATE TaiKhoan SET MatKhau = @p WHERE TaiKhoanID = @id",
            p => { p.AddWithValue("@p", PasswordHelper.Hash(matKhauMoi)); p.AddWithValue("@id", taiKhoanId); });
    }

    /// <summary>Cap goi cho hoc vien. Khi chuyen sang DongPhi lan dau se gui email VIP.</summary>
    public (bool Ok, string Message) CapQuyenGoi(int taiKhoanId, string loaiGoi)
    {
        Session.YeuCauQuanTriVien();
        if (loaiGoi is not ("MienPhi" or "DongPhi"))
            return (false, "Gói không hợp lệ.");

        var row = Database.Query(@"
            SELECT tk.VaiTro, tk.Email, hv.HoTen, hv.LoaiGoi
            FROM TaiKhoan tk
            LEFT JOIN HocVien hv ON hv.TaiKhoanID = tk.TaiKhoanID
            WHERE tk.TaiKhoanID = @id",
            p => p.AddWithValue("@id", taiKhoanId));
        if (row.Rows.Count == 0)
            return (false, "Không tìm thấy tài khoản.");
        if (row.Rows[0]["VaiTro"]?.ToString() != "HocVien")
            return (false, "Chỉ học viên mới được cấp gói miễn phí hoặc đóng phí.");

        var cu = row.Rows[0]["LoaiGoi"]?.ToString();
        if (string.IsNullOrWhiteSpace(cu)) cu = "MienPhi";

        Database.Execute("UPDATE HocVien SET LoaiGoi = @g WHERE TaiKhoanID = @id",
            p =>
            {
                p.AddWithValue("@g", loaiGoi);
                p.AddWithValue("@id", taiKhoanId);
            });

        if (loaiGoi != "DongPhi")
            return (true, "Đã chuyển tài khoản về gói miễn phí.");

        // Chi gui email VIP khi chuyen tu mien phi sang dong phi (khong gui khi dang ky web).
        if (cu == "DongPhi")
            return (true, "Tài khoản đã ở gói đóng phí (VIP). Không gửi lại email VIP.");

        var email = row.Rows[0]["Email"]?.ToString() ?? "";
        var hoTen = row.Rows[0]["HoTen"]?.ToString() ?? "";
        var (sent, error) = VipEmailSender.GuiNangVip(email, hoTen);
        if (!sent)
            return (true, "Đã cấp quyền đóng phí, nhưng chưa gửi được email.\n" + error);

        return (true, $"Đã nâng VIP và gửi email tới {email}.");
    }

    /// <summary>Khoa / mo tai khoan.</summary>
    public void DoiTrangThai(int taiKhoanId, bool khoa)
    {
        Session.YeuCauQuanTriVien();
        if (khoa && Session.Current?.TaiKhoanID == taiKhoanId)
            throw new InvalidOperationException("Không thể tự khóa tài khoản quản trị đang đăng nhập.");

        Database.Execute("UPDATE TaiKhoan SET TrangThai = @t WHERE TaiKhoanID = @id",
            p => { p.AddWithValue("@t", khoa ? "Khoa" : "HoatDong"); p.AddWithValue("@id", taiKhoanId); });
    }

    /// <summary>Xoa tai khoan + ho so (khong cho xoa tai khoan dang dang nhap
    /// va tai khoan da co bai thi de bao toan lich su).</summary>
    public (bool Ok, string Error) Xoa(int taiKhoanId)
    {
        Session.YeuCauQuanTriVien();
        if (Session.Current?.TaiKhoanID == taiKhoanId)
            return (false, "Không thể xóa tài khoản đang đăng nhập.");

        using var conn = Database.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            using var dependencies = new SqlCommand(@"
                SELECT
                    (SELECT COUNT(*) FROM BaiLam bl JOIN HocVien hv ON hv.HocVienID=bl.HocVienID WHERE hv.TaiKhoanID=@id)
                  + (SELECT COUNT(*) FROM DangKyDotThi dk JOIN HocVien hv ON hv.HocVienID=dk.HocVienID WHERE hv.TaiKhoanID=@id)
                  + (SELECT COUNT(*) FROM CauHoi c WHERE c.NguoiTaoID=@id)
                  + (SELECT COUNT(*) FROM DeThi d WHERE d.NguoiTaoID=@id)
                  + (SELECT COUNT(*) FROM PhanCongGiangDay pc JOIN GiaoVien gv ON gv.GiaoVienID=pc.GiaoVienID WHERE gv.TaiKhoanID=@id)", conn, tx);
            dependencies.Parameters.AddWithValue("@id", taiKhoanId);
            if (Convert.ToInt32(dependencies.ExecuteScalar()) > 0)
                return (false, "Hồ sơ đã có bài thi, đăng ký thi, câu hỏi, đề thi hoặc phân công lớp. Hãy khóa tài khoản để giữ lịch sử.");

            using var delete = new SqlCommand(@"
                DELETE hl FROM HocVien_Lop hl JOIN HocVien hv ON hv.HocVienID=hl.HocVienID WHERE hv.TaiKhoanID=@id;
                DELETE gm FROM GiaoVien_MonHoc gm JOIN GiaoVien gv ON gv.GiaoVienID=gm.GiaoVienID WHERE gv.TaiKhoanID=@id;
                DELETE FROM GiaoVien WHERE TaiKhoanID=@id;
                DELETE FROM HocVien WHERE TaiKhoanID=@id;
                DELETE FROM QuanTriVien WHERE TaiKhoanID=@id;
                DELETE FROM TaiKhoan WHERE TaiKhoanID=@id;", conn, tx);
            delete.Parameters.AddWithValue("@id", taiKhoanId);
            if (delete.ExecuteNonQuery() == 0) return (false, "Không tìm thấy tài khoản.");
            tx.Commit();
            return (true, "");
        }
        catch (Exception ex)
        {
            try { tx.Rollback(); } catch { /* Giao dịch có thể đã đóng. */ }
            return (false, "Không thể xóa hồ sơ: " + ex.Message);
        }
    }
}
