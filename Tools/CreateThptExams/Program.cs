using Microsoft.Data.SqlClient;
using QLThiTN.Shared;
using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Services;

var winformsDir = args.Length > 0 ? Path.GetFullPath(args[0]) : @"D:\KLCN\QLThiTN\QLThiTN\QLThiTN.WinForms";
Database.LoadConfig(winformsDir);

var adminId = Database.ExecuteScalarInt("SELECT TOP 1 TaiKhoanID FROM TaiKhoan WHERE VaiTro=N'QuanTriVien' AND TrangThai=N'HoatDong' ORDER BY TaiKhoanID");
if (adminId == 0) throw new InvalidOperationException("Không tìm thấy quản trị viên.");
Session.Set(new TaiKhoan { TaiKhoanID = adminId, VaiTro = "QuanTriVien", TrangThai = "HoatDong" });
var service = new ExamService();

Console.WriteLine("=== BẮT ĐẦU TẠO 2 ĐỀ THPT QUỐC GIA CHO MỖI MÔN VÀ ĐƯA LÊN WEB ===");

var subjects = Database.ReadList("SELECT MonHocID, TenMon FROM MonHoc ORDER BY MonHocID",
    r => (Id: r.GetInt32(0), Name: r.GetString(1)));

var rng = new Random();

foreach (var sub in subjects)
{
    var format = ExamFormat.ForSubject(sub.Name);
    Console.WriteLine($"\n--- Môn {sub.Id}: {sub.Name} (Cấu trúc: {format.MultipleChoice} TN, {format.TrueFalse} Đ/S, {format.ShortAnswer} TLN, {format.Minutes} phút) ---");

    // Tìm số thứ tự đề lớn nhất hiện có
    var maxDe = Database.ExecuteScalarInt(@"
        SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(TenDe, CHARINDEX('(Đề ', TenDe) + 5, 2) AS INT)), 0)
        FROM DeThi WHERE MonHocID = @m AND TenDe LIKE N'%(Đề %'",
        p => p.AddWithValue("@m", sub.Id));

    // Lấy pool câu hỏi hợp lệ cho môn này
    var poolTN = Database.ReadList(@"
        SELECT c.CauHoiID FROM CauHoi c JOIN ChuDe cd ON cd.ChuDeID=c.ChuDeID
        JOIN DapAn a ON a.CauHoiID=c.CauHoiID
        WHERE cd.MonHocID=@m AND c.LoaiCauHoi='TracNghiem' AND c.TrangThai='HoatDong'
        GROUP BY c.CauHoiID
        HAVING COUNT(*)=4 AND SUM(CASE WHEN a.LaDapAnDung=1 THEN 1 ELSE 0 END)=1",
        r => r.GetInt32(0), p => p.AddWithValue("@m", sub.Id));

    var poolDS = Database.ReadList(@"
        SELECT c.CauHoiID, c.NhomTHPT FROM CauHoi c JOIN ChuDe cd ON cd.ChuDeID=c.ChuDeID
        JOIN DapAn a ON a.CauHoiID=c.CauHoiID
        WHERE cd.MonHocID=@m AND c.LoaiCauHoi='DungSai' AND c.TrangThai='HoatDong'
        GROUP BY c.CauHoiID, c.NhomTHPT
        HAVING COUNT(*)=4",
        r => (Id: r.GetInt32(0), Track: r.GetString(1)), p => p.AddWithValue("@m", sub.Id));

    var poolTLN = Database.ReadList(@"
        SELECT c.CauHoiID FROM CauHoi c JOIN ChuDe cd ON cd.ChuDeID=c.ChuDeID
        JOIN DapAn a ON a.CauHoiID=c.CauHoiID
        WHERE cd.MonHocID=@m AND c.LoaiCauHoi='TraLoiNgan' AND c.TrangThai='HoatDong'
        GROUP BY c.CauHoiID
        HAVING COUNT(*)=1",
        r => r.GetInt32(0), p => p.AddWithValue("@m", sub.Id));

    Console.WriteLine($"  Kho câu hỏi hợp lệ: {poolTN.Count} Trắc nghiệm, {poolDS.Count} Đúng/Sai, {poolTLN.Count} Trả lời ngắn");

    // Trộn ngẫu nhiên các pool
    poolTN = poolTN.OrderBy(_ => rng.Next()).ToList();
    poolTLN = poolTLN.OrderBy(_ => rng.Next()).ToList();

    var usedTN = new HashSet<int>();
    var usedDS = new HashSet<int>();
    var usedTLN = new HashSet<int>();

    for (int k = 1; k <= 2; k++)
    {
        var examNum = maxDe + k;
        var tenDe = $"Đề thi tốt nghiệp THPT 2025 - Môn {sub.Name} (Đề {examNum:D2})";
        var tenDot = $"Kỳ thi thử Tốt nghiệp THPT 2025 - Môn {sub.Name} (Đợt {examNum})";
        var isFree = (k == 1); // Đề 1 miễn phí, Đề 2 dành cho VIP / Học viên trung tâm

        var selectedQuestions = new List<int>();

        // 1. Lấy Trắc nghiệm
        var pickTN = poolTN.Where(id => !usedTN.Contains(id)).Take(format.MultipleChoice).ToList();
        if (pickTN.Count < format.MultipleChoice)
        {
            var extra = poolTN.Where(id => !pickTN.Contains(id)).Take(format.MultipleChoice - pickTN.Count).ToList();
            pickTN.AddRange(extra);
        }
        foreach (var id in pickTN) usedTN.Add(id);
        selectedQuestions.AddRange(pickTN);

        // 2. Lấy Đúng/Sai
        if (sub.Name.Contains("Tin", StringComparison.OrdinalIgnoreCase))
        {
            var chung = poolDS.Where(x => x.Track == "Chung" && !usedDS.Contains(x.Id)).Take(2).ToList();
            if (chung.Count < 2) chung = poolDS.Where(x => x.Track == "Chung").Take(2).ToList();
            var khmt = poolDS.Where(x => x.Track == "KhoaHocMayTinh" && !usedDS.Contains(x.Id)).Take(2).ToList();
            if (khmt.Count < 2) khmt = poolDS.Where(x => x.Track == "KhoaHocMayTinh").Take(2).ToList();

            foreach (var q in chung.Concat(khmt))
            {
                usedDS.Add(q.Id);
                selectedQuestions.Add(q.Id);
            }
        }
        else if (format.TrueFalse > 0)
        {
            var pickDS = poolDS.Where(x => !usedDS.Contains(x.Id)).Take(format.TrueFalse).ToList();
            if (pickDS.Count < format.TrueFalse)
            {
                var extra = poolDS.Where(x => !pickDS.Any(p => p.Id == x.Id)).Take(format.TrueFalse - pickDS.Count).ToList();
                pickDS.AddRange(extra);
            }
            foreach (var q in pickDS)
            {
                usedDS.Add(q.Id);
                selectedQuestions.Add(q.Id);
            }
        }

        // 3. Lấy Trả lời ngắn
        if (format.ShortAnswer > 0)
        {
            var pickTLN = poolTLN.Where(id => !usedTLN.Contains(id)).Take(format.ShortAnswer).ToList();
            if (pickTLN.Count < format.ShortAnswer)
            {
                var extra = poolTLN.Where(id => !pickTLN.Contains(id)).Take(format.ShortAnswer - pickTLN.Count).ToList();
                pickTLN.AddRange(extra);
            }
            foreach (var id in pickTLN) usedTLN.Add(id);
            selectedQuestions.AddRange(pickTLN);
        }

        if (selectedQuestions.Count != format.MultipleChoice + format.TrueFalse + format.ShortAnswer)
            throw new InvalidOperationException($"Không đủ câu hỏi để tạo đề {tenDe}.");

        // Tạo đề trong DB
        int deThiId;
        using (var conn = Database.Open())
        using (var tx = conn.BeginTransaction())
        {
            using var cmd = new SqlCommand(@"
                INSERT INTO DeThi (MonHocID, TenDe, LoaiDe, SoLuongCauHoi, HinhThucTao, LaMienPhi, DaDuyet, NguoiTaoID)
                VALUES (@mon, @ten, N'ThiThu', @soCau, N'MaTranTHPTQG', @free, 0, @creator);
                SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, tx);
            cmd.Parameters.AddWithValue("@mon", sub.Id);
            cmd.Parameters.AddWithValue("@ten", tenDe);
            cmd.Parameters.AddWithValue("@soCau", selectedQuestions.Count);
            cmd.Parameters.AddWithValue("@free", isFree);
            cmd.Parameters.AddWithValue("@creator", adminId);
            deThiId = Convert.ToInt32(cmd.ExecuteScalar());

            using var insQ = new SqlCommand(@"
                INSERT INTO DeThi_CauHoi (DeThiID, CauHoiID, ThuTu)
                VALUES (@de, @cau, @thutu)", conn, tx);
            insQ.Parameters.Add("@de", System.Data.SqlDbType.Int);
            insQ.Parameters.Add("@cau", System.Data.SqlDbType.Int);
            insQ.Parameters.Add("@thutu", System.Data.SqlDbType.Int);

            for (int i = 0; i < selectedQuestions.Count; i++)
            {
                insQ.Parameters["@de"].Value = deThiId;
                insQ.Parameters["@cau"].Value = selectedQuestions[i];
                insQ.Parameters["@thutu"].Value = i + 1;
                insQ.ExecuteNonQuery();
            }
            tx.Commit();
        }

        // Trộn mã đề
        var tronResult = service.TronDeThi(deThiId, 4, 101, true, false);
        if (!tronResult.Success)
            throw new InvalidOperationException($"Không trộn được đề #{deThiId}: {tronResult.Message}");

        // Kiểm tra và Duyệt đề
        using (var conn = Database.Open())
        {
            var report = ExamReview.Read(conn, deThiId);
            var validationError = ExamReview.GetApprovalValidationError(report);
            if (validationError != null)
                throw new InvalidOperationException($"Đề #{deThiId} chưa hợp lệ để duyệt: {validationError}");

            ExamReview.Approve(conn, deThiId, adminId, report.Fingerprint);
        }

        // Tạo Đợt thi mở trên web (SoLanThiToiDa = 0: không giới hạn)
        Database.Execute(@"
            INSERT INTO DotThi (DeThiID, TenDotThi, ThoiGianMoCong, ThoiGianDongCong, ThoiLuongLamBai, GioiHanSoLuong, CongBoDiemSom, TrangThai, PhamVi, SoLanThiToiDa)
            VALUES (@de, @ten, @mo, @dong, @tl, NULL, 1, N'DangMo', N'ToanTruong', 0)",
            p => {
                p.AddWithValue("@de", deThiId);
                p.AddWithValue("@ten", tenDot);
                p.AddWithValue("@mo", DateTime.Now.AddDays(-1));
                p.AddWithValue("@dong", DateTime.Now.AddYears(1));
                p.AddWithValue("@tl", format.Minutes);
            });

        var freeLabel = isFree ? "Miễn phí" : "VIP";
        Console.WriteLine($"  [OK] Tạo thành công Đề #{deThiId}: {tenDe} [{freeLabel}] - Đã duyệt & Mở đợt thi ({format.Minutes} phút, 4 mã đề)");
    }
}

// Duyệt và mở các đề chuẩn THPT cũ nếu có
Console.WriteLine("\n=== DUYỆT VÀ MỞ THÊM CÁC ĐỀ CŨ ĐẠT CHUẨN TRÊN WEB ===");
var existingExams = Database.ReadList(@"
    SELECT d.DeThiID, d.TenDe, m.TenMon, d.DaDuyet,
           (SELECT COUNT(*) FROM DotThi dt WHERE dt.DeThiID=d.DeThiID AND dt.TrangThai=N'DangMo') AS SoDotDangMo
    FROM DeThi d JOIN MonHoc m ON m.MonHocID=d.MonHocID
    WHERE d.HinhThucTao=N'MaTranTHPTQG' AND (d.DaDuyet=0 OR (SELECT COUNT(*) FROM DotThi dt WHERE dt.DeThiID=d.DeThiID AND dt.TrangThai=N'DangMo')=0)",
    r => (Id: r.GetInt32(0), Name: r.GetString(1), Subject: r.GetString(2), Approved: r.GetBoolean(3), OpenSessions: r.GetInt32(4)));

foreach (var ex in existingExams)
{
    try
    {
        using var conn = Database.Open();
        var report = ExamReview.Read(conn, ex.Id);
        var err = ExamReview.GetApprovalValidationError(report);
        if (err == null)
        {
            if (!ex.Approved)
            {
                ExamReview.Approve(conn, ex.Id, adminId, report.Fingerprint);
                Console.WriteLine($"  Đã duyệt đề cũ #{ex.Id}: {ex.Name}");
            }

            if (ex.OpenSessions == 0)
            {
                var format = ExamFormat.ForSubject(ex.Subject);
                var hasDot = Database.ExecuteScalarInt("SELECT COUNT(*) FROM DotThi WHERE DeThiID=@de", p => p.AddWithValue("@de", ex.Id)) > 0;
                if (hasDot)
                {
                    Database.Execute("UPDATE DotThi SET TrangThai=N'DangMo', ThoiGianMoCong=DATEADD(day, -1, SYSDATETIME()), ThoiGianDongCong=DATEADD(year, 1, SYSDATETIME()), SoLanThiToiDa=0 WHERE DeThiID=@de",
                        p => p.AddWithValue("@de", ex.Id));
                }
                else
                {
                    Database.Execute(@"
                        INSERT INTO DotThi (DeThiID, TenDotThi, ThoiGianMoCong, ThoiGianDongCong, ThoiLuongLamBai, GioiHanSoLuong, CongBoDiemSom, TrangThai, PhamVi, SoLanThiToiDa)
                        VALUES (@de, @ten, @mo, @dong, @tl, NULL, 1, N'DangMo', N'ToanTruong', 0)",
                        p => {
                            p.AddWithValue("@de", ex.Id);
                            p.AddWithValue("@ten", "Kỳ thi thử THPT Quốc gia - " + ex.Name);
                            p.AddWithValue("@mo", DateTime.Now.AddDays(-1));
                            p.AddWithValue("@dong", DateTime.Now.AddYears(1));
                            p.AddWithValue("@tl", format.Minutes);
                        });
                }
                Console.WriteLine($"  Đã mở đợt thi cho đề #{ex.Id}: {ex.Name}");
            }
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"  Bỏ qua đề #{ex.Id} ({ex.Name}): {e.Message}");
    }
}

Console.WriteLine("\n=== HOÀN TẤT TOÀN BỘ 100%! ===");
