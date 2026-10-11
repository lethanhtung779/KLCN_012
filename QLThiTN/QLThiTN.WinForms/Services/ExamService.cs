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
        bool xoaCu = true)
    {
        if (soLuongMaDe < 1 || soLuongMaDe > 50)
            return (false, "Số lượng mã đề phải từ 1 đến 50.", new());

        var (coBai, soBai) = KiemTraBaiLamCuaDe(deThiId);
        if (coBai && xoaCu)
            return (false, $"Đề thi đã có {soBai} bài làm của thí sinh, không thể xóa mã đề cũ để trộn lại. Hãy tạo đề thi mới nếu muốn trộn bộ đề khác.", new());

        // Lấy danh sách câu hỏi gốc của đề
        var dtCauHoi = Database.Query(@"
            SELECT dc.CauHoiID, dc.ThuTu AS ThuTuGoc, c.LoaiCauHoi, c.NoiDung, c.Diem
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
                LoaiCauHoi = r["LoaiCauHoi"]?.ToString() ?? "TracNghiem",
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

                // Dùng seed ngẫu nhiên độc lập cho từng mã đề
                var rng = new Random(deThiId * 7919 + (maDeBatDau + k) * 31 + k * 17);

                // Shuffle các câu hỏi trong từng phần độc lập (bảo toàn cấu trúc Phần I -> Phần II -> Phần III)
                var p1Shuffled = phan1.OrderBy(_ => rng.Next()).ToList();
                var p2Shuffled = phan2.OrderBy(_ => rng.Next()).ToList();
                var p3Shuffled = phan3.OrderBy(_ => rng.Next()).ToList();

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
                        // Hoán vị 4 phương án A, B, C, D
                        var daShuffled = q.DapAns.OrderBy(_ => rng.Next()).ToList();
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
                        // Hoán vị 4 ý con a, b, c, d
                        var daShuffled = q.DapAns.OrderBy(_ => rng.Next()).ToList();
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
            SELECT mc.ThuTu AS CauSo, c.LoaiCauHoi, c.NoiDung AS NoiDungCau,
                   md.ThuTu AS ThuTuDapAn, d.NoiDung AS NoiDungDapAn, d.LaDapAnDung
            FROM MaDe_CauHoi mc
            JOIN CauHoi c ON c.CauHoiID = mc.CauHoiID
            JOIN MaDe_DapAn md ON md.MaDeID = mc.MaDeID AND md.CauHoiID = mc.CauHoiID
            JOIN DapAn d ON d.DapAnID = md.DapAnID
            WHERE mc.MaDeID = @m
            ORDER BY mc.ThuTu, md.ThuTu",
            p => p.AddWithValue("@m", maDeId));
    }

    private class CauHoiItem
    {
        public int CauHoiID { get; set; }
        public int ThuTuGoc { get; set; }
        public string LoaiCauHoi { get; set; } = "TracNghiem";
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
