using System.Data;
using QLThiTN.WinForms.Models;

namespace QLThiTN.WinForms.Services;

/// <summary>CRUD tai khoan (man hinh Quan tri vien): them/sua/xoa, khoa/mo.</summary>
public class AccountService
{
    public DataTable LayDanhSach(string? tuKhoa, string? vaiTro)
    {
        var sql = @"
            SELECT tk.TaiKhoanID, tk.TenDangNhap, tk.Email, tk.VaiTro, tk.TrangThai,
                   COALESCE(gv.HoTen, hv.HoTen, qtv.HoTen, N'') AS HoTen,
                   tk.NgayTao
            FROM TaiKhoan tk
            LEFT JOIN GiaoVien gv ON gv.TaiKhoanID = tk.TaiKhoanID
            LEFT JOIN HocVien hv ON hv.TaiKhoanID = tk.TaiKhoanID
            LEFT JOIN QuanTriVien qtv ON qtv.TaiKhoanID = tk.TaiKhoanID
            WHERE (@tuKhoa = N'' OR tk.TenDangNhap LIKE '%' + @tuKhoa + N'%' OR tk.Email LIKE '%' + @tuKhoa + N'%'
                   OR COALESCE(gv.HoTen, hv.HoTen, qtv.HoTen, N'') LIKE '%' + @tuKhoa + N'%')
              AND (@vaiTro = N'' OR tk.VaiTro = @vaiTro)
            ORDER BY tk.TaiKhoanID DESC";
        return Database.Query(sql, p =>
        {
            p.AddWithValue("@tuKhoa", tuKhoa?.Trim() ?? "");
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
        tenDangNhap = tenDangNhap.Trim();
        email = email.Trim();
        hoTen = hoTen.Trim();

        if (string.IsNullOrWhiteSpace(tenDangNhap)) return (false, "Chưa nhập tên đăng nhập.", 0);
        if (string.IsNullOrWhiteSpace(hoTen)) return (false, "Chưa nhập họ tên.", 0);
        if (string.IsNullOrWhiteSpace(matKhau) || matKhau.Length < 6) return (false, "Mật khẩu tối thiểu 6 ký tự.", 0);
        if (vaiTro is not ("GiaoVien" or "HocVien" or "QuanTriVien")) return (false, "Vai trò không hợp lệ.", 0);
        if (TenDangNhapTonTai(tenDangNhap)) return (false, $"Tên đăng nhập '{tenDangNhap}' đã tồn tại.", 0);
        if (EmailTonTai(email)) return (false, $"Email '{email}' đã được sử dụng.", 0);

        var id = Database.ExecuteScalarInt(@"
            INSERT INTO TaiKhoan (TenDangNhap, MatKhau, Email, VaiTro, TrangThai, NgayTao)
            VALUES (@u, @p, @e, @v, N'HoatDong', SYSDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS INT);",
            p =>
            {
                p.AddWithValue("@u", tenDangNhap);
                p.AddWithValue("@p", PasswordHelper.Hash(matKhau));
                p.AddWithValue("@e", email);
                p.AddWithValue("@v", vaiTro);
            });

        var profileTable = vaiTro switch
        {
            "GiaoVien" => "GiaoVien",
            "QuanTriVien" => "QuanTriVien",
            _ => "HocVien"
        };
        var profilePk = vaiTro switch
        {
            "GiaoVien" => "GiaoVienID",
            "QuanTriVien" => "QuanTriVienID",
            _ => "HocVienID"
        };
        var loaiHocVien = vaiTro == "HocVien" ? ", LoaiHocVien" : "";
        var giaTriHocVien = vaiTro == "HocVien" ? ", N'HocSinh'" : "";

        Database.Execute(
            $"INSERT INTO {profileTable} (TaiKhoanID, HoTen{loaiHocVien}) VALUES (@id, @h{giaTriHocVien})",
            p => { p.AddWithValue("@id", id); p.AddWithValue("@h", hoTen); });

        return (true, "", id);
    }

    /// <summary>Sua ho ten, email (khong doi ten dang nhap va vai tro de bao toan du lieu).</summary>
    public (bool Ok, string Error) Sua(int taiKhoanId, string email, string hoTen, string vaiTro)
    {
        email = email.Trim();
        hoTen = hoTen.Trim();
        if (string.IsNullOrWhiteSpace(hoTen)) return (false, "Chưa nhập họ tên.");
        if (EmailTonTai(email, taiKhoanId)) return (false, $"Email '{email}' đã được sử dụng.");

        Database.Execute("UPDATE TaiKhoan SET Email = @e WHERE TaiKhoanID = @id",
            p => { p.AddWithValue("@e", email); p.AddWithValue("@id", taiKhoanId); });

        var profileTable = vaiTro switch
        {
            "GiaoVien" => "GiaoVien",
            "QuanTriVien" => "QuanTriVien",
            _ => "HocVien"
        };
        Database.Execute($"UPDATE {profileTable} SET HoTen = @h WHERE TaiKhoanID = @id",
            p => { p.AddWithValue("@h", hoTen); p.AddWithValue("@id", taiKhoanId); });
        return (true, "");
    }

    /// <summary>Dat lai mat khau cho tai khoan (quan tri vien reset).</summary>
    public void DatLaiMatKhau(int taiKhoanId, string matKhauMoi)
    {
        Database.Execute("UPDATE TaiKhoan SET MatKhau = @p WHERE TaiKhoanID = @id",
            p => { p.AddWithValue("@p", PasswordHelper.Hash(matKhauMoi)); p.AddWithValue("@id", taiKhoanId); });
    }

    /// <summary>Khoa / mo tai khoan.</summary>
    public void DoiTrangThai(int taiKhoanId, bool khoa)
    {
        Database.Execute("UPDATE TaiKhoan SET TrangThai = @t WHERE TaiKhoanID = @id",
            p => { p.AddWithValue("@t", khoa ? "Khoa" : "HoatDong"); p.AddWithValue("@id", taiKhoanId); });
    }

    /// <summary>Xoa tai khoan + ho so (khong cho xoa tai khoan dang dang nhap
    /// va tai khoan da co bai thi de bao toan lich su).</summary>
    public (bool Ok, string Error) Xoa(int taiKhoanId)
    {
        if (Session.Current?.TaiKhoanID == taiKhoanId)
            return (false, "Không thể xóa tài khoản đang đăng nhập.");

        var soBaiThi = Database.ExecuteScalarInt(@"
            SELECT COUNT(1) FROM BaiLam bl
            JOIN HocVien hv ON hv.HocVienID = bl.HocVienID
            WHERE hv.TaiKhoanID = @id", p => p.AddWithValue("@id", taiKhoanId));
        if (soBaiThi > 0)
            return (false, "Tài khoản này đã có bài thi, chỉ nên khóa thay vì xóa để giữ lịch sử.");

        Database.Execute(@"
            DELETE FROM GiaoVien WHERE TaiKhoanID = @id;
            DELETE FROM HocVien WHERE TaiKhoanID = @id;
            DELETE FROM QuanTriVien WHERE TaiKhoanID = @id;
            DELETE FROM TaiKhoan WHERE TaiKhoanID = @id;",
            p => p.AddWithValue("@id", taiKhoanId));
        return (true, "");
    }
}
