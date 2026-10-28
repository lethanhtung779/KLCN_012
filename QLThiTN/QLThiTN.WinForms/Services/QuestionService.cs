using System.Data;
using Microsoft.Data.SqlClient;
using QLThiTN.WinForms.Models;

namespace QLThiTN.WinForms.Services;

/// <summary>Quan ly ngan hang cau hoi: loc, CRUD ca 3 loai cau hoi, import tu file.</summary>
public class QuestionService
{
    // ------------------------------------------------------------------ danh muc
    public List<MonHoc> LayMonHoc()
    {
        if (Session.IsGiaoVien)
        {
            if (!Session.IsAccountActive()) throw new UnauthorizedAccessException("Tài khoản giáo viên không còn hoạt động.");
            return Database.ReadList(@"
                SELECT m.MonHocID, m.TenMon FROM MonHoc m
                JOIN GiaoVien_MonHoc gm ON gm.MonHocID = m.MonHocID
                WHERE gm.GiaoVienID = @gv
                ORDER BY m.MonHocID",
                r => new MonHoc { MonHocID = r.GetInt32(0), TenMon = r.GetString(1) },
                p => p.AddWithValue("@gv", Session.GiaoVienID));
        }
        Session.YeuCauQuanTriVien();
        return Database.ReadList("SELECT MonHocID, TenMon FROM MonHoc ORDER BY MonHocID",
            r => new MonHoc { MonHocID = r.GetInt32(0), TenMon = r.GetString(1) });
    }

    public List<ChuDe> LayChuDe(int? monHocId = null)
    {
        if (Session.IsGiaoVien)
        {
            if (!Session.IsAccountActive()) throw new UnauthorizedAccessException("Tài khoản giáo viên không còn hoạt động.");
            var sqlGv = monHocId.HasValue
                ? @"SELECT cd.ChuDeID, cd.MonHocID, cd.TenChuDe FROM ChuDe cd
                    JOIN GiaoVien_MonHoc gm ON gm.MonHocID = cd.MonHocID
                    WHERE gm.GiaoVienID = @gv AND cd.MonHocID = @m ORDER BY cd.TenChuDe"
                : @"SELECT cd.ChuDeID, cd.MonHocID, cd.TenChuDe FROM ChuDe cd
                    JOIN GiaoVien_MonHoc gm ON gm.MonHocID = cd.MonHocID
                    WHERE gm.GiaoVienID = @gv ORDER BY cd.TenChuDe";
            return Database.ReadList(sqlGv,
                r => new ChuDe { ChuDeID = r.GetInt32(0), MonHocID = r.GetInt32(1), TenChuDe = r.GetString(2) },
                p =>
                {
                    p.AddWithValue("@gv", Session.GiaoVienID);
                    if (monHocId.HasValue) p.AddWithValue("@m", monHocId.Value);
                });
        }
        Session.YeuCauQuanTriVien();
        var sql = monHocId.HasValue
            ? "SELECT ChuDeID, MonHocID, TenChuDe FROM ChuDe WHERE MonHocID = @m ORDER BY TenChuDe"
            : "SELECT ChuDeID, MonHocID, TenChuDe FROM ChuDe ORDER BY TenChuDe";
        return Database.ReadList(sql,
            r => new ChuDe { ChuDeID = r.GetInt32(0), MonHocID = r.GetInt32(1), TenChuDe = r.GetString(2) },
            p => { if (monHocId.HasValue) p.AddWithValue("@m", monHocId.Value); });
    }

    // ------------------------------------------------------------------ tra cuu
    public DataTable TraCuu(int? monHocId, int? chuDeId, string? loai, string? mucDo, string? tuKhoa, bool chiLocTrung = false,
        int? page = null, int pageSize = 100)
    {
        if (page < 0 || pageSize < 1 || pageSize > 500) throw new ArgumentOutOfRangeException(nameof(page));
        if (Session.IsGiaoVien)
        {
            if (!Session.IsAccountActive()) throw new UnauthorizedAccessException("Tài khoản giáo viên không còn hoạt động.");
        }
        else Session.YeuCauQuanTriVien();
        // Compile for the selected filters instead of reusing the all-subject plan.
        // Serial execution also avoids parallel-query memory grants on SQL Express.
        var sql = $@"
            SELECT {(page.HasValue ? "COUNT(*) OVER() AS TotalCount," : "")} c.CauHoiID, c.NoiDung,
                   CASE c.LoaiCauHoi
                       WHEN 'TracNghiem' THEN N'Trắc nghiệm'
                       WHEN 'DungSai' THEN N'Đúng/Sai'
                       WHEN 'TraLoiNgan' THEN N'Trả lời ngắn'
                       ELSE c.LoaiCauHoi END AS LoaiCauHoi,
                   CASE c.MucDoKho
                       WHEN 'ChuaXacDinh' THEN N'Chưa xác định'
                       WHEN 'NhanBiet' THEN N'Nhận biết'
                       WHEN 'ThongHieu' THEN N'Thông hiểu'
                       WHEN 'VanDung' THEN N'Vận dụng'
                       WHEN 'VanDungCao' THEN N'Vận dụng cao'
                       ELSE c.MucDoKho END AS MucDoKho,
                   c.Diem,
                   CASE c.TrangThai
                       WHEN 'HoatDong' THEN N'Hoạt động'
                       ELSE N'Đã ẩn' END AS TrangThai,
                   cd.TenChuDe, m.TenMon,
                   COALESCE(gv.HoTen, qtv.HoTen, tk.TenDangNhap, N'(không rõ)') AS TenGiaoVien,
                   c.NgayTao, c.ChuDeID, c.NguoiTaoID
            FROM CauHoi c
            JOIN ChuDe cd ON cd.ChuDeID = c.ChuDeID
            JOIN MonHoc m ON m.MonHocID = cd.MonHocID
            LEFT JOIN TaiKhoan tk ON tk.TaiKhoanID = c.NguoiTaoID
            LEFT JOIN GiaoVien gv ON gv.TaiKhoanID = tk.TaiKhoanID
            LEFT JOIN QuanTriVien qtv ON qtv.TaiKhoanID = tk.TaiKhoanID
            WHERE (@mon = 0 OR cd.MonHocID = @mon)
              AND (@chuDe = 0 OR c.ChuDeID = @chuDe)
              AND (@loai = N'' OR c.LoaiCauHoi = @loai)
              AND (@muc = N'' OR c.MucDoKho = @muc)
              AND (@tu = N'' OR c.NoiDung LIKE N'%' + @tu + N'%')
              AND (@isGv = 0 OR cd.MonHocID IN (SELECT MonHocID FROM GiaoVien_MonHoc WHERE GiaoVienID = @gvId))
              {(chiLocTrung ? @" AND c.NoiDung IN (
                    SELECT c2.NoiDung 
                    FROM CauHoi c2 
                    JOIN ChuDe cd2 ON cd2.ChuDeID = c2.ChuDeID
                    WHERE c2.TrangThai = N'HoatDong'
                      AND (@mon = 0 OR cd2.MonHocID = @mon)
                    GROUP BY c2.NoiDung 
                    HAVING COUNT(*) > 1
                )" : "")}
            ORDER BY {(chiLocTrung ? "c.NoiDung, c.CauHoiID" : "c.CauHoiID DESC")}
            {(page.HasValue ? "OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY" : "")}
            OPTION (RECOMPILE, MAXDOP 1)";
        return Database.Query(sql, p =>
        {
            p.AddWithValue("@mon", monHocId ?? 0);
            p.AddWithValue("@chuDe", chuDeId ?? 0);
            p.AddWithValue("@loai", loai ?? "");
            p.AddWithValue("@muc", mucDo ?? "");
            p.AddWithValue("@tu", tuKhoa?.Trim() ?? "");
            p.AddWithValue("@isGv", Session.IsGiaoVien ? 1 : 0);
            p.AddWithValue("@gvId", Session.GiaoVienID);
            if (page.HasValue)
            {
                p.AddWithValue("@offset", checked(page.Value * pageSize));
                p.AddWithValue("@pageSize", pageSize);
            }
        });
    }

    /// <summary>Lay danh sach cau hoi trong cac mon hoc / chu de de phuc vu loc trung khi import.</summary>
    public static List<(int CauHoiID, int ChuDeID, string TenChuDe, int MonHocID, string NoiDung)> LayCauHoiKiemTraTrung(IEnumerable<int> chuDeIds)
    {
        var ids = chuDeIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0) return [];

        var pNames = string.Join(",", ids.Select((_, i) => $"@cd{i}"));
        var sql = $@"
            SELECT c.CauHoiID, c.ChuDeID, cd.TenChuDe, cd.MonHocID, c.NoiDung 
            FROM CauHoi c 
            JOIN ChuDe cd ON cd.ChuDeID = c.ChuDeID
            WHERE cd.MonHocID IN (SELECT DISTINCT MonHocID FROM ChuDe WHERE ChuDeID IN ({pNames}))";

        return Database.ReadList(sql,
            r => (r.GetInt32(0), r.GetInt32(1), r.GetString(2), r.GetInt32(3), r.GetString(4)),
            p =>
            {
                for (int i = 0; i < ids.Count; i++)
                    p.AddWithValue($"@cd{i}", ids[i]);
            });
    }

    public CauHoi? LayChiTiet(int cauHoiId)
    {
        if (Session.IsGiaoVien)
            KiemTraPhanCongMon(Database.ExecuteScalarInt("SELECT ChuDeID FROM CauHoi WHERE CauHoiID=@id",
                p => p.AddWithValue("@id", cauHoiId)));
        else Session.YeuCauQuanTriVien();
        var cau = Database.ReadList(@"
            SELECT c.CauHoiID, c.ChuDeID, c.NguoiTaoID, c.NoiDung, c.LoaiCauHoi, c.MucDoKho,
                   c.Diem, c.TrangThai, c.NgayTao, c.GiaiThichDapAn, c.NhomTHPT
            FROM CauHoi c WHERE c.CauHoiID = @id",
            r => new CauHoi
            {
                CauHoiID = r.GetInt32(0),
                ChuDeID = r.GetInt32(1),
                NguoiTaoID = r.IsDBNull(2)?0:r.GetInt32(2),
                NoiDung = r.GetString(3),
                LoaiCauHoi = r.GetString(4),
                MucDoKho = r.GetString(5),
                Diem = r.GetDecimal(6),
                TrangThai = r.GetString(7),
                NgayTao = r.GetDateTime(8),
                GiaiThichDapAn = r.IsDBNull(9) ? null : r.GetString(9),
                NhomTHPT = r.GetString(10)
            },
            p => p.AddWithValue("@id", cauHoiId)).FirstOrDefault();

        if (cau == null) return null;

        cau.DapAns = Database.ReadList(
            "SELECT DapAnID, CauHoiID, ThuTu, NoiDung, LaDapAnDung FROM DapAn WHERE CauHoiID = @id ORDER BY ThuTu",
            r => new DapAn
            {
                DapAnID = r.GetInt32(0),
                CauHoiID = r.GetInt32(1),
                ThuTu = r.GetInt32(2),
                NoiDung = r.GetString(3),
                LaDapAnDung = r.GetBoolean(4)
            },
            p => p.AddWithValue("@id", cauHoiId));
        var images = Database.ReadList(@"
            SELECT HinhAnhID, DapAnID, DuongDan, MoTa FROM HinhAnh
            WHERE CauHoiID = @id OR DapAnID IN (SELECT DapAnID FROM DapAn WHERE CauHoiID = @id)
            ORDER BY ThuTu, HinhAnhID",
            r => (AnswerId: r.IsDBNull(1) ? (int?)null : r.GetInt32(1), Image: new QuestionImage
            {
                HinhAnhID = r.GetInt32(0), DuongDan = r.GetString(2),
                MoTa = r.IsDBNull(3) ? null : r.GetString(3)
            }), p => p.AddWithValue("@id", cauHoiId));
        cau.HinhAnhs = images.Where(x => x.AnswerId == null).Select(x => x.Image).ToList();
        foreach (var answer in cau.DapAns)
            answer.HinhAnhs = images.Where(x => x.AnswerId == answer.DapAnID).Select(x => x.Image).ToList();
        return cau;
    }

    // ------------------------------------------------------------------ CRUD
    public int Them(CauHoi c) => Luu(c, themMoi: true);

    public void Sua(CauHoi c) => Luu(c, themMoi: false);

    public static void KiemTraHopLe(CauHoi c)
    {
        if ((string.IsNullOrWhiteSpace(c.NoiDung) && c.HinhAnhs?.Count is not > 0) || c.ChuDeID <= 0 || c.Diem <= 0)
            throw new InvalidOperationException("Hãy nhập chủ đề, nội dung và điểm hợp lệ.");
        var hopLe = c.LoaiCauHoi switch
        {
            LoaiCauHoi.TracNghiem => c.DapAns.Count == 4 && c.DapAns.Count(d => d.LaDapAnDung) == 1,
            LoaiCauHoi.DungSai => c.DapAns.Count == 4,
            LoaiCauHoi.TraLoiNgan => c.DapAns.Count == 1 && c.DapAns[0].LaDapAnDung,
            _ => false
        };
        if (!hopLe || c.DapAns.Any(d => string.IsNullOrWhiteSpace(d.NoiDung) && (c.LoaiCauHoi == LoaiCauHoi.TraLoiNgan || d.HinhAnhs?.Count is not > 0)))
            throw new InvalidOperationException("Đáp án chưa đầy đủ hoặc không hợp lệ.");

    }

    private static void KiemTraPhanCongMon(int chuDeId)
    {
        if (!Session.IsGiaoVien) { Session.YeuCauQuanTriVien(); return; }
        if (!Session.IsAccountActive()) throw new UnauthorizedAccessException("Tài khoản giáo viên không còn hoạt động.");
        var gvId = Session.GiaoVienID;
        var duocPhanCong = Database.ExecuteScalarInt(@"
            SELECT COUNT(1) FROM GiaoVien_MonHoc gm
            JOIN ChuDe cd ON cd.MonHocID = gm.MonHocID
            WHERE gm.GiaoVienID = @gv AND cd.ChuDeID = @cd",
            p =>
            {
                p.AddWithValue("@gv", gvId);
                p.AddWithValue("@cd", chuDeId);
            }) > 0;
        if (!duocPhanCong)
            throw new UnauthorizedAccessException("Bạn không được phân công phụ trách môn học của chủ đề này.");
    }

    private static void KiemTraTacGia(int cauHoiId)
    {
        if (!Session.IsGiaoVien) { Session.YeuCauQuanTriVien(); return; }
        KiemTraPhanCongMon(Database.ExecuteScalarInt("SELECT ChuDeID FROM CauHoi WHERE CauHoiID=@id",
            p => p.AddWithValue("@id", cauHoiId)));
        var authorId = Database.ExecuteScalarInt(
            "SELECT NguoiTaoID FROM CauHoi WHERE CauHoiID = @id",
            p => p.AddWithValue("@id", cauHoiId));
        if (authorId != Session.Current!.TaiKhoanID)
            throw new UnauthorizedAccessException("Bạn chỉ được phép chỉnh sửa hoặc xóa câu hỏi do chính mình tạo.");
    }

    private static int Luu(CauHoi c, bool themMoi)
    {
        KiemTraHopLe(c);
        KiemTraPhanCongMon(c.ChuDeID);
        if (!themMoi) KiemTraTacGia(c.CauHoiID);
        using var files = new QuestionImageStore.Batch();
        using var conn = Database.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);
        QuestionDuplicates.Reject(conn, tx, c);
        if (!themMoi) KiemTraChoPhepThayDoi(conn, tx, c.CauHoiID);
        using var cmd = new SqlCommand(themMoi ? @"
            INSERT INTO CauHoi (ChuDeID, NguoiTaoID, NoiDung, LoaiCauHoi, MucDoKho, Diem, TrangThai, NgayTao, GiaiThichDapAn)
            VALUES (@cd, @author, @nd, @loai, @muc, @diem, N'HoatDong', SYSDATETIME(), @gt);
            SELECT CAST(SCOPE_IDENTITY() AS INT);" : @"
            UPDATE CauHoi SET ChuDeID = @cd, NoiDung = @nd, LoaiCauHoi = @loai,
                MucDoKho = @muc, Diem = @diem, GiaiThichDapAn = @gt WHERE CauHoiID = @id;
            SELECT @id;", conn, tx);
        cmd.Parameters.AddWithValue("@cd", c.ChuDeID);
        cmd.Parameters.AddWithValue("@author", Session.Current!.TaiKhoanID);
        cmd.Parameters.AddWithValue("@nd", c.NoiDung.Trim());
        cmd.Parameters.AddWithValue("@loai", c.LoaiCauHoi);
        cmd.Parameters.AddWithValue("@muc", c.MucDoKho);
        cmd.Parameters.AddWithValue("@diem", c.Diem);
        cmd.Parameters.AddWithValue("@gt", (object?)c.GiaiThichDapAn ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", c.CauHoiID);
        var id = Convert.ToInt32(cmd.ExecuteScalar());
        using (var track=new SqlCommand("UPDATE CauHoi SET NhomTHPT=@track WHERE CauHoiID=@id",conn,tx))
        {
            if(c.NhomTHPT is not ("ChuaPhanLoai" or "Chung" or "KhoaHocMayTinh" or "TinHocUngDung"))throw new InvalidOperationException("Nhánh Tin học không hợp lệ.");
            track.Parameters.AddWithValue("@track",c.NhomTHPT);track.Parameters.AddWithValue("@id",id);track.ExecuteNonQuery();
        }
        LuuHinhAnh(conn, tx, files, id, null, c.HinhAnhs);

        // Giu ID dap an va hinh anh khi sua noi dung; khong xoa du lieu bai lam.
        var ids = new List<int>();
        using (var read = new SqlCommand("SELECT DapAnID FROM DapAn WHERE CauHoiID = @id ORDER BY ThuTu, DapAnID", conn, tx))
        {
            read.Parameters.AddWithValue("@id", id);
            using var reader = read.ExecuteReader();
            while (reader.Read()) ids.Add(reader.GetInt32(0));
        }
        for (var i = 0; i < c.DapAns.Count; i++)
        {
            using var answer = new SqlCommand(i < ids.Count ?
                "UPDATE DapAn SET ThuTu = @t, NoiDung = @nd, LaDapAnDung = @dung WHERE DapAnID = @da; SELECT @da;" :
                "INSERT INTO DapAn (CauHoiID, ThuTu, NoiDung, LaDapAnDung) VALUES (@id, @t, @nd, @dung); SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx);
            answer.Parameters.AddWithValue("@id", id);
            answer.Parameters.AddWithValue("@da", i < ids.Count ? ids[i] : 0);
            answer.Parameters.AddWithValue("@t", i);
            answer.Parameters.AddWithValue("@nd", c.DapAns[i].NoiDung.Trim());
            answer.Parameters.AddWithValue("@dung", c.DapAns[i].LaDapAnDung);
            var answerId = Convert.ToInt32(answer.ExecuteScalar());
            LuuHinhAnh(conn, tx, files, id, answerId, c.DapAns[i].HinhAnhs);
        }
        foreach (var answerId in ids.Skip(c.DapAns.Count))
        {
            if (c.HinhAnhs != null)
                LuuHinhAnh(conn, tx, files, id, answerId, []);
            using var remove = new SqlCommand(@"
                IF EXISTS (SELECT 1 FROM HinhAnh WHERE DapAnID = @da)
                    THROW 50001, N'Đáp án có hình ảnh. Không thể bỏ đáp án này khi đổi loại câu hỏi.', 1;
                DELETE FROM DapAn WHERE DapAnID = @da;", conn, tx);
            remove.Parameters.AddWithValue("@da", answerId);
            remove.ExecuteNonQuery();
        }
        tx.Commit();
        files.Committed = true;
        return id;
    }

    private static void LuuHinhAnh(SqlConnection conn, SqlTransaction tx, QuestionImageStore.Batch files,
        int questionId, int? answerId, List<QuestionImage>? images)
    {
        if (images == null) return; // Caller không chỉnh sửa ảnh: giữ nguyên.
        var existing = new HashSet<int>();
        var where = answerId.HasValue ? "DapAnID = @owner" : "CauHoiID = @owner AND DapAnID IS NULL";
        using (var read = new SqlCommand($"SELECT HinhAnhID FROM HinhAnh WHERE {where}", conn, tx))
        {
            read.Parameters.AddWithValue("@owner", answerId ?? questionId);
            using var reader = read.ExecuteReader();
            while (reader.Read()) existing.Add(reader.GetInt32(0));
        }
        if (images.Where(x => x.HinhAnhID != 0).Any(x => !existing.Contains(x.HinhAnhID)))
            throw new InvalidOperationException("Ảnh không thuộc nội dung đang sửa. Hãy mở lại câu hỏi.");
        foreach (var removed in existing.Except(images.Select(x => x.HinhAnhID)))
        {
            using var delete = new SqlCommand("DELETE FROM HinhAnh WHERE HinhAnhID = @id", conn, tx);
            delete.Parameters.AddWithValue("@id", removed);
            delete.ExecuteNonQuery();
        }
        for (var i = 0; i < images.Count; i++)
        {
            var image = images[i];
            if (image.MoTa?.Length > 200) throw new InvalidOperationException("Chú thích ảnh tối đa 200 ký tự.");
            if (image.HinhAnhID == 0 && image.PendingPng == null)
                throw new InvalidOperationException("Ảnh mới chưa có dữ liệu.");
            using var save = new SqlCommand(image.HinhAnhID == 0 ? @"
                INSERT INTO HinhAnh (CauHoiID, DapAnID, DuongDan, MoTa, ThuTu)
                VALUES (@q, @a, @path, @caption, @order);" : @"
                UPDATE HinhAnh SET MoTa = @caption, ThuTu = @order WHERE HinhAnhID = @id;", conn, tx);
            save.Parameters.AddWithValue("@q", answerId.HasValue ? DBNull.Value : questionId);
            save.Parameters.AddWithValue("@a", (object?)answerId ?? DBNull.Value);
            save.Parameters.AddWithValue("@path", image.HinhAnhID == 0 ? files.Save(image) : image.DuongDan);
            save.Parameters.AddWithValue("@caption", (object?)image.MoTa ?? DBNull.Value);
            save.Parameters.AddWithValue("@order", i);
            save.Parameters.AddWithValue("@id", image.HinhAnhID);
            save.ExecuteNonQuery();
        }
    }

    private static void KiemTraChoPhepThayDoi(SqlConnection conn, SqlTransaction tx, int id)
    {
        using var cmd = new SqlCommand(@"
            SELECT CASE
                WHEN NOT EXISTS (SELECT 1 FROM CauHoi WITH (UPDLOCK, HOLDLOCK) WHERE CauHoiID = @id) THEN -1
                WHEN EXISTS (SELECT 1 FROM ChiTietBaiLam WHERE CauHoiID = @id)
                  OR EXISTS (SELECT 1 FROM DeThi_CauHoi WHERE CauHoiID = @id)
                  OR EXISTS (SELECT 1 FROM MaDe_CauHoi WHERE CauHoiID = @id)
                  OR EXISTS (SELECT 1 FROM MaDe_DapAn WHERE CauHoiID = @id) THEN 1
                ELSE 0 END", conn, tx);
        cmd.Parameters.AddWithValue("@id", id);
        var state = Convert.ToInt32(cmd.ExecuteScalar());
        if (state == -1) throw new InvalidOperationException("Câu hỏi không còn tồn tại. Hãy tải lại danh sách.");
        if (state == 1) throw new InvalidOperationException(
            "Câu hỏi đã được dùng trong đề thi hoặc bài làm, không thể sửa/xóa. Bạn có thể ẩn câu hỏi và thêm câu hỏi mới.");
    }

    public (bool Ok, string Error) Xoa(int cauHoiId)
    {
        KiemTraTacGia(cauHoiId);
        using var conn = Database.Open();
        using var tx = conn.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            KiemTraChoPhepThayDoi(conn, tx, cauHoiId);
            using var cmd = new SqlCommand(@"
                DELETE FROM HinhAnh WHERE CauHoiID = @id OR DapAnID IN (SELECT DapAnID FROM DapAn WHERE CauHoiID = @id);
                DELETE FROM DapAn WHERE CauHoiID = @id;
                DELETE FROM CauHoi WHERE CauHoiID = @id;", conn, tx);
            cmd.Parameters.AddWithValue("@id", cauHoiId);
            cmd.ExecuteNonQuery();
            tx.Commit();
            return (true, "");
        }
        catch (InvalidOperationException ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>An / hien cau hoi (An = khong hien trong kho de web).</summary>
    public void DoiTrangThai(int cauHoiId, bool an)
    {
        KiemTraTacGia(cauHoiId);
        Database.Execute("UPDATE CauHoi SET TrangThai = @t WHERE CauHoiID = @id",
            p => { p.AddWithValue("@t", an ? "An" : "HoatDong"); p.AddWithValue("@id", cauHoiId); });
    }

}
