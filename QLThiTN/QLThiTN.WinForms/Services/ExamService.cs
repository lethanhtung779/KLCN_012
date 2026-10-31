using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using QLThiTN.WinForms.Models;

namespace QLThiTN.WinForms.Services;

/// <summary>Quan ly de thi: tao tu dong / thu cong tu ngan hang cau hoi, quan ly dot thi.</summary>
public class ExamService
{
    // Tham số cũ chỉ giữ để tương thích các điểm gọi trước đây; đề nay thuộc tài khoản Admin tạo,
    // không còn gán một giáo viên phụ trách khi tạo đề.
    public ExamService(int? ignoredTeacherId = null) { _ = ignoredTeacherId; }
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
        Session.YeuCauQuanTriVien();
    }

    public DataTable LayLopChoDotThi(int deThiId)
    {
        YeuCauQuanTriVien();
        return Database.Query(@"SELECT lh.LopHocID, CONCAT('LH', FORMAT(lh.LopHocID, '000000'), N' · ', lh.TenLop, N' — ', kh.TenKhoaHoc) AS TenHienThi
            FROM DeThi d JOIN KhoaHoc kh ON kh.MonHocID=d.MonHocID
            JOIN LopHoc lh ON lh.KhoaHocID=kh.KhoaHocID
            WHERE d.DeThiID=@de AND kh.TrangThai<>N'DaKetThuc'
            ORDER BY kh.TenKhoaHoc,lh.TenLop", p => p.AddWithValue("@de", deThiId));
    }

    public DataTable LayDanhSachDe()
    {
        return Database.Query(@"
            SELECT d.DeThiID, d.TenDe, m.TenMon, d.LoaiDe, d.SoLuongCauHoi, d.HinhThucTao,
                   d.LaMienPhi, CASE WHEN d.DaDuyet=1 THEN N'Đã duyệt' ELSE N'Chờ duyệt' END AS Duyet,
                   CASE WHEN d.LaMienPhi = 1 THEN N'Miễn phí' ELSE N'Đóng phí (VIP)' END AS QuyenThi,
                   COUNT(dt.DotThiID) AS SoDotThi,
                   (SELECT COUNT(*) FROM MaDeThi md WHERE md.DeThiID = d.DeThiID) AS SoMaDe
            FROM DeThi d
            JOIN MonHoc m ON m.MonHocID = d.MonHocID
            LEFT JOIN DotThi dt ON dt.DeThiID = d.DeThiID
            GROUP BY d.DeThiID, d.TenDe, m.TenMon, d.LoaiDe, d.SoLuongCauHoi, d.HinhThucTao, d.LaMienPhi, d.DaDuyet
            ORDER BY d.DeThiID DESC");
    }

    public List<int> LayCauHoiCuaDe(int deThiId) =>
        Database.ReadList(
            "SELECT CauHoiID FROM DeThi_CauHoi WHERE DeThiID = @id ORDER BY ThuTu",
            r => r.GetInt32(0),
            p => p.AddWithValue("@id", deThiId));

    public string? LayLoiMaDe(int deThiId)
    {
        var soCau = Database.ExecuteScalarInt("SELECT COUNT(*) FROM DeThi_CauHoi WHERE DeThiID=@id",
            p => p.AddWithValue("@id", deThiId));
        if (soCau == 0) return "Đề chưa có câu hỏi.";
        if (Database.ExecuteScalarInt("SELECT COUNT(*) FROM MaDeThi WHERE DeThiID=@id",
                p => p.AddWithValue("@id", deThiId)) == 0)
            return "Đề chưa có mã đề.";

        var badQuestionOrder = Database.Query(@"
            SELECT TOP (1) md.TenMaDe,COUNT(mc.CauHoiID) AS SoThuTu,COUNT(DISTINCT mc.ThuTu) AS SoThuTuKhac
            FROM MaDeThi md LEFT JOIN MaDe_CauHoi mc ON mc.MaDeID=md.MaDeID
            WHERE md.DeThiID=@id
            GROUP BY md.MaDeID,md.TenMaDe
            HAVING COUNT(mc.CauHoiID)<>@questions OR COUNT(DISTINCT mc.ThuTu)<>@questions",
            p => { p.AddWithValue("@id", deThiId); p.AddWithValue("@questions", soCau); });
        if (badQuestionOrder.Rows.Count > 0)
            return $"Mã đề {badQuestionOrder.Rows[0]["TenMaDe"]} chưa có đủ thứ tự cho {soCau} câu hỏi.";

        var badAnswerOrder = Database.Query(@"
            SELECT TOP (1) md.TenMaDe,dc.CauHoiID,
                   (SELECT COUNT(*) FROM DapAn a WHERE a.CauHoiID=dc.CauHoiID) AS SoDapAn,
                   (SELECT COUNT(*) FROM MaDe_DapAn x WHERE x.MaDeID=md.MaDeID AND x.CauHoiID=dc.CauHoiID) AS SoThuTu
            FROM MaDeThi md CROSS JOIN DeThi_CauHoi dc
            WHERE md.DeThiID=@id AND dc.DeThiID=@id
              AND ((SELECT COUNT(*) FROM DapAn a WHERE a.CauHoiID=dc.CauHoiID) <>
                   (SELECT COUNT(*) FROM MaDe_DapAn x WHERE x.MaDeID=md.MaDeID AND x.CauHoiID=dc.CauHoiID)
                OR (SELECT COUNT(DISTINCT x.ThuTu) FROM MaDe_DapAn x WHERE x.MaDeID=md.MaDeID AND x.CauHoiID=dc.CauHoiID) <>
                   (SELECT COUNT(*) FROM DapAn a WHERE a.CauHoiID=dc.CauHoiID))",
            p => p.AddWithValue("@id", deThiId));
        if (badAnswerOrder.Rows.Count > 0)
            return $"Mã đề {badAnswerOrder.Rows[0]["TenMaDe"]}, câu ID {badAnswerOrder.Rows[0]["CauHoiID"]} chưa có đủ thứ tự đáp án.";
        return null;
    }

    /// <summary>Tao de tu dong: chon ngau nhien so cau theo mon (+ do kho tuy chon)
    /// tu cac cau HoatDong. Tra ve DeThiID moi.</summary>
    public (int DeThiID, string Error) TaoDeTuDong(string tenDe, int monHocId, int soCau, string? mucDo,
        string loaiDe, string tinHocTrack="KhoaHocMayTinh", bool laMienPhi=false)
    {
        YeuCauQuanTriVien();
        if (string.IsNullOrWhiteSpace(tenDe)) return (0, "Chưa nhập tên đề.");
        if (soCau < 1) return (0, "Số câu phải >= 1.");

        try
        {
            var subject=Database.ExecuteScalarString("SELECT TenMon FROM MonHoc WHERE MonHocID=@m",p=>p.AddWithValue("@m",monHocId));
            var format=QLThiTN.Shared.ExamFormat.ForSubject(subject);
            var chosen=new List<int>();
            var tin=subject.Contains("Tin",StringComparison.OrdinalIgnoreCase);
            if(tin && tinHocTrack is not ("KhoaHocMayTinh" or "TinHocUngDung"))return(0,"Chọn định hướng Tin học.");
            var parts=tin ? new[]{("TracNghiem",24,""),("DungSai",2,"Chung"),("DungSai",2,tinHocTrack)} :
                new[]{("TracNghiem",format.MultipleChoice,""),("DungSai",format.TrueFalse,""),("TraLoiNgan",format.ShortAnswer,"")};
            foreach(var (type,count,track) in parts)
            {
                if(count==0)continue;
                var pool=Database.ReadList(@"SELECT c.CauHoiID FROM CauHoi c JOIN ChuDe cd ON cd.ChuDeID=c.ChuDeID
                    WHERE cd.MonHocID=@m AND c.TrangThai=N'HoatDong' AND c.LoaiCauHoi=@type
                      AND (@level=N'' OR c.MucDoKho=@level) AND (@track=N'' OR c.NhomTHPT=@track) ORDER BY NEWID()",r=>r.GetInt32(0),p=>{
                        p.AddWithValue("@m",monHocId);p.AddWithValue("@type",type);p.AddWithValue("@level",mucDo??"");p.AddWithValue("@track",track);});
                if(pool.Count<count)return (0,$"Thiếu câu {type} {track}: cần {count}, chỉ có {pool.Count}. Không tạo đề sai cấu trúc.");
                chosen.AddRange(pool.Take(count));
            }
            var id=TaoDeCore(tenDe,monHocId,loaiDe,"TuDong",chosen,laMienPhi);
            return (id, "");
        }
        catch(InvalidOperationException ex){return (0,ex.Message);}

    }

    /// <summary>Tao de thu cong tu danh sach cau hoi nguoi dung chon (giu dung thu tu).</summary>
    public (int DeThiID, string Error) TaoDeThuCong(string tenDe, int monHocId, string loaiDe,
        List<int> cauHoiIds, bool laMienPhi=false)
    {
        YeuCauQuanTriVien();
        if (string.IsNullOrWhiteSpace(tenDe)) return (0, "Chưa nhập tên đề.");
        if (cauHoiIds.Count == 0) return (0, "Chưa chọn câu hỏi nào cho đề.");
        var id = TaoDeCore(tenDe, monHocId, loaiDe, "ThuCong", cauHoiIds, laMienPhi);
        return (id, "");
    }

    private static bool _hinhThucTaoEnsured;
    private static void EnsureConstraintHinhThucTao(SqlConnection conn)
    {
        if (_hinhThucTaoEnsured) return;
        try
        {
            using var cmd = new SqlCommand(@"
DECLARE @constraint sysname;
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
SELECT name FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID(N'dbo.DeThi')
  AND definition LIKE N'%HinhThucTao%'
  AND definition NOT LIKE N'%MaTranTHPTQG%';
OPEN cur;
FETCH NEXT FROM cur INTO @constraint;
WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @dropSql nvarchar(max) = N'ALTER TABLE dbo.DeThi DROP CONSTRAINT ' + QUOTENAME(@constraint);
    EXEC sp_executesql @dropSql;
    FETCH NEXT FROM cur INTO @constraint;
END
CLOSE cur;
DEALLOCATE cur;

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.DeThi')
      AND definition LIKE N'%HinhThucTao%'
      AND definition LIKE N'%MaTranTHPTQG%'
)
BEGIN
    ALTER TABLE dbo.DeThi WITH CHECK ADD CONSTRAINT CK_DeThi_HinhThucTao
        CHECK (HinhThucTao IN (N'ThuCong', N'TuDong', N'MaTranTHPTQG'));
END", conn);
            cmd.ExecuteNonQuery();
            _hinhThucTaoEnsured = true;
        }
        catch { /* Khong chan luong tao de neu thieu quyen DDL */ }
    }

    private int TaoDeCore(string tenDe, int monHocId, string loaiDe, string hinhThuc,
        List<int> cauHoiIds, bool laMienPhi)
    {
        if (cauHoiIds.Distinct().Count() != cauHoiIds.Count)
            throw new InvalidOperationException("Danh sách câu hỏi trong đề không được trùng lặp.");

        var creatorId = Session.Current!.TaiKhoanID;
        using var conn = Database.Open();
        EnsureConstraintHinhThucTao(conn);
        using var tx = conn.BeginTransaction();
        try
        {
            using (var validate = new SqlCommand(@"SELECT COUNT(*) FROM CauHoi c
                JOIN ChuDe cd ON cd.ChuDeID=c.ChuDeID
                WHERE c.CauHoiID IN (SELECT value FROM STRING_SPLIT(@ids, ','))
                  AND cd.MonHocID=@mon AND c.TrangThai=N'HoatDong'", conn, tx))
            {
                validate.Parameters.AddWithValue("@ids", string.Join(",", cauHoiIds));
                validate.Parameters.AddWithValue("@mon", monHocId);
                if (Convert.ToInt32(validate.ExecuteScalar()) != cauHoiIds.Count)
                    throw new InvalidOperationException("Đề có câu hỏi khác môn, đã ẩn hoặc không còn tồn tại.");
            }
            using var cmd1 = new SqlCommand(@"
                INSERT INTO DeThi (MonHocID, NguoiTaoID, TenDe, LoaiDe, SoLuongCauHoi, HinhThucTao, LaMienPhi)
                VALUES (@m, @gv, @t, @l, @s, @h, @free);
                SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx);
            cmd1.Parameters.AddWithValue("@m", monHocId);
            cmd1.Parameters.AddWithValue("@gv", creatorId);
            cmd1.Parameters.AddWithValue("@t", tenDe.Trim());
            cmd1.Parameters.AddWithValue("@l", loaiDe);
            cmd1.Parameters.AddWithValue("@s", cauHoiIds.Count);
            cmd1.Parameters.AddWithValue("@h", hinhThuc);
            cmd1.Parameters.AddWithValue("@free", laMienPhi);
            int id = 0;
            try
            {
                id = Convert.ToInt32(cmd1.ExecuteScalar() ?? 0);
            }
            catch (SqlException ex) when (ex.Number == 547 && hinhThuc == "MaTranTHPTQG")
            {
                // Fallback nếu database cũ vẫn còn constraint chỉ cho phép ThuCong/TuDong
                cmd1.Parameters["@h"].Value = "TuDong";
                id = Convert.ToInt32(cmd1.ExecuteScalar() ?? 0);
            }

            for (var i = 0; i < cauHoiIds.Count; i++)
            {
                using var cmd2 = new SqlCommand(
                    "INSERT INTO DeThi_CauHoi (DeThiID, CauHoiID, ThuTu) VALUES (@d, @c, @t)", conn, tx);
                cmd2.Parameters.AddWithValue("@d", id);
                cmd2.Parameters.AddWithValue("@c", cauHoiIds[i]);
                cmd2.Parameters.AddWithValue("@t", i + 1);
                cmd2.ExecuteNonQuery();
            }

            // Tạo mã đề mặc định 101 với đầy đủ thứ tự câu/đáp án. Nhờ vậy bản nháp
            // có thể được xem trước chính xác và không bao giờ sinh ra một mã đề rỗng.
            using var cmd3 = new SqlCommand(
                "INSERT INTO MaDeThi (DeThiID, TenMaDe) VALUES (@d, N'101'); SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx);
            cmd3.Parameters.AddWithValue("@d", id);
            var maDeId = Convert.ToInt32(cmd3.ExecuteScalar());

            for (var i = 0; i < cauHoiIds.Count; i++)
            {
                using var mapQuestion = new SqlCommand(
                    "INSERT INTO MaDe_CauHoi (MaDeID, CauHoiID, ThuTu) VALUES (@m, @c, @t)", conn, tx);
                mapQuestion.Parameters.AddWithValue("@m", maDeId);
                mapQuestion.Parameters.AddWithValue("@c", cauHoiIds[i]);
                mapQuestion.Parameters.AddWithValue("@t", i + 1);
                mapQuestion.ExecuteNonQuery();

                using var answers = new SqlCommand(
                    "SELECT DapAnID, ROW_NUMBER() OVER (ORDER BY ThuTu, DapAnID) AS ThuTu FROM DapAn WHERE CauHoiID=@c", conn, tx);
                answers.Parameters.AddWithValue("@c", cauHoiIds[i]);
                using var reader = answers.ExecuteReader();
                var answerOrder = new List<(int Id, int Order)>();
                while (reader.Read()) answerOrder.Add((reader.GetInt32(0), Convert.ToInt32(reader.GetValue(1))));
                reader.Close();

                foreach (var answer in answerOrder)
                {
                    using var mapAnswer = new SqlCommand(@"
                        INSERT INTO MaDe_DapAn (MaDeID, CauHoiID, DapAnID, ThuTu)
                        VALUES (@m, @c, @a, @t)", conn, tx);
                    mapAnswer.Parameters.AddWithValue("@m", maDeId);
                    mapAnswer.Parameters.AddWithValue("@c", cauHoiIds[i]);
                    mapAnswer.Parameters.AddWithValue("@a", answer.Id);
                    mapAnswer.Parameters.AddWithValue("@t", answer.Order);
                    mapAnswer.ExecuteNonQuery();
                }
            }

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
    public int TaoDotThi(DotThi d, IReadOnlyCollection<int>? lopHocIds = null)
    {
        YeuCauQuanTriVien();
        if (string.IsNullOrWhiteSpace(d.TenDotThi)) d.TenDotThi = "Đợt thi - " + d.TenDe;
        if (d.ThoiGianDongCong <= d.ThoiGianMoCong) throw new Exception("Giờ đóng công phải sau giờ mở công.");
        if (d.ThoiLuongLamBai <= 0) throw new Exception("Thời lượng làm bài phải > 0.");
        if (d.GioiHanSoLuong is <= 0 || d.SoLanThiToiDa < 1)
            throw new InvalidOperationException("Số chỗ và số lần thi phải lớn hơn 0.");
        if (d.PhamVi is not ("ToanTruong" or "TheoLop"))
            throw new InvalidOperationException("Phạm vi đợt thi không hợp lệ.");
        var classes = (lopHocIds ?? Array.Empty<int>()).Distinct().ToArray();
        if (d.PhamVi == "TheoLop" && classes.Length == 0)
            throw new InvalidOperationException("Hãy chọn ít nhất một lớp cho đợt thi theo lớp.");

        var trangThai = DateTime.Now < d.ThoiGianMoCong ? "SapMo"
            : DateTime.Now > d.ThoiGianDongCong ? "DaDong" : "DangMo";

        using var conn = Database.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            if (d.PhamVi == "TheoLop")
            {
                using var validate = new SqlCommand(@"SELECT COUNT(*) FROM LopHoc lh
                    JOIN KhoaHoc kh ON kh.KhoaHocID=lh.KhoaHocID
                    JOIN DeThi de ON de.MonHocID=kh.MonHocID
                    WHERE de.DeThiID=@de AND lh.LopHocID IN (SELECT value FROM STRING_SPLIT(@ids, ','))
                      AND kh.TrangThai<>N'DaKetThuc'", conn, tx);
                validate.Parameters.AddWithValue("@de", d.DeThiID);
                validate.Parameters.AddWithValue("@ids", string.Join(",", classes));
                if (Convert.ToInt32(validate.ExecuteScalar()) != classes.Length)
                    throw new InvalidOperationException("Có lớp không thuộc môn của đề hoặc khóa học đã kết thúc.");
            }
            using var create = new SqlCommand(@"
            INSERT INTO DotThi (DeThiID, TenDotThi, ThoiGianMoCong, ThoiGianDongCong, ThoiLuongLamBai,
                                GioiHanSoLuong, CongBoDiemSom, TrangThai, PhamVi, SoLanThiToiDa)
            VALUES (@de, @ten, @mo, @dong, @luong, @han, @cong, @tt, @pv, @lan);
            SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx);
            create.Parameters.AddWithValue("@de", d.DeThiID);
            create.Parameters.AddWithValue("@ten", d.TenDotThi.Trim());
            create.Parameters.AddWithValue("@mo", d.ThoiGianMoCong);
            create.Parameters.AddWithValue("@dong", d.ThoiGianDongCong);
            create.Parameters.AddWithValue("@luong", d.ThoiLuongLamBai);
            create.Parameters.AddWithValue("@han", (object?)d.GioiHanSoLuong ?? DBNull.Value);
            create.Parameters.AddWithValue("@cong", d.CongBoDiemSom);
            create.Parameters.AddWithValue("@tt", trangThai);
            create.Parameters.AddWithValue("@pv", d.PhamVi);
            create.Parameters.AddWithValue("@lan", d.SoLanThiToiDa);
            var id = Convert.ToInt32(create.ExecuteScalar());
            foreach (var lop in classes)
            {
                if (d.PhamVi != "TheoLop") break;
                using var link = new SqlCommand("INSERT INTO DotThi_LopHoc(DotThiID,LopHocID) VALUES(@dot,@lop)", conn, tx);
                link.Parameters.AddWithValue("@dot", id);
                link.Parameters.AddWithValue("@lop", lop);
                link.ExecuteNonQuery();
            }
            tx.Commit();
            return id;
        }
        catch { tx.Rollback(); throw; }
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

        try
        {
            var examInfo=Database.Query("SELECT m.TenMon,d.HinhThucTao FROM DeThi d JOIN MonHoc m ON m.MonHocID=d.MonHocID WHERE d.DeThiID=@id",p=>p.AddWithValue("@id",deThiId));
            var subject=examInfo.Rows[0]["TenMon"].ToString()!;
            var followsNationalFormat=examInfo.Rows[0]["HinhThucTao"].ToString()=="MaTranTHPTQG";
            if(followsNationalFormat)
            {
                QLThiTN.Shared.ExamFormat.ForSubject(subject).Validate(dtCauHoi.AsEnumerable().Select(r=>r["LoaiCauHoi"].ToString()!));
                if(subject.Contains("Tin",StringComparison.OrdinalIgnoreCase))
                {
                    var tracks=Database.ReadList("SELECT c.NhomTHPT FROM CauHoi c JOIN DeThi_CauHoi d ON d.CauHoiID=c.CauHoiID WHERE d.DeThiID=@id AND c.LoaiCauHoi=N'DungSai'",r=>r.GetString(0),p=>p.AddWithValue("@id",deThiId));
                    if(tracks.Count(t=>t=="Chung")!=2 || !(tracks.Count(t=>t=="KhoaHocMayTinh")==2 || tracks.Count(t=>t=="TinHocUngDung")==2))
                        return(false,"Tin học cần 2 câu Đ/S chung và 2 câu cùng định hướng. Hãy phân loại nhánh trong ngân hàng và chọn lại câu hỏi.",new());
                }
            }
        }
        catch(InvalidOperationException ex){return (false,ex.Message,new());}

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
        // Không cho mã mới trùng thứ tự câu của đề gốc hoặc của mã vừa sinh.
        var usedQuestionOrders = new HashSet<string>
        {
            QuestionOrderSignature(phan1.Concat(phan2).Concat(phan3))
        };

        using var conn = Database.Open();
        using var tran = conn.BeginTransaction();
        try
        {
            if (xoaCu && xoaBaiLamCu)
                XoaBaiLamCuaDe(conn, tran, deThiId);
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

                // Giữ đúng ranh giới ba phần của mẫu THPT, nhưng trộn đều toàn bộ
                // câu trong từng phần. Không khóa thứ tự theo chủ đề/mức độ vì khi
                // mỗi ô ma trận chỉ có một câu thì thực tế không có gì được trộn.
                List<CauHoiItem>? allQuestions = null;
                for (var attempt = 0; attempt < 100; attempt++)
                {
                    var candidate = new List<CauHoiItem>();
                    candidate.AddRange(TronNgauNhienTrongPhan(phan1, rng));
                    candidate.AddRange(TronNgauNhienTrongPhan(phan2, rng));
                    candidate.AddRange(TronNgauNhienTrongPhan(phan3, rng));
                    if (usedQuestionOrders.Add(QuestionOrderSignature(candidate)))
                    {
                        allQuestions = candidate;
                        break;
                    }
                }
                if (allQuestions is null)
                    throw new InvalidOperationException("Không tạo được thứ tự câu hỏi khác nhau cho các mã đề.");

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
                        var daShuffled=q.DapAns.Any(d=>LaDapAnCoDinhCuoi(d.NoiDung))
                            ? q.DapAns.OrderBy(d=>d.ThuTuGoc).ToList()
                            : q.DapAns.OrderBy(_=>rng.Next()).ToList();

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
                        var daShuffled=q.DapAns.OrderBy(d=>d.ThuTuGoc).ToList();

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
        using var conn = Database.Open();
        using var tx = conn.BeginTransaction();
        try { XoaBaiLamCuaDe(conn, tx, deThiId); tx.Commit(); }
        catch { tx.Rollback(); throw; }
    }

    private static void XoaBaiLamCuaDe(SqlConnection conn, SqlTransaction tx, int deThiId)
    {
        using var cmd = new SqlCommand(@"
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
                SELECT DotThiID FROM DotThi WHERE DeThiID = @id);", conn, tx);
        cmd.Parameters.AddWithValue("@id", deThiId);
        cmd.ExecuteNonQuery();
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
            CROSS JOIN (VALUES (N'NhanBiet'), (N'ThongHieu'), (N'VanDung'), (N'VanDungCao'), (N'ChuaXacDinh')) lv(MucDoKho)
            LEFT JOIN CauHoi c ON c.ChuDeID = cd.ChuDeID AND c.MucDoKho = lv.MucDoKho
                AND c.TrangThai = N'HoatDong'
            WHERE cd.MonHocID = @m
            GROUP BY cd.ChuDeID, cd.TenChuDe, lv.MucDoKho
            ORDER BY cd.ChuDeID, CASE lv.MucDoKho
                WHEN N'NhanBiet' THEN 1 WHEN N'ThongHieu' THEN 2
                WHEN N'VanDung' THEN 3 WHEN N'VanDungCao' THEN 4 ELSE 5 END",
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
        int soCauMoiChuDeToiThieu = 1,
        bool laMienPhi = false)
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

        var tenMon = Database.ExecuteScalarString(
            "SELECT TenMon FROM MonHoc WHERE MonHocID=@id",
            p => p.AddWithValue("@id", monHocId));
        try
        {
            var format = QLThiTN.Shared.ExamFormat.ForSubject(tenMon);
            var selectedTypes = Enumerable.Repeat("TracNghiem", tongPhan1)
                .Concat(Enumerable.Repeat("DungSai", tongPhan2))
                .Concat(Enumerable.Repeat("TraLoiNgan", tongPhan3));
            var formatError = format.GetValidationError(selectedTypes);
            if (formatError != null)
                return (0, "Ma trận chưa đúng cấu trúc THPT của môn " + tenMon + ". " + formatError);
        }
        catch (InvalidOperationException ex)
        {
            return (0, ex.Message);
        }

        if (maTran.Any(x => x.SoCauPhan1 < 0 || x.SoCauPhan2 < 0 || x.SoCauPhan3 < 0))
            return (0, "Số câu trong ma trận không được âm.");
        if (maTran.GroupBy(x => (x.ChuDeID, x.MucDoKho)).Any(g => g.Count() > 1))
            return (0, "Ma trận có chủ đề và mức độ bị lặp.");
        if (maTran.Any(x => !string.IsNullOrEmpty(x.MucDoKho) &&
            x.MucDoKho is not ("NhanBiet" or "ThongHieu" or "VanDung" or "VanDungCao" or "ChuaXacDinh")))
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
        var deThiId = TaoDeCore(tenDe, monHocId, loaiDe, "MaTranTHPTQG", allQuestionIds, laMienPhi);

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

    private static List<CauHoiItem> TronNgauNhienTrongPhan(List<CauHoiItem> items, Random rng)
    {
        var result = items.ToList();
        for (var i = result.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }
        return result;
    }

    private static string QuestionOrderSignature(IEnumerable<CauHoiItem> items) =>
        string.Join(',', items.Select(x => x.CauHoiID));

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
