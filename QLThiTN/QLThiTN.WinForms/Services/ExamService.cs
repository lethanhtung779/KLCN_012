using System.Data;
using QLThiTN.WinForms.Models;

namespace QLThiTN.WinForms.Services;

/// <summary>Quan ly de thi: tao tu dong / thu cong tu ngan hang cau hoi, quan ly dot thi.</summary>
public class ExamService
{
    public DataTable LayDanhSachDe()
    {
        return Database.Query(@"
            SELECT d.DeThiID, d.TenDe, m.TenMon, d.LoaiDe, d.SoLuongCauHoi, d.HinhThucTao,
                   COUNT(dt.DotThiID) AS SoDotThi
            FROM DeThi d
            JOIN MonHoc m ON m.MonHocID = d.MonHocID
            LEFT JOIN DotThi dt ON dt.DeThiID = d.DeThiID
            GROUP BY d.DeThiID, d.TenDe, m.TenMon, d.LoaiDe, d.SoLuongCauHoi, d.HinhThucTao
            ORDER BY d.DeThiID DESC");
    }

    public List<int> LayCauHoiCuaDe(int deThiId) =>
        Database.ReadList(
            "SELECT CauHoiID FROM DeThi_CauHoi WHERE DeThiID = @id ORDER BY ThuTu",
            r => r.GetInt32(0),
            p => p.AddWithValue("@id", deThiId));

    /// <summary>Tao de tu dong: chon ngau nhien so cau theo mon (+ do kho tuy chon)
    /// tu cac cau HoatDong. Tra ve DeThiID moi.</summary>
    public (int DeThiID, string Error) TaoDeTuDong(string tenDe, int monHocId, int soCau, string? mucDo, string loaiDe)
    {
        if (string.IsNullOrWhiteSpace(tenDe)) return (0, "Chưa nhập tên đề.");
        if (soCau < 1) return (0, "Số câu phải >= 1.");

        var pool = Database.ReadList(@"
            SELECT c.CauHoiID
            FROM CauHoi c
            JOIN ChuDe cd ON cd.ChuDeID = c.ChuDeID
            WHERE cd.MonHocID = @m AND c.TrangThai = N'HoatDong'
              AND (@muc = N'' OR c.MucDoKho = @muc)
            ORDER BY NEWID()",
            r => r.GetInt32(0),
            p =>
            {
                p.AddWithValue("@m", monHocId);
                p.AddWithValue("@muc", mucDo ?? "");
            });

        if (pool.Count < soCau)
            return (0, $"Ngân hàng chỉ còn {pool.Count} câu phù hợp, không đủ {soCau} câu.");

        var id = TaoDeCore(tenDe, monHocId, loaiDe, "TuDong", pool.Take(soCau).ToList());
        return (id, "");
    }

    /// <summary>Tao de thu cong tu danh sach cau hoi nguoi dung chon (giu dung thu tu).</summary>
    public (int DeThiID, string Error) TaoDeThuCong(string tenDe, int monHocId, string loaiDe, List<int> cauHoiIds)
    {
        if (string.IsNullOrWhiteSpace(tenDe)) return (0, "Chưa nhập tên đề.");
        if (cauHoiIds.Count == 0) return (0, "Chưa chọn câu hỏi nào cho đề.");
        var id = TaoDeCore(tenDe, monHocId, loaiDe, "ThuCong", cauHoiIds);
        return (id, "");
    }

    private int TaoDeCore(string tenDe, int monHocId, string loaiDe, string hinhThuc, List<int> cauHoiIds)
    {
        var id = Database.ExecuteScalarInt(@"
            INSERT INTO DeThi (MonHocID, GiaoVienID, TenDe, LoaiDe, SoLuongCauHoi, HinhThucTao)
            VALUES (@m, @gv, @t, @l, @s, @h);
            SELECT CAST(SCOPE_IDENTITY() AS INT);",
            p =>
            {
                p.AddWithValue("@m", monHocId);
                p.AddWithValue("@gv", Session.GiaoVienID);
                p.AddWithValue("@t", tenDe.Trim());
                p.AddWithValue("@l", loaiDe);
                p.AddWithValue("@s", cauHoiIds.Count);
                p.AddWithValue("@h", hinhThuc);
            });

        for (var i = 0; i < cauHoiIds.Count; i++)
        {
            var cau = i;
            Database.Execute(
                "INSERT INTO DeThi_CauHoi (DeThiID, CauHoiID, ThuTu) VALUES (@d, @c, @t)",
                p => { p.AddWithValue("@d", id); p.AddWithValue("@c", cauHoiIds[cau]); p.AddWithValue("@t", i + 1); });
        }

        // Tao ma de mac dinh "101" giong luong website
        Database.Execute("INSERT INTO MaDeThi (DeThiID, TenMaDe) VALUES (@d, N'101')",
            p => p.AddWithValue("@d", id));
        return id;
    }

    public void XoaDe(int deThiId)
    {
        Database.Execute(@"
            DELETE FROM LuaChonBaiLam WHERE ChiTietID IN (
                SELECT ct.ChiTietID FROM ChiTietBaiLam ct
                JOIN BaiLam bl ON bl.BaiLamID = ct.BaiLamID WHERE bl.DeThiID = @id);
            DELETE FROM ChiTietBaiLam WHERE BaiLamID IN (SELECT BaiLamID FROM BaiLam WHERE DeThiID = @id);
            DELETE FROM BaiLam WHERE DeThiID = @id;
            DELETE FROM DangKyDotThi WHERE DotThiID IN (SELECT DotThiID FROM DotThi WHERE DeThiID = @id);
            DELETE FROM DotThi WHERE DeThiID = @id;
            DELETE FROM MaDeThi WHERE DeThiID = @id;
            DELETE FROM DeThi_CauHoi WHERE DeThiID = @id;
            DELETE FROM DeThi WHERE DeThiID = @id;",
            p => p.AddWithValue("@id", deThiId));
    }

    // ------------------------------------------------------------------ dot thi
    public DataTable LayDanhSachDotThi(int? deThiId)
    {
        var sql = @"
            SELECT dt.DotThiID, dt.TenDotThi, d.TenDe, dt.ThoiGianMoCong, dt.ThoiGianDongCong,
                   dt.ThoiLuongLamBai, dt.GioiHanSoLuong, dt.CongBoDiemSom, dt.TrangThai,
                   dt.PhamVi, dt.SoLanThiToiDa,
                   (SELECT COUNT(1) FROM DangKyDotThi k WHERE k.DotThiID = dt.DotThiID AND k.TrangThai <> N'DaHuy') AS SoDangKy,
                   (SELECT COUNT(1) FROM BaiLam bl WHERE bl.DotThiID = dt.DotThiID AND bl.TrangThai <> N'DangLam') AS SoBaiDaNop
            FROM DotThi dt
            JOIN DeThi d ON d.DeThiID = dt.DeThiID
            WHERE (@de = 0 OR dt.DeThiID = @de)
            ORDER BY dt.ThoiGianMoCong DESC";
        return Database.Query(sql, p => p.AddWithValue("@de", deThiId ?? 0));
    }

    /// <summary>Tao dot thi moi. Trang thai duoc tinh tu thoi gian hien tai.</summary>
    public int TaoDotThi(DotThi d)
    {
        if (string.IsNullOrWhiteSpace(d.TenDotThi)) d.TenDotThi = "Đợt thi - " + d.TenDe;
        if (d.ThoiGianDongCong <= d.ThoiGianMoCong) throw new Exception("Giờ đóng công phải sau giờ mở công.");
        if (d.ThoiLuongLamBai <= 0) throw new Exception("Thời lượng làm bài phải > 0.");

        var trangThai = DateTime.Now < d.ThoiGianMoCong ? "SapMo"
            : DateTime.Now > d.ThoiGianDongCong ? "DaDong" : "DangMo";

        return Database.ExecuteScalarInt(@"
            INSERT INTO DotThi (DeThiID, TenDotThi, ThoiGianMoCong, ThoiGianDongCong, ThoiLuongLamBai,
                                GioiHanSoLuong, CongBoDiemSom, TrangThai, PhamVi, SoLanThiToiDa)
            VALUES (@de, @ten, @mo, @dong, @luong, @han, @cong, @tt, @pv, @lan);
            SELECT CAST(SCOPE_IDENTITY() AS INT);",
            p =>
            {
                p.AddWithValue("@de", d.DeThiID);
                p.AddWithValue("@ten", d.TenDotThi.Trim());
                p.AddWithValue("@mo", d.ThoiGianMoCong);
                p.AddWithValue("@dong", d.ThoiGianDongCong);
                p.AddWithValue("@luong", d.ThoiLuongLamBai);
                p.AddWithValue("@han", (object?)d.GioiHanSoLuong ?? DBNull.Value);
                p.AddWithValue("@cong", d.CongBoDiemSom);
                p.AddWithValue("@tt", trangThai);
                p.AddWithValue("@pv", d.PhamVi);
                p.AddWithValue("@lan", Math.Max(1, d.SoLanThiToiDa));
            });
    }

    public void CapNhatTrangThaiDot(int dotThiId, string trangThai) =>
        Database.Execute("UPDATE DotThi SET TrangThai = @t WHERE DotThiID = @id",
            p => { p.AddWithValue("@t", trangThai); p.AddWithValue("@id", dotThiId); });

    /// <summary>Dong cac dot thi da qua han (chay khi mo man hinh de thi).</summary>
    public void DongDotThiQuaHan() =>
        Database.Execute("UPDATE DotThi SET TrangThai = N'DaDong' WHERE ThoiGianDongCong < SYSDATETIME() AND TrangThai <> N'DaDong'");

    // ------------------------------------------------------------------ pool cau hoi cho tao de thu cong
    public DataTable LayCauHoiPool(int monHocId, string? mucDo) =>
        Database.Query(@"
            SELECT c.CauHoiID, c.NoiDung, c.LoaiCauHoi, c.MucDoKho, c.Diem, cd.TenChuDe
            FROM CauHoi c
            JOIN ChuDe cd ON cd.ChuDeID = c.ChuDeID
            WHERE cd.MonHocID = @m AND c.TrangThai = N'HoatDong'
              AND (@muc = N'' OR c.MucDoKho = @muc)
            ORDER BY c.CauHoiID",
            p =>
            {
                p.AddWithValue("@m", monHocId);
                p.AddWithValue("@muc", mucDo ?? "");
            });
}
