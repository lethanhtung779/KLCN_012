using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using QLThiTN.WinForms.Models;

namespace QLThiTN.WinForms.Services;

/// <summary>Quan ly de thi: tao tu dong / thu cong tu ngan hang cau hoi, quan ly dot thi.</summary>
public class ExamService(int? giaoVienPhuTrachId = null)
{
    private static string XoaDauTiengViet(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D');
    }

    private static readonly Regex[] ReDapAnCuoi =
    [
        new(@"^tat ca\s+(cac\s+)?(dap an|phuong an|cau|y)?\s*(tren|neu tren|ke tren)?\s*deu\s+(dung|sai|chinh xac)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^tat ca\s+(cac\s+)?(dap an|phuong an|cau|y)\s+(tren|neu tren|ke tren)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^tat ca\s+deu\s+(dung|sai|chinh xac)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^ca\s+(([a-d](\s*,\s*|\s+va\s*))+[a-d]|ba|hai|3|2)\s+(dap an|phuong an|cau|y)?\s*(tren|neu tren)?\s*(deu\s+)?(dung|sai|chinh xac)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^khong co\s+(dap an|phuong an|cau|y)\s+nao\s+(dung|sai|chinh xac)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    ];

    /// <summary>
    /// Kiểm tra xem một phương án có phải là loại đáp án phụ thuộc thứ tự
    /// (như "Tất cả các đáp án trên đều đúng/sai", "Cả A và B...", "Không có đáp án nào đúng"...)
    /// cần được cố định ở vị trí cuối cùng (D) khi trộn đề hay không.
    /// </summary>
    public static bool LaDapAnCoDinhCuoi(string? noiDung)
    {
        if (string.IsNullOrWhiteSpace(noiDung)) return false;
        var clean = Regex.Replace(noiDung, "<.*?>", string.Empty);
        clean = clean.Trim().TrimEnd('.', ';', ':', '!', ',').ToLowerInvariant();
        clean = Regex.Replace(clean, @"\s+", " ");
        var nonAccent = XoaDauTiengViet(clean);
        foreach (var re in ReDapAnCuoi)
        {
            if (re.IsMatch(nonAccent)) return true;
        }
        return false;
    }

    public static void YeuCauQuanTriVien()
    {
        if (!Session.IsQuanTriVien)
            throw new UnauthorizedAccessException("Chỉ Admin được quản lý đề thi và kỳ thi.");
    }

    private int GiaoVienPhuTrach()
    {
        YeuCauQuanTriVien();
        if (giaoVienPhuTrachId is not > 0)
            throw new InvalidOperationException("Hãy chọn giáo viên phụ trách đề thi.");
        var exists = Database.ExecuteScalarInt(@"SELECT COUNT(*) FROM GiaoVien gv
            JOIN TaiKhoan tk ON tk.TaiKhoanID = gv.TaiKhoanID
            WHERE gv.GiaoVienID = @id AND tk.TrangThai = N'HoatDong' AND tk.VaiTro = N'GiaoVien'",
            p => p.AddWithValue("@id", giaoVienPhuTrachId.Value));
        if (exists == 0) throw new InvalidOperationException("Giáo viên phụ trách không tồn tại hoặc tài khoản đã bị khóa.");
        return giaoVienPhuTrachId.Value;
    }

    public DataTable LayDanhSachDe()
    {
        return Database.Query(@"
            SELECT d.DeThiID, d.TenDe, m.TenMon, d.LoaiDe, d.SoLuongCauHoi, d.HinhThucTao,
                   d.LaMienPhi,
                   CASE WHEN d.LaMienPhi = 1 THEN N'Miễn phí' ELSE N'Đóng phí (VIP)' END AS QuyenThi,
                   COUNT(dt.DotThiID) AS SoDotThi,
                   (SELECT COUNT(*) FROM MaDeThi md WHERE md.DeThiID = d.DeThiID) AS SoMaDe
            FROM DeThi d
            JOIN MonHoc m ON m.MonHocID = d.MonHocID
            LEFT JOIN DotThi dt ON dt.DeThiID = d.DeThiID
            GROUP BY d.DeThiID, d.TenDe, m.TenMon, d.LoaiDe, d.SoLuongCauHoi, d.HinhThucTao, d.LaMienPhi
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
        YeuCauQuanTriVien();
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
        YeuCauQuanTriVien();
        if (string.IsNullOrWhiteSpace(tenDe)) return (0, "Chưa nhập tên đề.");
        if (cauHoiIds.Count == 0) return (0, "Chưa chọn câu hỏi nào cho đề.");
        var id = TaoDeCore(tenDe, monHocId, loaiDe, "ThuCong", cauHoiIds);
        return (id, "");
    }

    private int TaoDeCore(string tenDe, int monHocId, string loaiDe, string hinhThuc, List<int> cauHoiIds)
    {
        if (cauHoiIds.Distinct().Count() != cauHoiIds.Count)
            throw new InvalidOperationException("Danh sách câu hỏi trong đề không được trùng lặp.");

        var gvId = GiaoVienPhuTrach();
        using var conn = Database.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            using var cmd1 = new SqlCommand(@"
                INSERT INTO DeThi (MonHocID, GiaoVienID, TenDe, LoaiDe, SoLuongCauHoi, HinhThucTao)
                VALUES (@m, @gv, @t, @l, @s, @h);
                SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx);
            cmd1.Parameters.AddWithValue("@m", monHocId);
            cmd1.Parameters.AddWithValue("@gv", gvId);
            cmd1.Parameters.AddWithValue("@t", tenDe.Trim());
            cmd1.Parameters.AddWithValue("@l", loaiDe);
            cmd1.Parameters.AddWithValue("@s", cauHoiIds.Count);
            cmd1.Parameters.AddWithValue("@h", hinhThuc);
            var id = Convert.ToInt32(cmd1.ExecuteScalar() ?? 0);

            for (var i = 0; i < cauHoiIds.Count; i++)
            {
                using var cmd2 = new SqlCommand(
                    "INSERT INTO DeThi_CauHoi (DeThiID, CauHoiID, ThuTu) VALUES (@d, @c, @t)", conn, tx);
                cmd2.Parameters.AddWithValue("@d", id);
                cmd2.Parameters.AddWithValue("@c", cauHoiIds[i]);
                cmd2.Parameters.AddWithValue("@t", i + 1);
                cmd2.ExecuteNonQuery();
            }

            // Tao ma de mac dinh "101" giong luong website
            using var cmd3 = new SqlCommand(
                "INSERT INTO MaDeThi (DeThiID, TenMaDe) VALUES (@d, N'101')", conn, tx);
                cmd3.Parameters.AddWithValue("@d", id);
            cmd3.ExecuteNonQuery();

            tx.Commit();
            return id;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>Danh dau de mo cho tai khoan mien phi, hoac chi danh cho tai khoan dong phi.</summary>
    public void DatMienPhi(int deThiId, bool mienPhi)
    {
        YeuCauQuanTriVien();
        Database.Execute("UPDATE DeThi SET LaMienPhi = @v WHERE DeThiID = @id",
            p =>
            {
                p.AddWithValue("@v", mienPhi);
                p.AddWithValue("@id", deThiId);
            });
    }

    public void XoaDe(int deThiId)
    {
        YeuCauQuanTriVien();
        var (coBaiLam, soBaiLam) = KiemTraBaiLamCuaDe(deThiId);
        if (coBaiLam)
            throw new InvalidOperationException($"Không thể xóa đề thi này vì đã có {soBaiLam} bài thi của thí sinh. Hãy đóng đợt thi thay vì xóa.");

        using var conn = Database.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            using var cmd = new SqlCommand(@"
                DELETE FROM DotThi_LopHoc WHERE DotThiID IN (SELECT DotThiID FROM DotThi WHERE DeThiID = @id);
                DELETE FROM DangKyDotThi WHERE DotThiID IN (SELECT DotThiID FROM DotThi WHERE DeThiID = @id);
                DELETE FROM DotThi WHERE DeThiID = @id;
                DELETE FROM MaDe_DapAn WHERE MaDeID IN (SELECT MaDeID FROM MaDeThi WHERE DeThiID = @id);
                DELETE FROM MaDe_CauHoi WHERE MaDeID IN (SELECT MaDeID FROM MaDeThi WHERE DeThiID = @id);
                DELETE FROM MaDeThi WHERE DeThiID = @id;
                DELETE FROM DeThi_CauHoi WHERE DeThiID = @id;
                DELETE FROM DeThi WHERE DeThiID = @id;", conn, tx);
            cmd.Parameters.AddWithValue("@id", deThiId);
            cmd.ExecuteNonQuery();
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
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
        YeuCauQuanTriVien();
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

    public void CapNhatTrangThaiDot(int dotThiId, string trangThai)
    {
        YeuCauQuanTriVien();
        Database.Execute("UPDATE DotThi SET TrangThai = @t WHERE DotThiID = @id",
            p => { p.AddWithValue("@t", trangThai); p.AddWithValue("@id", dotThiId); });
    }

    /// <summary>Dong cac dot thi da qua han (chay khi mo man hinh de thi).</summary>
    public void DongDotThiQuaHan()
    {
        YeuCauQuanTriVien();
        Database.Execute("UPDATE DotThi SET TrangThai = N'DaDong' WHERE ThoiGianDongCong < SYSDATETIME() AND TrangThai <> N'DaDong'");
    }

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

    // ------------------------------------------------------------------ TRỘN ĐỀ THI CHUẨN THPT QUỐC GIA
    public (bool CoBaiLam, int SoBaiLam) KiemTraBaiLamCuaDe(int deThiId)
    {
        var soBai = Database.ExecuteScalarInt(@"
            SELECT COUNT(1) FROM BaiLam bl
            JOIN DotThi dt ON dt.DotThiID = bl.DotThiID
            WHERE dt.DeThiID = @id",
            p => p.AddWithValue("@id", deThiId));
        return (soBai > 0, soBai);
    }

    public DataTable LayDanhSachMaDe(int deThiId)
    {
        return Database.Query(@"
            SELECT m.MaDeID, m.TenMaDe,
                   COUNT(DISTINCT mc.CauHoiID) AS SoCauHoi,
                   (SELECT COUNT(1) FROM BaiLam bl WHERE bl.MaDeID = m.MaDeID) AS SoBaiLam
            FROM MaDeThi m
            LEFT JOIN MaDe_CauHoi mc ON mc.MaDeID = m.MaDeID
            WHERE m.DeThiID = @id
            GROUP BY m.MaDeID, m.TenMaDe
            ORDER BY m.TenMaDe",
            p => p.AddWithValue("@id", deThiId));
    }

    public (int TongCau, int SoTracNghiem, int SoDungSai, int SoTraLoiNgan) LayThongKePhanCauHoi(int deThiId)
    {
        var dt = Database.Query(@"
            SELECT c.LoaiCauHoi, COUNT(1) AS SoLuong
            FROM DeThi_CauHoi dc
            JOIN CauHoi c ON c.CauHoiID = dc.CauHoiID
            WHERE dc.DeThiID = @id
            GROUP BY c.LoaiCauHoi",
            p => p.AddWithValue("@id", deThiId));

        int tn = 0, ds = 0, tln = 0;
        foreach (DataRow r in dt.Rows)
        {
            var loai = r["LoaiCauHoi"]?.ToString() ?? "";
            var sl = Convert.ToInt32(r["SoLuong"]);
            if (loai == "TracNghiem") tn += sl;
            else if (loai == "DungSai") ds += sl;
            else if (loai == "TraLoiNgan") tln += sl;
            else tn += sl;
        }
        return (tn + ds + tln, tn, ds, tln);
    }

    public (bool Success, string Message, List<int> MaDeIds) TronDeThi(
        int deThiId,
        int soLuongMaDe,
        int maDeBatDau = 101,
        bool xoaCu = true,
        bool xoaBaiLamCu = false,
        bool ghimDapAnCuoi = true)
    {
        YeuCauQuanTriVien();
        if (soLuongMaDe < 1 || soLuongMaDe > 50)
            return (false, "Số lượng mã đề phải từ 1 đến 50.", new());

        var (coBai, soBai) = KiemTraBaiLamCuaDe(deThiId);
        if (coBai)
        {
            if (xoaCu && !xoaBaiLamCu)
            {
                return (false, $"Đề thi đã có {soBai} bài làm của thí sinh. Hãy chọn 'Sinh thêm mã đề mới' hoặc chọn 'Làm mới & Xóa bài làm cũ'.", new());
            }

            if (xoaCu && xoaBaiLamCu)
            {
                // Xóa bài làm cũ trước để không vi phạm FK
                XoaBaiLamCuaDe(deThiId);
            }
        }

        // Lấy danh sách câu hỏi gốc của đề
        var dtCauHoi = Database.Query(@"
            SELECT dc.CauHoiID, dc.ThuTu AS ThuTuGoc, c.ChuDeID, c.LoaiCauHoi, c.MucDoKho, c.NoiDung, c.Diem
            FROM DeThi_CauHoi dc
            JOIN CauHoi c ON c.CauHoiID = dc.CauHoiID
            WHERE dc.DeThiID = @id
            ORDER BY dc.ThuTu",
            p => p.AddWithValue("@id", deThiId));

        if (dtCauHoi.Rows.Count == 0)
            return (false, "Đề thi chưa có câu hỏi nào để trộn.", new());

        // Lấy danh sách đáp án gốc
        var dtDapAn = Database.Query(@"
            SELECT d.DapAnID, d.CauHoiID, d.ThuTu, d.NoiDung, d.LaDapAnDung, d.GiaiThich
            FROM DapAn d
            JOIN DeThi_CauHoi dc ON dc.CauHoiID = d.CauHoiID
            WHERE dc.DeThiID = @id
            ORDER BY d.CauHoiID, d.ThuTu",
            p => p.AddWithValue("@id", deThiId));

        var dapAnByCauHoi = new Dictionary<int, List<DapAnItem>>();
        foreach (DataRow r in dtDapAn.Rows)
        {
            var chId = Convert.ToInt32(r["CauHoiID"]);
            if (!dapAnByCauHoi.TryGetValue(chId, out var list))
            {
                list = new List<DapAnItem>();
                dapAnByCauHoi[chId] = list;
            }
            list.Add(new DapAnItem
            {
                DapAnID = Convert.ToInt32(r["DapAnID"]),
                CauHoiID = chId,
                ThuTuGoc = Convert.ToInt32(r["ThuTu"]),
                NoiDung = r["NoiDung"]?.ToString() ?? "",
                LaDapAnDung = Convert.ToBoolean(r["LaDapAnDung"]),
                GiaiThich = r["GiaiThich"]?.ToString()
            });
        }

        // Tách câu hỏi thành 3 nhóm chuẩn đề thi THPT QG
        var phan1 = new List<CauHoiItem>(); // Phần I: Trắc nghiệm 4 lựa chọn A-D
        var phan2 = new List<CauHoiItem>(); // Phần II: Trắc nghiệm Đúng / Sai 4 ý a-d
        var phan3 = new List<CauHoiItem>(); // Phần III: Trắc nghiệm Trả lời ngắn

        foreach (DataRow r in dtCauHoi.Rows)
        {
            var chId = Convert.ToInt32(r["CauHoiID"]);
            var item = new CauHoiItem
            {
                CauHoiID = chId,
                ThuTuGoc = Convert.ToInt32(r["ThuTuGoc"]),
                ChuDeID = Convert.ToInt32(r["ChuDeID"]),
                LoaiCauHoi = r["LoaiCauHoi"]?.ToString() ?? "TracNghiem",
                MucDoKho = r["MucDoKho"]?.ToString() ?? "ChuaXacDinh",
                NoiDung = r["NoiDung"]?.ToString() ?? "",
                Diem = Convert.ToDecimal(r["Diem"]),
                DapAns = dapAnByCauHoi.GetValueOrDefault(chId, new())
            };

            if (item.LoaiCauHoi == "TracNghiem")
                phan1.Add(item);
            else if (item.LoaiCauHoi == "DungSai")
                phan2.Add(item);
            else
                phan3.Add(item);
        }

        var maDeIds = new List<int>();

        using var conn = Database.Open();
        using var tran = conn.BeginTransaction();
        try
        {
            if (xoaCu)
            {
                using var cmdXoa = conn.CreateCommand();
                cmdXoa.Transaction = tran;
                cmdXoa.CommandText = @"
                    DELETE FROM MaDe_DapAn WHERE MaDeID IN (SELECT MaDeID FROM MaDeThi WHERE DeThiID = @id);
                    DELETE FROM MaDe_CauHoi WHERE MaDeID IN (SELECT MaDeID FROM MaDeThi WHERE DeThiID = @id);
                    DELETE FROM MaDeThi WHERE DeThiID = @id;";
                cmdXoa.Parameters.AddWithValue("@id", deThiId);
                cmdXoa.ExecuteNonQuery();
            }

            for (int k = 0; k < soLuongMaDe; k++)
            {
                var tenMaDe = (maDeBatDau + k).ToString();

                // Kiểm tra nếu tên mã đề đã tồn tại trong đề này (trường hợp không xóa cũ)
                using var cmdCheck = conn.CreateCommand();
                cmdCheck.Transaction = tran;
                cmdCheck.CommandText = "SELECT MaDeID FROM MaDeThi WHERE DeThiID = @de AND TenMaDe = @ten";
                cmdCheck.Parameters.AddWithValue("@de", deThiId);
                cmdCheck.Parameters.AddWithValue("@ten", tenMaDe);
                var existingId = cmdCheck.ExecuteScalar();

                int maDeId;
                if (existingId != null && existingId != DBNull.Value)
                {
                    if (!xoaCu)
                    {
                        tran.Rollback();
                        return (false, $"Mã đề {tenMaDe} đã tồn tại. Hãy chọn mã bắt đầu khác để sinh thêm.", new());
                    }
                    maDeId = Convert.ToInt32(existingId);
                    using var cmdDel = conn.CreateCommand();
                    cmdDel.Transaction = tran;
                    cmdDel.CommandText = @"
                        DELETE FROM MaDe_DapAn WHERE MaDeID = @m;
                        DELETE FROM MaDe_CauHoi WHERE MaDeID = @m;";
                    cmdDel.Parameters.AddWithValue("@m", maDeId);
                    cmdDel.ExecuteNonQuery();
                }
                else
                {
                    using var cmdIns = conn.CreateCommand();
                    cmdIns.Transaction = tran;
                    cmdIns.CommandText = @"
                        INSERT INTO MaDeThi (DeThiID, TenMaDe) VALUES (@de, @ten);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    cmdIns.Parameters.AddWithValue("@de", deThiId);
                    cmdIns.Parameters.AddWithValue("@ten", tenMaDe);
                    maDeId = Convert.ToInt32(cmdIns.ExecuteScalar());
                }

                maDeIds.Add(maDeId);

                // Mỗi lần trộn cần một chuỗi ngẫu nhiên mới; seed cố định theo ID
                // khiến bấm "Làm mới" luôn tạo lại đúng cùng thứ tự cũ.
                var rng = new Random(Random.Shared.Next());

                // Giữ ba phần; trong mỗi phần gom câu theo chủ đề (mạch kiến thức),
                // sau đó theo mức độ. Chỉ hoán vị câu trong cùng một ô ma trận.
                var p1Shuffled = TronTheoChuDeVaMucDo(phan1, rng);
                var p2Shuffled = TronTheoChuDeVaMucDo(phan2, rng);
                var p3Shuffled = TronTheoChuDeVaMucDo(phan3, rng);

                var allQuestions = new List<CauHoiItem>();
                allQuestions.AddRange(p1Shuffled);
                allQuestions.AddRange(p2Shuffled);
                allQuestions.AddRange(p3Shuffled);

                // Đánh số thứ tự câu hỏi tăng dần từ 1..N
                int orderIndex = 1;
                foreach (var q in allQuestions)
                {
                    using var cmdQ = conn.CreateCommand();
                    cmdQ.Transaction = tran;
                    cmdQ.CommandText = "INSERT INTO MaDe_CauHoi (MaDeID, CauHoiID, ThuTu) VALUES (@m, @c, @t)";
                    cmdQ.Parameters.AddWithValue("@m", maDeId);
                    cmdQ.Parameters.AddWithValue("@c", q.CauHoiID);
                    cmdQ.Parameters.AddWithValue("@t", orderIndex);
                    cmdQ.ExecuteNonQuery();

                    // Xử lý đảo đáp án theo loại câu hỏi
                    if (q.LoaiCauHoi == "TracNghiem")
                    {
                        List<DapAnItem> daShuffled;
                        if (ghimDapAnCuoi)
                        {
                            // Tự động ghim các đáp án phụ thuộc thứ tự ("Tất cả...", "Cả A và B...", "Không có...") ở vị trí cuối (D)
                            var anchors = q.DapAns.Where(d => LaDapAnCoDinhCuoi(d.NoiDung)).ToList();
                            var nonAnchors = q.DapAns.Where(d => !LaDapAnCoDinhCuoi(d.NoiDung)).OrderBy(_ => rng.Next()).ToList();
                            nonAnchors.AddRange(anchors);
                            daShuffled = nonAnchors;
                        }
                        else
                        {
                            daShuffled = q.DapAns.OrderBy(_ => rng.Next()).ToList();
                        }

                        for (int dIdx = 0; dIdx < daShuffled.Count; dIdx++)
                        {
                            using var cmdD = conn.CreateCommand();
                            cmdD.Transaction = tran;
                            cmdD.CommandText = "INSERT INTO MaDe_DapAn (MaDeID, CauHoiID, DapAnID, ThuTu) VALUES (@m, @c, @d, @t)";
                            cmdD.Parameters.AddWithValue("@m", maDeId);
                            cmdD.Parameters.AddWithValue("@c", q.CauHoiID);
                            cmdD.Parameters.AddWithValue("@d", daShuffled[dIdx].DapAnID);
                            cmdD.Parameters.AddWithValue("@t", dIdx);
                            cmdD.ExecuteNonQuery();
                        }
                    }
                    else if (q.LoaiCauHoi == "DungSai")
                    {
                        List<DapAnItem> daShuffled;
                        if (ghimDapAnCuoi)
                        {
                            var anchors = q.DapAns.Where(d => LaDapAnCoDinhCuoi(d.NoiDung)).ToList();
                            var nonAnchors = q.DapAns.Where(d => !LaDapAnCoDinhCuoi(d.NoiDung)).OrderBy(_ => rng.Next()).ToList();
                            nonAnchors.AddRange(anchors);
                            daShuffled = nonAnchors;
                        }
                        else
                        {
                            daShuffled = q.DapAns.OrderBy(_ => rng.Next()).ToList();
                        }

                        for (int dIdx = 0; dIdx < daShuffled.Count; dIdx++)
                        {
                            using var cmdD = conn.CreateCommand();
                            cmdD.Transaction = tran;
                            cmdD.CommandText = "INSERT INTO MaDe_DapAn (MaDeID, CauHoiID, DapAnID, ThuTu) VALUES (@m, @c, @d, @t)";
                            cmdD.Parameters.AddWithValue("@m", maDeId);
                            cmdD.Parameters.AddWithValue("@c", q.CauHoiID);
                            cmdD.Parameters.AddWithValue("@d", daShuffled[dIdx].DapAnID);
                            cmdD.Parameters.AddWithValue("@t", dIdx);
                            cmdD.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Trả lời ngắn: giữ nguyên thứ tự đáp án
                        for (int dIdx = 0; dIdx < q.DapAns.Count; dIdx++)
                        {
                            using var cmdD = conn.CreateCommand();
                            cmdD.Transaction = tran;
                            cmdD.CommandText = "INSERT INTO MaDe_DapAn (MaDeID, CauHoiID, DapAnID, ThuTu) VALUES (@m, @c, @d, @t)";
                            cmdD.Parameters.AddWithValue("@m", maDeId);
                            cmdD.Parameters.AddWithValue("@c", q.CauHoiID);
                            cmdD.Parameters.AddWithValue("@d", q.DapAns[dIdx].DapAnID);
                            cmdD.Parameters.AddWithValue("@t", dIdx);
                            cmdD.ExecuteNonQuery();
                        }
                    }

                    orderIndex++;
                }
            }

            tran.Commit();
            return (true, $"Đã trộn và sinh thành công {soLuongMaDe} mã đề (từ {maDeBatDau} đến {maDeBatDau + soLuongMaDe - 1}) theo chuẩn đề thi THPT Quốc gia.", maDeIds);
        }
        catch (Exception ex)
        {
            tran.Rollback();
            return (false, "Lỗi khi trộn đề: " + ex.Message, new());
        }
    }

    public DataTable LayMaTranDapAn(int deThiId)
    {
        var dtMaDe = LayDanhSachMaDe(deThiId);
        var table = new DataTable();
        table.Columns.Add("ThuTu", typeof(int));
        table.Columns.Add("LoaiCauHoi", typeof(string));

        var maDes = new List<(int MaDeID, string TenMaDe)>();
        foreach (DataRow r in dtMaDe.Rows)
        {
            var id = Convert.ToInt32(r["MaDeID"]);
            var ten = r["TenMaDe"]?.ToString() ?? "";
            maDes.Add((id, ten));
            table.Columns.Add(ten, typeof(string));
        }

        if (maDes.Count == 0) return table;

        var sql = @"
            SELECT mc.MaDeID, mc.ThuTu AS ThuTuCau, mc.CauHoiID, c.LoaiCauHoi,
                   md.DapAnID, md.ThuTu AS ThuTuDapAn, d.LaDapAnDung, d.NoiDung
            FROM MaDe_CauHoi mc
            JOIN CauHoi c ON c.CauHoiID = mc.CauHoiID
            JOIN MaDeThi m ON m.MaDeID = mc.MaDeID
            LEFT JOIN MaDe_DapAn md ON md.MaDeID = mc.MaDeID AND md.CauHoiID = mc.CauHoiID
            LEFT JOIN DapAn d ON d.DapAnID = md.DapAnID
            WHERE m.DeThiID = @id
            ORDER BY mc.MaDeID, mc.ThuTu, md.ThuTu";

        var raw = Database.Query(sql, p => p.AddWithValue("@id", deThiId));

        var matrix = new Dictionary<(int MaDeID, int ThuTuCau), (string Loai, string Answer)>();
        var totalQuestions = 0;

        var grouped = raw.AsEnumerable().GroupBy(r => new
        {
            MaDeID = Convert.ToInt32(r["MaDeID"]),
            ThuTuCau = Convert.ToInt32(r["ThuTuCau"]),
            Loai = r["LoaiCauHoi"]?.ToString() ?? "TracNghiem"
        });

        foreach (var g in grouped)
        {
            if (g.Key.ThuTuCau > totalQuestions) totalQuestions = g.Key.ThuTuCau;

            string ans = "";
            if (g.Key.Loai == "TracNghiem")
            {
                var right = g.FirstOrDefault(x => x["LaDapAnDung"] != DBNull.Value && Convert.ToBoolean(x["LaDapAnDung"]));
                if (right != null && right["ThuTuDapAn"] != DBNull.Value)
                {
                    var idx = Convert.ToInt32(right["ThuTuDapAn"]);
                    ans = ((char)('A' + idx)).ToString();
                }
            }
            else if (g.Key.Loai == "DungSai")
            {
                var parts = new List<string>();
                foreach (var y in g.OrderBy(x => Convert.ToInt32(x["ThuTuDapAn"])))
                {
                    if (y["ThuTuDapAn"] != DBNull.Value && y["LaDapAnDung"] != DBNull.Value)
                    {
                        var idx = Convert.ToInt32(y["ThuTuDapAn"]);
                        var label = ((char)('a' + idx)).ToString();
                        var dung = Convert.ToBoolean(y["LaDapAnDung"]) ? "Đ" : "S";
                        parts.Add($"{label}:{dung}");
                    }
                }
                ans = string.Join(" ", parts);
            }
            else
            {
                var right = g.FirstOrDefault(x => x["LaDapAnDung"] != DBNull.Value && Convert.ToBoolean(x["LaDapAnDung"]));
                ans = right?["NoiDung"]?.ToString() ?? "";
            }

            matrix[(g.Key.MaDeID, g.Key.ThuTuCau)] = (g.Key.Loai, ans);
        }

        for (int q = 1; q <= totalQuestions; q++)
        {
            var row = table.NewRow();
            row["ThuTu"] = q;

            string loaiDisplay = "";
            foreach (var md in maDes)
            {
                if (matrix.TryGetValue((md.MaDeID, q), out var val))
                {
                    row[md.TenMaDe] = val.Answer;
                    if (string.IsNullOrEmpty(loaiDisplay))
                    {
                        loaiDisplay = val.Loai switch
                        {
                            "TracNghiem" => "Phần I (TN)",
                            "DungSai" => "Phần II (Đ/S)",
                            "TraLoiNgan" => "Phần III (TLN)",
                            _ => val.Loai
                        };
                    }
                }
            }
            row["LoaiCauHoi"] = loaiDisplay;
            table.Rows.Add(row);
        }

        return table;
    }

    public DataTable LayChiTietMaDe(int maDeId)
    {
        return Database.Query(@"
            SELECT mc.ThuTu AS CauSo, c.LoaiCauHoi, c.MucDoKho, cd.TenChuDe, c.NoiDung AS NoiDungCau,
                   md.ThuTu AS ThuTuDapAn, d.NoiDung AS NoiDungDapAn, d.LaDapAnDung
            FROM MaDe_CauHoi mc
            JOIN CauHoi c ON c.CauHoiID = mc.CauHoiID
            JOIN ChuDe cd ON cd.ChuDeID = c.ChuDeID
            JOIN MaDe_DapAn md ON md.MaDeID = mc.MaDeID AND md.CauHoiID = mc.CauHoiID
            JOIN DapAn d ON d.DapAnID = md.DapAnID
            WHERE mc.MaDeID = @m
            ORDER BY mc.ThuTu, md.ThuTu",
            p => p.AddWithValue("@m", maDeId));
    }

    public void XoaBaiLamCuaDe(int deThiId)
    {
        YeuCauQuanTriVien();
        Database.Execute(@"
            DELETE FROM LuaChonBaiLam WHERE ChiTietID IN (
                SELECT ct.ChiTietID FROM ChiTietBaiLam ct
                JOIN BaiLam bl ON bl.BaiLamID = ct.BaiLamID
                JOIN DotThi dt ON dt.DotThiID = bl.DotThiID
                WHERE dt.DeThiID = @id);
            DELETE FROM ChiTietBaiLam WHERE BaiLamID IN (
                SELECT bl.BaiLamID FROM BaiLam bl
                JOIN DotThi dt ON dt.DotThiID = bl.DotThiID
                WHERE dt.DeThiID = @id);
            DELETE FROM BaiLam WHERE DotThiID IN (
                SELECT DotThiID FROM DotThi WHERE DeThiID = @id);",
            p => p.AddWithValue("@id", deThiId));
    }

    // ------------------------------------------------------------------ TẠO ĐỀ THEO MA TRẬN CHỦ ĐỀ CHUẨN THPTQG
    public DataTable LayMaTranChuDeTheoMon(int monHocId)
    {
        return Database.Query(@"
            SELECT cd.ChuDeID, cd.TenChuDe,
                   (SELECT COUNT(*) FROM CauHoi WHERE ChuDeID = cd.ChuDeID AND LoaiCauHoi = 'TracNghiem' AND TrangThai = 'HoatDong') AS SoTN,
                   (SELECT COUNT(*) FROM CauHoi WHERE ChuDeID = cd.ChuDeID AND LoaiCauHoi = 'DungSai' AND TrangThai = 'HoatDong') AS SoDS,
                   (SELECT COUNT(*) FROM CauHoi WHERE ChuDeID = cd.ChuDeID AND LoaiCauHoi = 'TraLoiNgan' AND TrangThai = 'HoatDong') AS SoTLN,
                   (SELECT COUNT(*) FROM CauHoi WHERE ChuDeID = cd.ChuDeID AND TrangThai = 'HoatDong') AS TongCau
            FROM ChuDe cd
            WHERE cd.MonHocID = @m
            ORDER BY cd.ChuDeID",
            p => p.AddWithValue("@m", monHocId));
    }

    public DataTable LayMaTranTheoChuDeVaMucDo(int monHocId)
    {
        return Database.Query(@"
            SELECT cd.ChuDeID, cd.TenChuDe, lv.MucDoKho,
                   SUM(CASE WHEN c.LoaiCauHoi = 'TracNghiem' THEN 1 ELSE 0 END) AS SoTN,
                   SUM(CASE WHEN c.LoaiCauHoi = 'DungSai' THEN 1 ELSE 0 END) AS SoDS,
                   SUM(CASE WHEN c.LoaiCauHoi = 'TraLoiNgan' THEN 1 ELSE 0 END) AS SoTLN
            FROM ChuDe cd
            CROSS JOIN (VALUES (N'NhanBiet'), (N'ThongHieu'), (N'VanDung'), (N'VanDungCao')) lv(MucDoKho)
            LEFT JOIN CauHoi c ON c.ChuDeID = cd.ChuDeID AND c.MucDoKho = lv.MucDoKho
                AND c.TrangThai = N'HoatDong'
            WHERE cd.MonHocID = @m
            GROUP BY cd.ChuDeID, cd.TenChuDe, lv.MucDoKho
            ORDER BY cd.ChuDeID, CASE lv.MucDoKho
                WHEN N'NhanBiet' THEN 1 WHEN N'ThongHieu' THEN 2
                WHEN N'VanDung' THEN 3 ELSE 4 END",
            p => p.AddWithValue("@m", monHocId));
    }

    public (int DeThiID, string Error) TaoDeTheoMaTranTHPTQG(
        string tenDe,
        int monHocId,
        string loaiDe,
        List<MaTranChuDeItem> maTran,
        bool tuDongTronMaDe = true,
        int soLuongMaDe = 4,
        int soChuDeToiThieu = 1,
        int soCauMoiChuDeToiThieu = 1)
    {
        YeuCauQuanTriVien();
        if (string.IsNullOrWhiteSpace(tenDe))
            return (0, "Chưa nhập tên đề thi.");

        var tongPhan1 = maTran.Sum(x => x.SoCauPhan1);
        var tongPhan2 = maTran.Sum(x => x.SoCauPhan2);
        var tongPhan3 = maTran.Sum(x => x.SoCauPhan3);
        var tongCau = tongPhan1 + tongPhan2 + tongPhan3;

        if (tongCau == 0)
            return (0, "Chưa phân bổ số lượng câu hỏi cho ma trận đề thi.");

        if (maTran.Any(x => x.SoCauPhan1 < 0 || x.SoCauPhan2 < 0 || x.SoCauPhan3 < 0))
            return (0, "Số câu trong ma trận không được âm.");
        if (maTran.GroupBy(x => (x.ChuDeID, x.MucDoKho)).Any(g => g.Count() > 1))
            return (0, "Ma trận có chủ đề và mức độ bị lặp.");
        if (maTran.Any(x => !string.IsNullOrEmpty(x.MucDoKho) &&
            x.MucDoKho is not ("NhanBiet" or "ThongHieu" or "VanDung" or "VanDungCao")))
            return (0, "Ma trận có mức độ tư duy không hợp lệ.");

        var soChuDe = maTran.Where(x => x.SoCauPhan1 + x.SoCauPhan2 + x.SoCauPhan3 > 0)
            .Select(x => x.ChuDeID).Distinct().Count();
        if (soChuDe < soChuDeToiThieu)
            return (0, $"Đề cần ít nhất {soChuDeToiThieu} chủ đề; hiện mới có {soChuDe}.");
        foreach (var chuDe in maTran.GroupBy(x => x.ChuDeID))
        {
            var soCauChuDe = chuDe.Sum(x => x.SoCauPhan1 + x.SoCauPhan2 + x.SoCauPhan3);
            if (soCauChuDe > 0 && soCauChuDe < soCauMoiChuDeToiThieu)
                return (0, $"Chủ đề '{chuDe.First().TenChuDe}' cần ít nhất {soCauMoiChuDeToiThieu} câu; hiện có {soCauChuDe}.");
        }

        // Kiểm tra ngân hàng câu hỏi
        var dtPool = LayMaTranChuDeTheoMon(monHocId);
        var poolMap = new Dictionary<int, (int TN, int DS, int TLN)>();
        foreach (DataRow r in dtPool.Rows)
        {
            var id = Convert.ToInt32(r["ChuDeID"]);
            poolMap[id] = (Convert.ToInt32(r["SoTN"]), Convert.ToInt32(r["SoDS"]), Convert.ToInt32(r["SoTLN"]));
        }

        var dtTheoMuc = LayMaTranTheoChuDeVaMucDo(monHocId);
        var mucMap = dtTheoMuc.Rows.Cast<DataRow>().ToDictionary(
            r => (Convert.ToInt32(r["ChuDeID"]), r["MucDoKho"].ToString()!),
            r => (TN: Convert.ToInt32(r["SoTN"]), DS: Convert.ToInt32(r["SoDS"]), TLN: Convert.ToInt32(r["SoTLN"])));
        foreach (var item in maTran)
        {
            if (!poolMap.TryGetValue(item.ChuDeID, out var pool))
                return (0, $"Chủ đề '{item.TenChuDe}' không thuộc môn học đã chọn.");
            if (!string.IsNullOrEmpty(item.MucDoKho))
            {
                if (!mucMap.TryGetValue((item.ChuDeID, item.MucDoKho), out pool))
                    return (0, $"Không tìm thấy mức độ '{item.MucDoKho}' cho chủ đề '{item.TenChuDe}'.");
            }
            var mucText = string.IsNullOrEmpty(item.MucDoKho) ? "" : $" / {Models.MucDoKho.Display(item.MucDoKho)}";
            if (item.SoCauPhan1 > pool.TN)
                return (0, $"Chủ đề '{item.TenChuDe}'{mucText} chỉ có {pool.TN} câu Trắc nghiệm (cần {item.SoCauPhan1} câu).");
            if (item.SoCauPhan2 > pool.DS)
                return (0, $"Chủ đề '{item.TenChuDe}'{mucText} chỉ có {pool.DS} câu Đúng/Sai (cần {item.SoCauPhan2} câu).");
            if (item.SoCauPhan3 > pool.TLN)
                return (0, $"Chủ đề '{item.TenChuDe}'{mucText} chỉ có {pool.TLN} câu Trả lời ngắn (cần {item.SoCauPhan3} câu).");
        }

        // Bốc ngẫu nhiên câu hỏi theo từng chủ đề và từng phần độc lập
        var listPhan1 = new List<int>();
        var listPhan2 = new List<int>();
        var listPhan3 = new List<int>();

        foreach (var item in maTran)
        {
            if (item.SoCauPhan1 > 0)
            {
                var qs = Database.ReadList(
                    "SELECT TOP (@n) CauHoiID FROM CauHoi WHERE ChuDeID = @c AND LoaiCauHoi = 'TracNghiem' AND TrangThai = 'HoatDong' AND (@muc = N'' OR MucDoKho = @muc) ORDER BY NEWID()",
                    r => r.GetInt32(0),
                    p => { p.AddWithValue("@n", item.SoCauPhan1); p.AddWithValue("@c", item.ChuDeID); p.AddWithValue("@muc", item.MucDoKho); });
                listPhan1.AddRange(qs);
            }
            if (item.SoCauPhan2 > 0)
            {
                var qs = Database.ReadList(
                    "SELECT TOP (@n) CauHoiID FROM CauHoi WHERE ChuDeID = @c AND LoaiCauHoi = 'DungSai' AND TrangThai = 'HoatDong' AND (@muc = N'' OR MucDoKho = @muc) ORDER BY NEWID()",
                    r => r.GetInt32(0),
                    p => { p.AddWithValue("@n", item.SoCauPhan2); p.AddWithValue("@c", item.ChuDeID); p.AddWithValue("@muc", item.MucDoKho); });
                listPhan2.AddRange(qs);
            }
            if (item.SoCauPhan3 > 0)
            {
                var qs = Database.ReadList(
                    "SELECT TOP (@n) CauHoiID FROM CauHoi WHERE ChuDeID = @c AND LoaiCauHoi = 'TraLoiNgan' AND TrangThai = 'HoatDong' AND (@muc = N'' OR MucDoKho = @muc) ORDER BY NEWID()",
                    r => r.GetInt32(0),
                    p => { p.AddWithValue("@n", item.SoCauPhan3); p.AddWithValue("@c", item.ChuDeID); p.AddWithValue("@muc", item.MucDoKho); });
                listPhan3.AddRange(qs);
            }
        }

        // Sắp xếp đề gốc chuẩn THPT Quốc gia: Phần I -> Phần II -> Phần III
        var allQuestionIds = new List<int>();
        allQuestionIds.AddRange(listPhan1);
        allQuestionIds.AddRange(listPhan2);
        allQuestionIds.AddRange(listPhan3);

        if (allQuestionIds.Count != tongCau)
            return (0, "Ngân hàng câu hỏi vừa thay đổi; vui lòng nạp lại ma trận và thử lại.");
        var deThiId = TaoDeCore(tenDe, monHocId, loaiDe, "MaTranTHPTQG", allQuestionIds);

        // Tự động trộn mã đề nếu được yêu cầu
        if (tuDongTronMaDe)
        {
            var (success, message, _) = TronDeThi(deThiId, Math.Max(1, soLuongMaDe), 101, true, false);
            if (!success)
            {
                XoaDe(deThiId);
                return (0, "Không tạo được mã đề: " + message);
            }
        }
        return (deThiId, "");
    }

    private static List<CauHoiItem> TronTheoChuDeVaMucDo(List<CauHoiItem> items, Random rng)
    {
        var levels = new[] { "NhanBiet", "ThongHieu", "VanDung", "VanDungCao", "ChuaXacDinh" };
        return items.GroupBy(x => x.ChuDeID)
            .OrderBy(g => g.Min(x => x.ThuTuGoc))
            .SelectMany(chuDe => chuDe.GroupBy(x => x.MucDoKho)
                .OrderBy(g => Array.IndexOf(levels, g.Key) is var i && i >= 0 ? i : levels.Length)
                .SelectMany(g => g.OrderBy(_ => rng.Next())))
            .ToList();
    }

    private class CauHoiItem
    {
        public int CauHoiID { get; set; }
        public int ThuTuGoc { get; set; }
        public int ChuDeID { get; set; }
        public string LoaiCauHoi { get; set; } = "TracNghiem";
        public string MucDoKho { get; set; } = "ChuaXacDinh";
        public string NoiDung { get; set; } = "";
        public decimal Diem { get; set; }
        public List<DapAnItem> DapAns { get; set; } = new();
    }

    private class DapAnItem
    {
        public int DapAnID { get; set; }
        public int CauHoiID { get; set; }
        public int ThuTuGoc { get; set; }
        public string NoiDung { get; set; } = "";
        public bool LaDapAnDung { get; set; }
        public string? GiaiThich { get; set; }
    }
}

public class MaTranChuDeItem
{
    public int ChuDeID { get; set; }
    public string TenChuDe { get; set; } = "";
    public string MucDoKho { get; set; } = "";
    public int SoCauPhan1 { get; set; }
    public int SoCauPhan2 { get; set; }
    public int SoCauPhan3 { get; set; }
}
