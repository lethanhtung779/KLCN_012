using System.Data;
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
        ten = ten.Trim();
        if (string.IsNullOrWhiteSpace(ten)) return (false, "Chưa nhập tên khóa học.", 0);
        if (monHocId <= 0) return (false, "Hãy chọn môn học.", 0);
        if (trangThai is not (TrangThaiKhoaHoc.HoatDong or TrangThaiKhoaHoc.TamDung or TrangThaiKhoaHoc.DaKetThuc))
            return (false, "Trạng thái không hợp lệ.", 0);

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
        ten = ten.Trim();
        if (id <= 0) return (false, "Khóa học không hợp lệ.");
        if (string.IsNullOrWhiteSpace(ten)) return (false, "Chưa nhập tên khóa học.");
        if (monHocId <= 0) return (false, "Hãy chọn môn học.");

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
        if (Database.ExecuteScalarInt("SELECT COUNT(1) FROM LopHoc WHERE KhoaHocID = @id", p => p.AddWithValue("@id", id)) > 0)
            return (false, "Không thể xóa: khóa học đã có lớp học. Hãy xóa các lớp trước.");
        Database.Execute("DELETE FROM KhoaHoc WHERE KhoaHocID = @id", p => p.AddWithValue("@id", id));
        return (true, "");
    }

    public DataTable LayDanhSachLopHoc(int? khoaHocId, string? tuKhoa)
    {
        var sql = @"
            SELECT lh.LopHocID, lh.TenLop, lh.KhoaHocID, kh.TenKhoaHoc, mh.TenMon,
                   (SELECT COUNT(1) FROM HocVien_Lop hvl WHERE hvl.LopHocID = lh.LopHocID AND hvl.TrangThai = N'DangHoc') AS SoHocVien
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
        tenLop = tenLop.Trim();
        if (string.IsNullOrWhiteSpace(tenLop)) return (false, "Chưa nhập tên lớp.");
        Database.Execute("UPDATE LopHoc SET TenLop = @ten, KhoaHocID = @kh WHERE LopHocID = @id",
            p => { p.AddWithValue("@ten", tenLop); p.AddWithValue("@kh", khoaHocId); p.AddWithValue("@id", id); });
        return (true, "");
    }

    public (bool Ok, string Error) XoaLopHoc(int id)
    {
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
        Database.Execute("DELETE FROM HocVien_Lop WHERE LopHocID = @lop AND HocVienID = @hv",
            p => { p.AddWithValue("@lop", lopHocId); p.AddWithValue("@hv", hocVienId); });
        return (true, "");
    }

    public (bool Ok, string Error) DoiTrangThaiHocVienLop(int lopHocId, int hocVienId, string trangThai)
    {
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
            WHERE NOT EXISTS (
                SELECT 1 FROM PhanCongGiangDay pc
                WHERE pc.LopHocID = @lop AND pc.MonHocID = mh.MonHocID)
            ORDER BY mh.TenMon",
            p => p.AddWithValue("@lop", lopHocId));

    public DataTable LayGiaoVienTheoMon(int monHocId) =>
        Database.Query(@"
            SELECT gv.GiaoVienID, gv.HoTen
            FROM GiaoVien gv
            INNER JOIN GiaoVien_MonHoc gvm ON gvm.GiaoVienID = gv.GiaoVienID
            WHERE gvm.MonHocID = @mon
            ORDER BY gv.HoTen",
            p => p.AddWithValue("@mon", monHocId));

    public DataTable LayTatCaGiaoVien() =>
        Database.Query("SELECT GiaoVienID, HoTen FROM GiaoVien ORDER BY HoTen");

    public (bool Ok, string Error) LuuPhanCong(int lopHocId, int monHocId, int giaoVienId, bool sua)
    {
        if (lopHocId <= 0 || monHocId <= 0 || giaoVienId <= 0)
            return (false, "Hãy chọn đủ lớp, môn và giáo viên.");

        // Giao vu phan cong lop -> tu dong ghi nhan GV duoc day mon do (neu chua co trong GiaoVien_MonHoc)
        Database.Execute(@"
            IF NOT EXISTS (SELECT 1 FROM GiaoVien_MonHoc WHERE GiaoVienID = @gv AND MonHocID = @mon)
                INSERT INTO GiaoVien_MonHoc (GiaoVienID, MonHocID) VALUES (@gv, @mon)",
            p => { p.AddWithValue("@gv", giaoVienId); p.AddWithValue("@mon", monHocId); });

        if (!sua && Database.ExecuteScalarInt(
                "SELECT COUNT(1) FROM PhanCongGiangDay WHERE LopHocID = @lop AND MonHocID = @mon",
                p => { p.AddWithValue("@lop", lopHocId); p.AddWithValue("@mon", monHocId); }) > 0)
            return (false, "Môn này đã có phân công cho lớp. Hãy sửa thay vì thêm mới.");

        if (sua)
        {
            Database.Execute(@"
                UPDATE PhanCongGiangDay SET GiaoVienID = @gv
                WHERE LopHocID = @lop AND MonHocID = @mon",
                p => { p.AddWithValue("@gv", giaoVienId); p.AddWithValue("@lop", lopHocId); p.AddWithValue("@mon", monHocId); });
        }
        else
        {
            Database.Execute(@"
                INSERT INTO PhanCongGiangDay (LopHocID, MonHocID, GiaoVienID)
                VALUES (@lop, @mon, @gv)",
                p => { p.AddWithValue("@lop", lopHocId); p.AddWithValue("@mon", monHocId); p.AddWithValue("@gv", giaoVienId); });
        }
        return (true, "");
    }

    public (bool Ok, string Error) XoaPhanCong(int lopHocId, int monHocId)
    {
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
        if (lopHocId <= 0) return (false, "Hãy chọn lớp.", 0);
        if (ketThuc <= batDau) return (false, "Thời gian kết thúc phải sau thời gian bắt đầu.", 0);

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
        if (ketThuc <= batDau) return (false, "Thời gian kết thúc phải sau thời gian bắt đầu.");
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
