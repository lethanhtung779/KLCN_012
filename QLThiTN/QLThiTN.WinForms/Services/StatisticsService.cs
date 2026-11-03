using System.Data;
using QLThiTN.WinForms.Models;

namespace QLThiTN.WinForms.Services;

/// <summary>Thong ke ket qua thi: theo ky thi, theo hoc vien, chat luong cau hoi.</summary>
public class StatisticsService
{
    private static void KiemTraQuyenDotThi(int dotThiId)
    {
        if (!Session.IsGiaoVien) { Session.YeuCauQuanTriVien(); return; }
        if (!Session.IsAccountActive()) throw new UnauthorizedAccessException("Tài khoản giáo viên không còn hoạt động.");
        var coQuyen = Database.ExecuteScalarInt(@"
            SELECT COUNT(1) FROM DotThi dt
            JOIN DeThi d ON d.DeThiID = dt.DeThiID
            WHERE dt.DotThiID = @dt AND EXISTS (
                SELECT 1 FROM GiaoVien_MonHoc gm
                WHERE gm.GiaoVienID=@gv AND gm.MonHocID=d.MonHocID)",
            p =>
            {
                p.AddWithValue("@dt", dotThiId);
                p.AddWithValue("@gv", Session.GiaoVienID);
            }) > 0;
        if (!coQuyen)
            throw new UnauthorizedAccessException("Bạn không được phân công giảng dạy môn học của đợt thi này.");
    }

    public DataTable LayDanhSachDotThiChoThongKe()
    {
        if (Session.IsGiaoVien)
        {
            if (!Session.IsAccountActive()) throw new UnauthorizedAccessException("Tài khoản giáo viên không còn hoạt động.");
            return Database.Query(@"
                SELECT dt.DotThiID, dt.TenDotThi + N' — ' + d.TenDe AS HienThi
                FROM DotThi dt
                JOIN DeThi d ON d.DeThiID = dt.DeThiID
                WHERE EXISTS (
                    SELECT 1 FROM GiaoVien_MonHoc gm
                    WHERE gm.GiaoVienID=@gv AND gm.MonHocID=d.MonHocID)
                ORDER BY dt.ThoiGianMoCong DESC",
                p => p.AddWithValue("@gv", Session.GiaoVienID));
        }
        Session.YeuCauQuanTriVien();
        return Database.Query(@"
            SELECT dt.DotThiID, dt.TenDotThi + N' — ' + d.TenDe AS HienThi
            FROM DotThi dt
            JOIN DeThi d ON d.DeThiID = dt.DeThiID
            ORDER BY dt.ThoiGianMoCong DESC");
    }

    /// <summary>Ket qua chi tiet tung hoc vien trong dot thi (co the loc theo LopHocID).</summary>
    public List<KetQuaThi> LayKetQuaDotThi(int dotThiId, int? lopHocId = null)
    {
        KiemTraQuyenDotThi(dotThiId);
        if (lopHocId.HasValue && lopHocId.Value > 0)
        {
            return Database.ReadList(@"
                SELECT bl.BaiLamID, COALESCE(hv.HoTen, N'(khách)') AS HoTen, tk.TenDangNhap,
                       bl.LanThi, bl.TongDiem, bl.ThoiGianBatDau, bl.ThoiGianNop, bl.TrangThai
                FROM BaiLam bl
                LEFT JOIN HocVien hv ON hv.HocVienID = bl.HocVienID
                LEFT JOIN TaiKhoan tk ON tk.TaiKhoanID = hv.TaiKhoanID
                WHERE bl.DotThiID = @id AND bl.TrangThai <> N'DangLam'
                  AND EXISTS (SELECT 1 FROM HocVien_Lop hl WHERE hl.HocVienID = bl.HocVienID AND hl.LopHocID = @lop)
                ORDER BY bl.TongDiem DESC, bl.ThoiGianNop",
                r => new KetQuaThi
                {
                    BaiLamID = r.GetInt32(0),
                    HoTen = r.GetString(1),
                    TenDangNhap = r.IsDBNull(2) ? "" : r.GetString(2),
                    LanThi = r.GetInt32(3),
                    TongDiem = r.IsDBNull(4) ? null : r.GetDecimal(4),
                    ThoiGianBatDau = r.GetDateTime(5),
                    ThoiGianNop = r.IsDBNull(6) ? null : r.GetDateTime(6),
                    TrangThai = r.GetString(7)
                },
                p => { p.AddWithValue("@id", dotThiId); p.AddWithValue("@lop", lopHocId.Value); });
        }

        return Database.ReadList(@"
            SELECT bl.BaiLamID, COALESCE(hv.HoTen, N'(khách)') AS HoTen, tk.TenDangNhap,
                   bl.LanThi, bl.TongDiem, bl.ThoiGianBatDau, bl.ThoiGianNop, bl.TrangThai
            FROM BaiLam bl
            LEFT JOIN HocVien hv ON hv.HocVienID = bl.HocVienID
            LEFT JOIN TaiKhoan tk ON tk.TaiKhoanID = hv.TaiKhoanID
            WHERE bl.DotThiID = @id AND bl.TrangThai <> N'DangLam'
            ORDER BY bl.TongDiem DESC, bl.ThoiGianNop",
            r => new KetQuaThi
            {
                BaiLamID = r.GetInt32(0),
                HoTen = r.GetString(1),
                TenDangNhap = r.IsDBNull(2) ? "" : r.GetString(2),
                LanThi = r.GetInt32(3),
                TongDiem = r.IsDBNull(4) ? null : r.GetDecimal(4),
                ThoiGianBatDau = r.GetDateTime(5),
                ThoiGianNop = r.IsDBNull(6) ? null : r.GetDateTime(6),
                TrangThai = r.GetString(7)
            },
            p => p.AddWithValue("@id", dotThiId));
    }

    /// <summary>Danh sach lop hoc thuoc mon cua dot thi de loc ket qua theo lop.</summary>
    public DataTable LayDanhSachLopChoDotThi(int dotThiId)
    {
        return Database.Query(@"
            SELECT lh.LopHocID, CONCAT('LH', FORMAT(lh.LopHocID, '000000'), N' · ', lh.TenLop, N' — ', kh.TenKhoaHoc) AS TenHienThi
            FROM DotThi dt
            JOIN DeThi d ON d.DeThiID = dt.DeThiID
            JOIN KhoaHoc kh ON kh.MonHocID = d.MonHocID
            JOIN LopHoc lh ON lh.KhoaHocID = kh.KhoaHocID
            WHERE dt.DotThiID = @dt AND kh.TrangThai <> N'DaKetThuc'
            ORDER BY kh.TenKhoaHoc, lh.TenLop", p => p.AddWithValue("@dt", dotThiId));
    }

    /// <summary>Danh sach tat ca hoc vien de xem thong ke ca nhan.</summary>
    public DataTable LayDanhSachHocVienChoThongKe()
    {
        return Database.Query(@"
            SELECT hv.HocVienID, CONCAT(hv.HoTen, N' (', tk.TenDangNhap, N')') AS HienThi
            FROM HocVien hv
            JOIN TaiKhoan tk ON tk.TaiKhoanID = hv.TaiKhoanID
            WHERE tk.TrangThai = N'HoatDong'
            ORDER BY hv.HoTen");
    }

    /// <summary>Lich su thi tat ca cac dot cua mot hoc vien.</summary>
    public List<KetQuaThiHocVien> LayLichSuThiCuaHocVien(int hocVienId)
    {
        Session.YeuCauDangNhap();
        if (Session.IsGiaoVien)
        {
            if (!Session.IsAccountActive()) throw new UnauthorizedAccessException("Tài khoản giáo viên không còn hoạt động.");
            return Database.ReadList(@"
                SELECT bl.BaiLamID, dt.TenDotThi, d.TenDe, mh.TenMon, bl.LanThi, bl.TongDiem,
                       bl.ThoiGianBatDau, bl.ThoiGianNop, bl.TrangThai
                FROM BaiLam bl
                JOIN DotThi dt ON dt.DotThiID = bl.DotThiID
                JOIN DeThi d ON d.DeThiID = dt.DeThiID
                JOIN MonHoc mh ON mh.MonHocID = d.MonHocID
                WHERE bl.HocVienID = @hv AND bl.TrangThai <> N'DangLam'
                  AND EXISTS (SELECT 1 FROM GiaoVien_MonHoc gm WHERE gm.GiaoVienID = @gv AND gm.MonHocID = d.MonHocID)
                ORDER BY bl.ThoiGianNop DESC, bl.ThoiGianBatDau DESC",
                r => new KetQuaThiHocVien
                {
                    BaiLamID = r.GetInt32(0),
                    TenDotThi = r.GetString(1),
                    TenDe = r.GetString(2),
                    TenMon = r.GetString(3),
                    LanThi = r.GetInt32(4),
                    TongDiem = r.IsDBNull(5) ? null : r.GetDecimal(5),
                    ThoiGianBatDau = r.GetDateTime(6),
                    ThoiGianNop = r.IsDBNull(7) ? null : r.GetDateTime(7),
                    TrangThai = r.GetString(8)
                },
                p => { p.AddWithValue("@hv", hocVienId); p.AddWithValue("@gv", Session.GiaoVienID); });
        }

        Session.YeuCauQuanTriVien();
        return Database.ReadList(@"
            SELECT bl.BaiLamID, dt.TenDotThi, d.TenDe, mh.TenMon, bl.LanThi, bl.TongDiem,
                   bl.ThoiGianBatDau, bl.ThoiGianNop, bl.TrangThai
            FROM BaiLam bl
            JOIN DotThi dt ON dt.DotThiID = bl.DotThiID
            JOIN DeThi d ON d.DeThiID = dt.DeThiID
            JOIN MonHoc mh ON mh.MonHocID = d.MonHocID
            WHERE bl.HocVienID = @hv AND bl.TrangThai <> N'DangLam'
            ORDER BY bl.ThoiGianNop DESC, bl.ThoiGianBatDau DESC",
            r => new KetQuaThiHocVien
            {
                BaiLamID = r.GetInt32(0),
                TenDotThi = r.GetString(1),
                TenDe = r.GetString(2),
                TenMon = r.GetString(3),
                LanThi = r.GetInt32(4),
                TongDiem = r.IsDBNull(5) ? null : r.GetDecimal(5),
                ThoiGianBatDau = r.GetDateTime(6),
                ThoiGianNop = r.IsDBNull(7) ? null : r.GetDateTime(7),
                TrangThai = r.GetString(8)
            },
            p => p.AddWithValue("@hv", hocVienId));
    }

    /// <summary>Ti le dung/sai tung cau hoi trong dot thi — do chat luong cau hoi.
    /// TracNghiem/DungSai: dung khi lua chon khop dap an; TraLoiNgan: dung khi DiemDatDuoc = toi da.</summary>
    public List<ThongKeCauHoi> LayChatLuongCauHoi(int dotThiId)
    {
        KiemTraQuyenDotThi(dotThiId);
        return Database.ReadList(@"
            SELECT dch.CauHoiID, dch.ThuTu, c.NoiDung,
                   COUNT(ct.ChiTietID) AS SoLanTraLoi,
                   SUM(CASE WHEN ct.DiemDatDuoc >= CASE
                       WHEN c.LoaiCauHoi = N'DungSai' THEN 1.00
                       WHEN c.LoaiCauHoi = N'TraLoiNgan'
                           AND mh.TenMon COLLATE Latin1_General_100_CI_AI LIKE N'%Toan%' THEN 0.50
                       ELSE 0.25 END THEN 1 ELSE 0 END) AS SoLanDung
            FROM DeThi_CauHoi dch
            JOIN CauHoi c ON c.CauHoiID = dch.CauHoiID
            JOIN DeThi de ON de.DeThiID = dch.DeThiID
            JOIN MonHoc mh ON mh.MonHocID = de.MonHocID
            LEFT JOIN ChiTietBaiLam ct ON ct.CauHoiID = dch.CauHoiID
                 AND ct.BaiLamID IN (SELECT BaiLamID FROM BaiLam WHERE DotThiID = @id AND TrangThai <> N'DangLam')
            WHERE dch.DeThiID = (SELECT DeThiID FROM DotThi WHERE DotThiID = @id)
            GROUP BY dch.CauHoiID, dch.ThuTu, c.NoiDung
            ORDER BY dch.ThuTu",
            r => new ThongKeCauHoi
            {
                CauHoiID = r.GetInt32(0),
                ThuTu = r.GetInt32(1),
                NoiDung = r.GetString(2),
                SoLanTraLoi = r.GetInt32(3),
                SoLanDung = r.IsDBNull(4) ? 0 : r.GetInt32(4)
            },
            p => p.AddWithValue("@id", dotThiId));
    }

    /// <summary>Phan bo diem (so bai theo khoang diem 0-10, buoc 1 diem).</summary>
    public Dictionary<string, int> PhanBoDiem(List<KetQuaThi> ketQua)
    {
        var buckets = new Dictionary<string, int>();
        for (var i = 0; i < 10; i++) buckets[$"{i}–{i + 1}"] = 0;

        foreach (var k in ketQua)
        {
            if (k.TongDiem == null) continue;
            var idx = (int)Math.Floor(k.TongDiem.Value);
            if (idx < 0) idx = 0;
            if (idx > 9) idx = 9;
            var key = buckets.Keys.ElementAt(idx);
            buckets[key]++;
        }
        return buckets;
    }

    /// <summary>Thong ke tong quan toan he thong (cho man hinh chinh).</summary>
    public (int CauHoi, int DeThi, int HocVien, int BaiThi) ThongKeTongQuan()
    {
        // Lấy cả bốn số liệu trong cùng một kết nối. Cách cũ mở bốn kết nối
        // liên tiếp nên dashboard dễ bị timeout khi SQL Server vừa khởi động
        // hoặc đang chờ một khóa schema ngắn hạn.
        var row = Database.Query(@"
            SELECT
                (SELECT COUNT_BIG(1) FROM CauHoi WHERE TrangThai=N'HoatDong') AS CauHoi,
                (SELECT COUNT_BIG(1) FROM DeThi) AS DeThi,
                (SELECT COUNT_BIG(1) FROM HocVien) AS HocVien,
                (SELECT COUNT_BIG(1) FROM BaiLam WHERE TrangThai<>N'DangLam') AS BaiThi").Rows[0];
        return (Convert.ToInt32(row["CauHoi"]), Convert.ToInt32(row["DeThi"]),
                Convert.ToInt32(row["HocVien"]), Convert.ToInt32(row["BaiThi"]));
    }
}
