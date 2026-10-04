using System.Data;
using QLThiTN.WinForms.Models;

namespace QLThiTN.WinForms.Services;

/// <summary>Thong ke ket qua thi: theo ky thi, theo hoc vien, chat luong cau hoi.</summary>
public class StatisticsService
{
    public DataTable LayDanhSachDotThiChoThongKe() =>
        Database.Query(@"
            SELECT dt.DotThiID, dt.TenDotThi + N' — ' + d.TenDe AS HienThi
            FROM DotThi dt
            JOIN DeThi d ON d.DeThiID = dt.DeThiID
            ORDER BY dt.ThoiGianMoCong DESC");

    /// <summary>Ket qua chi tiet tung hoc vien trong dot thi.</summary>
    public List<KetQuaThi> LayKetQuaDotThi(int dotThiId) =>
        Database.ReadList(@"
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

    /// <summary>Ti le dung/sai tung cau hoi trong dot thi — do chat luong cau hoi.
    /// TracNghiem/DungSai: dung khi lua chon khop dap an; TraLoiNgan: dung khi DiemDatDuoc = toi da.</summary>
    public List<ThongKeCauHoi> LayChatLuongCauHoi(int dotThiId) =>
        Database.ReadList(@"
            SELECT dch.CauHoiID, dch.ThuTu, c.NoiDung,
                   COUNT(ct.ChiTietID) AS SoLanTraLoi,
                   SUM(CASE WHEN ct.DiemDatDuoc >= c.Diem THEN 1 ELSE 0 END) AS SoLanDung
            FROM DeThi_CauHoi dch
            JOIN CauHoi c ON c.CauHoiID = dch.CauHoiID
            LEFT JOIN ChiTietBaiLam ct ON ct.CauHoiID = dch.CauHoiID
                 AND ct.BaiLamID IN (SELECT BaiLamID FROM BaiLam WHERE DotThiID = @id AND TrangThai <> N'DangLam')
            WHERE dch.DeThiID = (SELECT DeThiID FROM DotThi WHERE DotThiID = @id)
            GROUP BY dch.CauHoiID, dch.ThuTu, c.NoiDung, c.Diem
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
    public (int CauHoi, int DeThi, int HocVien, int BaiThi) ThongKeTongQuan() =>
        (
            Database.ExecuteScalarInt("SELECT COUNT(1) FROM CauHoi WHERE TrangThai = N'HoatDong'"),
            Database.ExecuteScalarInt("SELECT COUNT(1) FROM DeThi"),
            Database.ExecuteScalarInt("SELECT COUNT(1) FROM HocVien"),
            Database.ExecuteScalarInt("SELECT COUNT(1) FROM BaiLam WHERE TrangThai <> N'DangLam'")
        );
}
