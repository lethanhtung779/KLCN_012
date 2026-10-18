using System.Data;
using Microsoft.Data.SqlClient;
using QLThiTN.WinForms.Models;

namespace QLThiTN.WinForms.Services;

/// <summary>Quan ly khoa hoc, lop hoc, xep hoc vien, phan cong giang day.</summary>
public class TrainingService
{
    public DataTable LayDanhSachMonHoc() =>
        Database.Query("SELECT MonHocID, TenMon FROM MonHoc ORDER BY TenMon");

    public DataTable LayDanhSachKhoaHoc(string? tuKhoa, int? monHocId)
    {
        var sql = @"
            SELECT kh.KhoaHocID, kh.TenKhoaHoc, kh.MonHocID, mh.TenMon,
                   kh.NgayBatDau, kh.NgayKetThuc, kh.TrangThai,
                   (SELECT COUNT(1) FROM LopHoc lh WHERE lh.KhoaHocID = kh.KhoaHocID) AS SoLop
            FROM KhoaHoc kh
            INNER JOIN MonHoc mh ON mh.MonHocID = kh.MonHocID
            WHERE (@tuKhoa = N'' OR kh.TenKhoaHoc LIKE N'%' + @tuKhoa + N'%' OR mh.TenMon LIKE N'%' + @tuKhoa + N'%')
              AND (@monHocId IS NULL OR kh.MonHocID = @monHocId)
            ORDER BY kh.KhoaHocID DESC";
        return Database.Query(sql, p =>
        {
            p.AddWithValue("@tuKhoa", tuKhoa?.Trim() ?? "");
            p.AddWithValue("@monHocId", monHocId.HasValue ? monHocId.Value : DBNull.Value);
        });
    }

    public (bool Ok, string Error, int Id) ThemKhoaHoc(string ten, int monHocId, DateTime? ngayBd, DateTime? ngayKt, string trangThai)
    {
        Session.YeuCauQuanTriVien();
        ten = ten.Trim();
        if (string.IsNullOrWhiteSpace(ten)) return (false, "Chưa nhập tên khóa học.", 0);
        if (monHocId <= 0) return (false, "Hãy chọn môn học.", 0);
        if (trangThai is not (TrangThaiKhoaHoc.HoatDong or TrangThaiKhoaHoc.TamDung or TrangThaiKhoaHoc.DaKetThuc))
            return (false, "Trạng thái không hợp lệ.", 0);
        if (ngayBd.HasValue && ngayKt.HasValue && ngayKt.Value < ngayBd.Value)
            return (false, "Ngày kết thúc phải diễn ra sau ngày bắt đầu.", 0);

        var id = Database.ExecuteScalarInt(@"
            INSERT INTO KhoaHoc (TenKhoaHoc, MonHocID, NgayBatDau, NgayKetThuc, TrangThai)
            VALUES (@ten, @mon, @bd, @kt, @tt);
            SELECT CAST(SCOPE_IDENTITY() AS INT);",
            p =>
            {
                p.AddWithValue("@ten", ten);
                p.AddWithValue("@mon", monHocId);
                p.AddWithValue("@bd", ngayBd.HasValue ? ngayBd.Value.Date : DBNull.Value);
                p.AddWithValue("@kt", ngayKt.HasValue ? ngayKt.Value.Date : DBNull.Value);
                p.AddWithValue("@tt", trangThai);
            });
        return (true, "", id);
    }

    public (bool Ok, string Error) SuaKhoaHoc(int id, string ten, int monHocId, DateTime? ngayBd, DateTime? ngayKt, string trangThai)
    {
        Session.YeuCauQuanTriVien();
        ten = ten.Trim();
        if (id <= 0) return (false, "Khóa học không hợp lệ.");
        if (string.IsNullOrWhiteSpace(ten)) return (false, "Chưa nhập tên khóa học.");
        if (monHocId <= 0) return (false, "Hãy chọn môn học.");
        if (ngayBd.HasValue && ngayKt.HasValue && ngayKt.Value < ngayBd.Value)
            return (false, "Ngày kết thúc phải diễn ra sau ngày bắt đầu.");

        Database.Execute(@"
            UPDATE KhoaHoc SET TenKhoaHoc = @ten, MonHocID = @mon, NgayBatDau = @bd, NgayKetThuc = @kt, TrangThai = @tt
            WHERE KhoaHocID = @id",
            p =>
            {
                p.AddWithValue("@ten", ten);
                p.AddWithValue("@mon", monHocId);
                p.AddWithValue("@bd", ngayBd.HasValue ? ngayBd.Value.Date : DBNull.Value);
                p.AddWithValue("@kt", ngayKt.HasValue ? ngayKt.Value.Date : DBNull.Value);
                p.AddWithValue("@tt", trangThai);
                p.AddWithValue("@id", id);
            });
        return (true, "");
    }

    public (bool Ok, string Error) XoaKhoaHoc(int id)
    {
        Session.YeuCauQuanTriVien();
        if (Database.ExecuteScalarInt("SELECT COUNT(1) FROM LopHoc WHERE KhoaHocID = @id", p => p.AddWithValue("@id", id)) > 0)
            return (false, "Không thể xóa: khóa học đã có lớp học. Hãy xóa các lớp trước.");
        Database.Execute("DELETE FROM KhoaHoc WHERE KhoaHocID = @id", p => p.AddWithValue("@id", id));
        return (true, "");
    }

    public DataTable LayDanhSachLopHoc(int? khoaHocId, string? tuKhoa)
    {
        var sql = @"
            SELECT lh.LopHocID, lh.TenLop, lh.KhoaHocID, kh.TenKhoaHoc, mh.TenMon,
                   (SELECT COUNT(1) FROM HocVien_Lop hvl WHERE hvl.LopHocID = lh.LopHocID AND hvl.TrangThai = N'DangHoc') AS SoHocVien,
                   CASE WHEN EXISTS (SELECT 1 FROM PhanCongGiangDay pc WHERE pc.LopHocID = lh.LopHocID)
                        THEN N'Đã phân công' ELSE N'Chưa phân công' END AS TrangThaiPhanCong
            FROM LopHoc lh
            INNER JOIN KhoaHoc kh ON kh.KhoaHocID = lh.KhoaHocID
            INNER JOIN MonHoc mh ON mh.MonHocID = kh.MonHocID
            WHERE (@khoaHocId IS NULL OR lh.KhoaHocID = @khoaHocId)
              AND (@tuKhoa = N'' OR lh.TenLop LIKE N'%' + @tuKhoa + N'%' OR kh.TenKhoaHoc LIKE N'%' + @tuKhoa + N'%')
            ORDER BY lh.LopHocID DESC";
        return Database.Query(sql, p =>
        {
            p.AddWithValue("@khoaHocId", khoaHocId.HasValue ? khoaHocId.Value : DBNull.Value);
            p.AddWithValue("@tuKhoa", tuKhoa?.Trim() ?? "");
        });
    }

    /// <summary>Danh sách lớp cho màn hình chi tiết, giới hạn giáo viên theo phân công thật.</summary>
    public DataTable LayLopDuocXem(string? tuKhoa = null)
    {
        if (!Session.IsAccountActive() || !Session.IsQuanTriVien && !Session.IsGiaoVien)
            throw new UnauthorizedAccessException("Bạn không có quyền xem lớp học.");
        return Database.Query(@"
            SELECT lh.LopHocID, lh.TenLop, kh.TenKhoaHoc, mh.TenMon,
                   (SELECT COUNT(*) FROM HocVien_Lop hl WHERE hl.LopHocID = lh.LopHocID AND hl.TrangThai = N'DangHoc') AS SoHocVien,
                   CASE WHEN EXISTS (SELECT 1 FROM PhanCongGiangDay pc WHERE pc.LopHocID = lh.LopHocID)
                        THEN N'Đã phân công' ELSE N'Chưa phân công' END AS TrangThaiPhanCong
            FROM LopHoc lh
            JOIN KhoaHoc kh ON kh.KhoaHocID = lh.KhoaHocID
            JOIN MonHoc mh ON mh.MonHocID = kh.MonHocID
            WHERE (@admin = 1 OR EXISTS (
                SELECT 1 FROM PhanCongGiangDay pc
                WHERE pc.LopHocID = lh.LopHocID AND pc.GiaoVienID = @gv))
              AND (@search = N'' OR lh.TenLop LIKE N'%' + @search + N'%'
                   OR kh.TenKhoaHoc LIKE N'%' + @search + N'%')
            ORDER BY lh.LopHocID DESC", p =>
        {
            p.AddWithValue("@admin", Session.IsQuanTriVien);
            p.AddWithValue("@gv", Session.GiaoVienID);
            p.AddWithValue("@search", tuKhoa?.Trim() ?? "");
        });
    }

    public DataTable LayThongTinLopDuocXem(int lopHocId)
    {
        if (!Session.IsAccountActive() || !Session.IsQuanTriVien && !Session.IsGiaoVien)
            throw new UnauthorizedAccessException("Bạn không có quyền xem lớp học.");
        return Database.Query(@"
            SELECT lh.LopHocID, lh.TenLop, lh.KhoaHocID, kh.TenKhoaHoc, kh.NgayBatDau,
                   kh.NgayKetThuc, kh.TrangThai, mh.TenMon,
                   (SELECT COUNT(*) FROM HocVien_Lop hl WHERE hl.LopHocID = lh.LopHocID AND hl.TrangThai = N'DangHoc') AS SoHocVien,
                   (SELECT COUNT(*) FROM LichHoc lich WHERE lich.LopHocID = lh.LopHocID AND lich.ThoiGianBatDau >= SYSDATETIME()) AS SoBuoiSapToi
            FROM LopHoc lh
            JOIN KhoaHoc kh ON kh.KhoaHocID = lh.KhoaHocID
            JOIN MonHoc mh ON mh.MonHocID = kh.MonHocID
            WHERE lh.LopHocID = @lop AND (@admin = 1 OR EXISTS (
                SELECT 1 FROM PhanCongGiangDay pc
                WHERE pc.LopHocID = lh.LopHocID AND pc.GiaoVienID = @gv))", p =>
        {
            p.AddWithValue("@lop", lopHocId);
            p.AddWithValue("@admin", Session.IsQuanTriVien);
            p.AddWithValue("@gv", Session.GiaoVienID);
        });
    }

    public DataTable LayHocVienLopDuocXem(int lopHocId)
    {
        YeuCauQuyenXemLop(lopHocId);
        return LayHocVienTrongLop(lopHocId);
    }

    public DataTable LayPhanCongLopDuocXem(int lopHocId)
    {
        YeuCauQuyenXemLop(lopHocId);
        return LayPhanCongTheoLop(lopHocId);
    }

    public DataTable LayLichHocLopDuocXem(int lopHocId)
    {
        YeuCauQuyenXemLop(lopHocId);
        return LayLichHocTheoLop(lopHocId);
    }

    private void YeuCauQuyenXemLop(int lopHocId)
    {
        if (LayThongTinLopDuocXem(lopHocId).Rows.Count == 0)
            throw new UnauthorizedAccessException("Bạn không được phân công phụ trách lớp học này.");
    }

    public DataTable LayComboKhoaHoc() =>
        Database.Query(@"
            SELECT kh.KhoaHocID, kh.TenKhoaHoc + N' — ' + mh.TenMon AS TenHienThi
            FROM KhoaHoc kh
            INNER JOIN MonHoc mh ON mh.MonHocID = kh.MonHocID
            ORDER BY kh.TenKhoaHoc");

    public DataTable LayComboLopHoc() =>
        Database.Query(@"
            SELECT lh.LopHocID, lh.TenLop + N' (' + kh.TenKhoaHoc + N')' AS TenHienThi
            FROM LopHoc lh
            INNER JOIN KhoaHoc kh ON kh.KhoaHocID = lh.KhoaHocID
            ORDER BY lh.TenLop");

    public (bool Ok, string Error, int Id) ThemLopHoc(string tenLop, int khoaHocId)
    {
        Session.YeuCauQuanTriVien();
        tenLop = tenLop.Trim();
        if (string.IsNullOrWhiteSpace(tenLop)) return (false, "Chưa nhập tên lớp.", 0);
        if (khoaHocId <= 0) return (false, "Hãy chọn khóa học.", 0);

        var id = Database.ExecuteScalarInt(@"
            INSERT INTO LopHoc (TenLop, KhoaHocID) VALUES (@ten, @kh);
            SELECT CAST(SCOPE_IDENTITY() AS INT);",
            p => { p.AddWithValue("@ten", tenLop); p.AddWithValue("@kh", khoaHocId); });
        return (true, "", id);
    }

    public (bool Ok, string Error) SuaLopHoc(int id, string tenLop, int khoaHocId)
    {
        Session.YeuCauQuanTriVien();
        tenLop = tenLop.Trim();
        if (string.IsNullOrWhiteSpace(tenLop)) return (false, "Chưa nhập tên lớp.");
        Database.Execute("UPDATE LopHoc SET TenLop = @ten, KhoaHocID = @kh WHERE LopHocID = @id",
            p => { p.AddWithValue("@ten", tenLop); p.AddWithValue("@kh", khoaHocId); p.AddWithValue("@id", id); });
        return (true, "");
    }

    public (bool Ok, string Error) XoaLopHoc(int id)
    {
        Session.YeuCauQuanTriVien();
        if (Database.ExecuteScalarInt("SELECT COUNT(1) FROM HocVien_Lop WHERE LopHocID = @id", p => p.AddWithValue("@id", id)) > 0)
            return (false, "Không thể xóa: lớp còn học viên. Hãy gỡ học viên khỏi lớp trước.");
        if (Database.ExecuteScalarInt("SELECT COUNT(1) FROM PhanCongGiangDay WHERE LopHocID = @id", p => p.AddWithValue("@id", id)) > 0)
            return (false, "Không thể xóa: lớp đã có phân công giảng dạy.");
        if (Database.ExecuteScalarInt("SELECT COUNT(1) FROM LichHoc WHERE LopHocID = @id", p => p.AddWithValue("@id", id)) > 0)
            return (false, "Không thể xóa: lớp đã có lịch học.");
        Database.Execute("DELETE FROM LopHoc WHERE LopHocID = @id", p => p.AddWithValue("@id", id));
        return (true, "");
    }

    public DataTable LayHocVienTrongLop(int lopHocId) =>
        Database.Query(@"
            SELECT hvl.HocVienID, hv.HoTen, tk.TenDangNhap, tk.Email,
                   hvl.NgayThamGia, hvl.TrangThai
            FROM HocVien_Lop hvl
            INNER JOIN HocVien hv ON hv.HocVienID = hvl.HocVienID
            INNER JOIN TaiKhoan tk ON tk.TaiKhoanID = hv.TaiKhoanID
            WHERE hvl.LopHocID = @lop
            ORDER BY hv.HoTen",
            p => p.AddWithValue("@lop", lopHocId));

    public DataTable LayHocVienChuaTrongLop(int lopHocId, string? tuKhoa)
    {
        var sql = @"
            SELECT hv.HocVienID, hv.HoTen, tk.TenDangNhap, tk.Email
            FROM HocVien hv
            INNER JOIN TaiKhoan tk ON tk.TaiKhoanID = hv.TaiKhoanID
            WHERE tk.VaiTro = N'HocVien' AND tk.TrangThai = N'HoatDong'
              AND NOT EXISTS (SELECT 1 FROM HocVien_Lop hvl WHERE hvl.HocVienID = hv.HocVienID AND hvl.LopHocID = @lop)
              AND (@tuKhoa = N'' OR hv.HoTen LIKE N'%' + @tuKhoa + N'%'
                   OR tk.TenDangNhap LIKE N'%' + @tuKhoa + N'%' OR tk.Email LIKE N'%' + @tuKhoa + N'%')
            ORDER BY hv.HoTen";
        return Database.Query(sql, p =>
        {
            p.AddWithValue("@lop", lopHocId);
            p.AddWithValue("@tuKhoa", tuKhoa?.Trim() ?? "");
        });
    }

    public (bool Ok, string Error) ThemHocVienVaoLop(int lopHocId, int hocVienId)
    {
        Session.YeuCauQuanTriVien();
        if (lopHocId <= 0 || hocVienId <= 0) return (false, "Dữ liệu không hợp lệ.");
        if (Database.ExecuteScalarInt(
                "SELECT COUNT(1) FROM HocVien_Lop WHERE LopHocID = @lop AND HocVienID = @hv",
                p => { p.AddWithValue("@lop", lopHocId); p.AddWithValue("@hv", hocVienId); }) > 0)
            return (false, "Học viên đã có trong lớp này.");

        Database.Execute(@"
            INSERT INTO HocVien_Lop (HocVienID, LopHocID, NgayThamGia, TrangThai)
            VALUES (@hv, @lop, SYSDATETIME(), N'DangHoc')",
            p => { p.AddWithValue("@hv", hocVienId); p.AddWithValue("@lop", lopHocId); });
        return (true, "");
    }

    public (bool Ok, string Error) XoaHocVienKhoiLop(int lopHocId, int hocVienId)
    {
        Session.YeuCauQuanTriVien();
        Database.Execute("DELETE FROM HocVien_Lop WHERE LopHocID = @lop AND HocVienID = @hv",
            p => { p.AddWithValue("@lop", lopHocId); p.AddWithValue("@hv", hocVienId); });
        return (true, "");
    }

    public (bool Ok, string Error) DoiTrangThaiHocVienLop(int lopHocId, int hocVienId, string trangThai)
    {
        Session.YeuCauQuanTriVien();
        if (trangThai is not (TrangThaiHocVienLop.DangHoc or TrangThaiHocVienLop.DaKetThuc or TrangThaiHocVienLop.ThoiHoc))
            return (false, "Trạng thái không hợp lệ.");
        Database.Execute("UPDATE HocVien_Lop SET TrangThai = @tt WHERE LopHocID = @lop AND HocVienID = @hv",
            p => { p.AddWithValue("@tt", trangThai); p.AddWithValue("@lop", lopHocId); p.AddWithValue("@hv", hocVienId); });
        return (true, "");
    }

    public DataTable LayPhanCongTheoLop(int lopHocId) =>
        Database.Query(@"
            SELECT pc.LopHocID, pc.MonHocID, mh.TenMon, pc.GiaoVienID, gv.HoTen AS TenGiaoVien
            FROM PhanCongGiangDay pc
            INNER JOIN MonHoc mh ON mh.MonHocID = pc.MonHocID
            INNER JOIN GiaoVien gv ON gv.GiaoVienID = pc.GiaoVienID
            WHERE pc.LopHocID = @lop
            ORDER BY mh.TenMon",
            p => p.AddWithValue("@lop", lopHocId));

    public DataTable LayMonHocChuaPhanCong(int lopHocId) =>
        Database.Query(@"
            SELECT mh.MonHocID, mh.TenMon
            FROM MonHoc mh
            LEFT JOIN LopHoc lh ON lh.LopHocID = @lop
            LEFT JOIN KhoaHoc kh ON kh.KhoaHocID = lh.KhoaHocID
            WHERE NOT EXISTS (
                SELECT 1 FROM PhanCongGiangDay pc
                WHERE pc.LopHocID = @lop AND pc.MonHocID = mh.MonHocID)
            ORDER BY CASE WHEN mh.MonHocID = kh.MonHocID THEN 0 ELSE 1 END, mh.TenMon",
            p => p.AddWithValue("@lop", lopHocId));

    public DataTable LayGiaoVienTheoMon(int monHocId) =>
        Database.Query(@"
            SELECT gv.GiaoVienID, 
                   CASE 
                       WHEN gvm.MonHocID IS NOT NULL AND tk.TenDangNhap IS NOT NULL 
                           THEN gv.HoTen + N' (' + tk.TenDangNhap + N') [Đúng chuyên môn]'
                       WHEN tk.TenDangNhap IS NOT NULL 
                           THEN gv.HoTen + N' (' + tk.TenDangNhap + N')'
                       ELSE gv.HoTen 
                   END AS HoTen,
                   CASE WHEN gvm.MonHocID IS NOT NULL THEN 0 ELSE 1 END AS ThuTu
            FROM GiaoVien gv
            LEFT JOIN TaiKhoan tk ON tk.TaiKhoanID = gv.TaiKhoanID
            LEFT JOIN GiaoVien_MonHoc gvm ON gvm.GiaoVienID = gv.GiaoVienID AND gvm.MonHocID = @mon
            WHERE tk.TrangThai = N'HoatDong' OR tk.TrangThai IS NULL
            ORDER BY ThuTu, gv.HoTen",
            p => p.AddWithValue("@mon", monHocId));

    public DataTable LayTatCaGiaoVien() =>
        Database.Query(@"
            SELECT gv.GiaoVienID, 
                   CASE WHEN tk.TenDangNhap IS NOT NULL 
                        THEN gv.HoTen + N' (' + tk.TenDangNhap + N')' 
                        ELSE gv.HoTen END AS HoTen
            FROM GiaoVien gv
            LEFT JOIN TaiKhoan tk ON tk.TaiKhoanID = gv.TaiKhoanID
            WHERE tk.TrangThai = N'HoatDong' OR tk.TrangThai IS NULL
            ORDER BY gv.HoTen");

    public (bool Ok, string Error) LuuPhanCong(int lopHocId, int monHocId, int giaoVienId, bool sua)
    {
        Session.YeuCauQuanTriVien();
        if (lopHocId <= 0 || monHocId <= 0 || giaoVienId <= 0)
            return (false, "Hãy chọn đủ lớp, môn và giáo viên.");

        if (!sua && Database.ExecuteScalarInt(
                "SELECT COUNT(1) FROM PhanCongGiangDay WHERE LopHocID = @lop AND MonHocID = @mon",
                p => { p.AddWithValue("@lop", lopHocId); p.AddWithValue("@mon", monHocId); }) > 0)
            return (false, "Môn này đã có phân công cho lớp. Hãy sửa thay vì thêm mới.");

        using var conn = Database.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            using var cmd1 = new SqlCommand(@"
                IF NOT EXISTS (SELECT 1 FROM GiaoVien_MonHoc WHERE GiaoVienID = @gv AND MonHocID = @mon)
                    INSERT INTO GiaoVien_MonHoc (GiaoVienID, MonHocID) VALUES (@gv, @mon)", conn, tx);
            cmd1.Parameters.AddWithValue("@gv", giaoVienId);
            cmd1.Parameters.AddWithValue("@mon", monHocId);
            cmd1.ExecuteNonQuery();

            using var cmd2 = new SqlCommand(sua ? @"
                UPDATE PhanCongGiangDay SET GiaoVienID = @gv
                WHERE LopHocID = @lop AND MonHocID = @mon" : @"
                INSERT INTO PhanCongGiangDay (LopHocID, MonHocID, GiaoVienID)
                VALUES (@lop, @mon, @gv)", conn, tx);
            cmd2.Parameters.AddWithValue("@gv", giaoVienId);
            cmd2.Parameters.AddWithValue("@lop", lopHocId);
            cmd2.Parameters.AddWithValue("@mon", monHocId);
            cmd2.ExecuteNonQuery();

            tx.Commit();
            return (true, "");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            return (false, "Lỗi phân công: " + ex.Message);
        }
    }

    public (bool Ok, string Error) XoaPhanCong(int lopHocId, int monHocId)
    {
        Session.YeuCauQuanTriVien();
        Database.Execute("DELETE FROM PhanCongGiangDay WHERE LopHocID = @lop AND MonHocID = @mon",
            p => { p.AddWithValue("@lop", lopHocId); p.AddWithValue("@mon", monHocId); });
        return (true, "");
    }

    public DataTable LayLichHocTheoLop(int lopHocId) =>
        Database.Query(@"
            SELECT LichHocID, LopHocID, NoiDung, ThoiGianBatDau, ThoiGianKetThuc, DiaDiem
            FROM LichHoc
            WHERE LopHocID = @lop
            ORDER BY ThoiGianBatDau",
            p => p.AddWithValue("@lop", lopHocId));

    public (bool Ok, string Error, int Id) ThemLichHoc(int lopHocId, string? noiDung, DateTime batDau, DateTime ketThuc, string? diaDiem)
    {
        Session.YeuCauQuanTriVien();
        if (lopHocId <= 0) return (false, "Hãy chọn lớp.", 0);
        if (ketThuc <= batDau) return (false, "Thời gian kết thúc phải sau thời gian bắt đầu.", 0);

        var trungLich = Database.ExecuteScalarInt(@"
            SELECT COUNT(1) FROM LichHoc
            WHERE (LopHocID = @lop OR (@dd IS NOT NULL AND @dd <> N'' AND DiaDiem = @dd))
              AND (ThoiGianBatDau < @kt AND ThoiGianKetThuc > @bd)",
            p =>
            {
                p.AddWithValue("@lop", lopHocId);
                p.AddWithValue("@dd", string.IsNullOrWhiteSpace(diaDiem) ? DBNull.Value : diaDiem.Trim());
                p.AddWithValue("@bd", batDau);
                p.AddWithValue("@kt", ketThuc);
            }) > 0;
        if (trungLich)
            return (false, "Lịch học bị trùng thời gian với lớp học hoặc phòng học khác.", 0);

        var id = Database.ExecuteScalarInt(@"
            INSERT INTO LichHoc (LopHocID, NoiDung, ThoiGianBatDau, ThoiGianKetThuc, DiaDiem)
            VALUES (@lop, @nd, @bd, @kt, @dd);
            SELECT CAST(SCOPE_IDENTITY() AS INT);",
            p =>
            {
                p.AddWithValue("@lop", lopHocId);
                p.AddWithValue("@nd", string.IsNullOrWhiteSpace(noiDung) ? DBNull.Value : noiDung.Trim());
                p.AddWithValue("@bd", batDau);
                p.AddWithValue("@kt", ketThuc);
                p.AddWithValue("@dd", string.IsNullOrWhiteSpace(diaDiem) ? DBNull.Value : diaDiem.Trim());
            });
        return (true, "", id);
    }

    public (bool Ok, string Error) SuaLichHoc(int lichHocId, int lopHocId, string? noiDung, DateTime batDau, DateTime ketThuc, string? diaDiem)
    {
        Session.YeuCauQuanTriVien();
        if (ketThuc <= batDau) return (false, "Thời gian kết thúc phải sau thời gian bắt đầu.");

        var trungLich = Database.ExecuteScalarInt(@"
            SELECT COUNT(1) FROM LichHoc
            WHERE LichHocID <> @id
              AND (LopHocID = @lop OR (@dd IS NOT NULL AND @dd <> N'' AND DiaDiem = @dd))
              AND (ThoiGianBatDau < @kt AND ThoiGianKetThuc > @bd)",
            p =>
            {
                p.AddWithValue("@id", lichHocId);
                p.AddWithValue("@lop", lopHocId);
                p.AddWithValue("@dd", string.IsNullOrWhiteSpace(diaDiem) ? DBNull.Value : diaDiem.Trim());
                p.AddWithValue("@bd", batDau);
                p.AddWithValue("@kt", ketThuc);
            }) > 0;
        if (trungLich)
            return (false, "Lịch học bị trùng thời gian với lớp học hoặc phòng học khác.");

        Database.Execute(@"
            UPDATE LichHoc SET LopHocID = @lop, NoiDung = @nd, ThoiGianBatDau = @bd, ThoiGianKetThuc = @kt, DiaDiem = @dd
            WHERE LichHocID = @id",
            p =>
            {
                p.AddWithValue("@lop", lopHocId);
                p.AddWithValue("@nd", string.IsNullOrWhiteSpace(noiDung) ? DBNull.Value : noiDung.Trim());
                p.AddWithValue("@bd", batDau);
                p.AddWithValue("@kt", ketThuc);
                p.AddWithValue("@dd", string.IsNullOrWhiteSpace(diaDiem) ? DBNull.Value : diaDiem.Trim());
                p.AddWithValue("@id", lichHocId);
            });
        return (true, "");
    }

    public (bool Ok, string Error) XoaLichHoc(int lichHocId)
    {
        Session.YeuCauQuanTriVien();
        Database.Execute("DELETE FROM LichHoc WHERE LichHocID = @id", p => p.AddWithValue("@id", lichHocId));
        return (true, "");
    }

    /// <summary>Cay khoa hoc → lop → giao vien + hoc vien (loc theo tu khoa neu co).</summary>
    public List<TongHopKhoaHoc> LayTongHopDaoTao(string? tuKhoa)
    {
        var key = tuKhoa?.Trim() ?? "";

        var dtKhoa = Database.Query(@"
            SELECT kh.KhoaHocID, kh.TenKhoaHoc, mh.TenMon, kh.TrangThai
            FROM KhoaHoc kh
            INNER JOIN MonHoc mh ON mh.MonHocID = kh.MonHocID
            ORDER BY kh.TenKhoaHoc");

        var dtLop = Database.Query(@"
            SELECT lh.LopHocID, lh.TenLop, lh.KhoaHocID
            FROM LopHoc lh
            ORDER BY lh.TenLop");

        var dtGv = Database.Query(@"
            SELECT pc.LopHocID, mh.TenMon, gv.HoTen AS TenGiaoVien
            FROM PhanCongGiangDay pc
            INNER JOIN MonHoc mh ON mh.MonHocID = pc.MonHocID
            INNER JOIN GiaoVien gv ON gv.GiaoVienID = pc.GiaoVienID
            ORDER BY mh.TenMon, gv.HoTen");

        var dtHv = Database.Query(@"
            SELECT hvl.LopHocID, hv.HoTen, tk.TenDangNhap, hvl.TrangThai
            FROM HocVien_Lop hvl
            INNER JOIN HocVien hv ON hv.HocVienID = hvl.HocVienID
            INNER JOIN TaiKhoan tk ON tk.TaiKhoanID = hv.TaiKhoanID
            ORDER BY hv.HoTen");

        var gvByLop = new Dictionary<int, List<TongHopGiaoVienLop>>();
        foreach (DataRow r in dtGv.Rows)
        {
            var lopId = Convert.ToInt32(r["LopHocID"]);
            if (!gvByLop.TryGetValue(lopId, out var list))
            {
                list = [];
                gvByLop[lopId] = list;
            }
            list.Add(new TongHopGiaoVienLop
            {
                TenMon = r["TenMon"]?.ToString() ?? "",
                TenGiaoVien = r["TenGiaoVien"]?.ToString() ?? ""
            });
        }

        var hvByLop = new Dictionary<int, List<TongHopHocVienLop>>();
        foreach (DataRow r in dtHv.Rows)
        {
            var lopId = Convert.ToInt32(r["LopHocID"]);
            if (!hvByLop.TryGetValue(lopId, out var list))
            {
                list = [];
                hvByLop[lopId] = list;
            }
            list.Add(new TongHopHocVienLop
            {
                HoTen = r["HoTen"]?.ToString() ?? "",
                TenDangNhap = r["TenDangNhap"]?.ToString() ?? "",
                TrangThai = r["TrangThai"]?.ToString() ?? ""
            });
        }

        var lopByKhoa = new Dictionary<int, List<TongHopLopHoc>>();
        foreach (DataRow r in dtLop.Rows)
        {
            var khoaId = Convert.ToInt32(r["KhoaHocID"]);
            var lopId = Convert.ToInt32(r["LopHocID"]);
            if (!lopByKhoa.TryGetValue(khoaId, out var list))
            {
                list = [];
                lopByKhoa[khoaId] = list;
            }
            list.Add(new TongHopLopHoc
            {
                LopHocID = lopId,
                TenLop = r["TenLop"]?.ToString() ?? "",
                GiaoViens = gvByLop.GetValueOrDefault(lopId, []),
                HocViens = hvByLop.GetValueOrDefault(lopId, [])
            });
        }

        var result = new List<TongHopKhoaHoc>();
        foreach (DataRow r in dtKhoa.Rows)
        {
            var khoaId = Convert.ToInt32(r["KhoaHocID"]);
            var khoa = new TongHopKhoaHoc
            {
                KhoaHocID = khoaId,
                TenKhoaHoc = r["TenKhoaHoc"]?.ToString() ?? "",
                TenMon = r["TenMon"]?.ToString() ?? "",
                TrangThai = r["TrangThai"]?.ToString() ?? "",
                LopHocs = lopByKhoa.GetValueOrDefault(khoaId, [])
            };

            if (key.Length == 0 || KhoaHopTuKhoaTongHop(khoa, key))
                result.Add(khoa);
        }

        return result;
    }

    private static bool KhoaHopTuKhoaTongHop(TongHopKhoaHoc khoa, string key)
    {
        if (ChuaChuoi(khoa.TenKhoaHoc, key) || ChuaChuoi(khoa.TenMon, key) || ChuaChuoi(TrangThaiKhoaHoc.Display(khoa.TrangThai), key))
            return true;

        foreach (var lop in khoa.LopHocs)
        {
            if (ChuaChuoi(lop.TenLop, key))
                return true;
            if (lop.GiaoViens.Any(g => ChuaChuoi(g.TenGiaoVien, key) || ChuaChuoi(g.TenMon, key)))
                return true;
            if (lop.HocViens.Any(h => ChuaChuoi(h.HoTen, key) || ChuaChuoi(h.TenDangNhap, key)))
                return true;
        }

        return false;
    }

    private static bool ChuaChuoi(string? haystack, string needle) =>
        (haystack ?? "").Contains(needle, StringComparison.OrdinalIgnoreCase);
}
