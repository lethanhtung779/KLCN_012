using Microsoft.Data.SqlClient;
using QLThiTN.Shared;
using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Services;

if (args.Length != 1)
    throw new ArgumentException("Truyền thư mục chứa appsettings.json của WinForms.");

Database.LoadConfig(Path.GetFullPath(args[0]));
var adminId = Database.ExecuteScalarInt("SELECT TOP 1 TaiKhoanID FROM TaiKhoan WHERE VaiTro=N'QuanTriVien' AND TrangThai=N'HoatDong' ORDER BY TaiKhoanID");
if (adminId == 0) throw new InvalidOperationException("Không tìm thấy quản trị viên đang hoạt động.");
Session.Set(new TaiKhoan { TaiKhoanID = adminId, VaiTro = "QuanTriVien", TrangThai = "HoatDong" });
var service = new ExamService();

EnsureTinHocTracks();
var exams = LoadStandardExams();
var repaired = new List<(int OldId, int CurrentId, string Name)>();

foreach (var exam in exams)
{
    var format = ExamFormat.ForSubject(exam.Subject);
    var types = LoadTypes(exam.Id);
    if (format.GetValidationError(types) == null)
        continue;

    var questionIds = BuildQuestionSet(exam, format);
    var currentId = exam.Attempts == 0
        ? ReplaceInPlace(exam, questionIds)
        : ArchiveAndCreateReplacement(exam, questionIds);

    repaired.Add((exam.Id, currentId, exam.Name));
    Console.WriteLine($"REPAIRED #{exam.Id} -> #{currentId}: {exam.Name}");
}

foreach (var exam in LoadStandardExams())
{
    using var conn = Database.Open();
    var report = ExamReview.Read(conn, exam.Id);
    var error = ExamReview.GetApprovalValidationError(report);
    if (error != null)
        throw new InvalidOperationException($"Đề #{exam.Id} vẫn chưa hợp lệ: {error}");
    Console.WriteLine($"VALID #{exam.Id}: {exam.Subject} - {report.Variants.Count} mã đề");
}

Console.WriteLine($"Đã sửa {repaired.Count} đề. Tất cả đề còn mang hình thức MaTranTHPTQG đều hợp lệ.");

List<ExamInfo> LoadStandardExams() => Database.ReadList(@"
    SELECT d.DeThiID,d.TenDe,d.MonHocID,m.TenMon,d.LoaiDe,d.LaMienPhi,d.NguoiTaoID,
           (SELECT COUNT(*) FROM MaDeThi md WHERE md.DeThiID=d.DeThiID) SoMa,
           (SELECT COUNT(*) FROM DotThi dt JOIN BaiLam b ON b.DotThiID=dt.DotThiID WHERE dt.DeThiID=d.DeThiID) SoBai
    FROM DeThi d JOIN MonHoc m ON m.MonHocID=d.MonHocID
    WHERE d.HinhThucTao=N'MaTranTHPTQG' ORDER BY d.DeThiID",
    r => new ExamInfo(r.GetInt32(0), r.GetString(1), r.GetInt32(2), r.GetString(3), r.GetString(4),
        r.GetBoolean(5), r.GetInt32(6), r.GetInt32(7), r.GetInt32(8)));

List<string> LoadTypes(int examId) => Database.ReadList(@"
    SELECT c.LoaiCauHoi FROM DeThi_CauHoi dc JOIN CauHoi c ON c.CauHoiID=dc.CauHoiID
    WHERE dc.DeThiID=@id ORDER BY dc.ThuTu", r => r.GetString(0), p => p.AddWithValue("@id", examId));

List<int> BuildQuestionSet(ExamInfo exam, ExamFormat format)
{
    var result = new List<int>();
    result.AddRange(Pick(exam, "TracNghiem", format.MultipleChoice, null));
    if (exam.Subject.Contains("Tin", StringComparison.OrdinalIgnoreCase))
    {
        result.AddRange(Pick(exam, "DungSai", 2, "Chung"));
        result.AddRange(Pick(exam, "DungSai", 2, "KhoaHocMayTinh"));
    }
    else
    {
        result.AddRange(Pick(exam, "DungSai", format.TrueFalse, null));
    }
    result.AddRange(Pick(exam, "TraLoiNgan", format.ShortAnswer, null));
    if (result.Count != format.MultipleChoice + format.TrueFalse + format.ShortAnswer || result.Count != result.Distinct().Count())
        throw new InvalidOperationException($"Không thể tạo bộ câu hỏi hợp lệ cho môn {exam.Subject}.");
    return result;
}

List<int> Pick(ExamInfo exam, string type, int count, string? track)
{
    if (count == 0) return [];
    var rows = Database.ReadList(@"
        SELECT c.CauHoiID
        FROM CauHoi c JOIN ChuDe cd ON cd.ChuDeID=c.ChuDeID
        JOIN DapAn a ON a.CauHoiID=c.CauHoiID
        LEFT JOIN DeThi_CauHoi currentExam ON currentExam.DeThiID=@exam AND currentExam.CauHoiID=c.CauHoiID
        WHERE cd.MonHocID=@mon AND c.TrangThai=N'HoatDong' AND c.LoaiCauHoi=@type
          AND (@track=N'' OR c.NhomTHPT=@track)
        GROUP BY c.CauHoiID,currentExam.CauHoiID
        HAVING (@type=N'TracNghiem' AND COUNT(*)=4 AND SUM(CASE WHEN a.LaDapAnDung=1 THEN 1 ELSE 0 END)=1)
            OR (@type=N'DungSai' AND COUNT(*)=4)
            OR (@type=N'TraLoiNgan' AND COUNT(*)=1)
        ORDER BY CASE WHEN currentExam.CauHoiID IS NULL THEN 1 ELSE 0 END,c.CauHoiID",
        r => r.GetInt32(0), p =>
        {
            p.AddWithValue("@mon", exam.SubjectId);
            p.AddWithValue("@type", type);
            p.AddWithValue("@track", track ?? "");
            p.AddWithValue("@exam", exam.Id);
        });
    if (rows.Count < count)
        throw new InvalidOperationException($"Môn {exam.Subject} thiếu câu {type}{(track == null ? "" : " / " + track)} hợp lệ: cần {count}, có {rows.Count}.");
    return rows.Take(count).ToList();
}

int ReplaceInPlace(ExamInfo exam, IReadOnlyList<int> questionIds)
{
    using var conn = Database.Open();
    using var tx = conn.BeginTransaction();
    Execute(conn, tx, "DELETE FROM MaDe_DapAn WHERE MaDeID IN (SELECT MaDeID FROM MaDeThi WHERE DeThiID=@id)", ("@id", exam.Id));
    Execute(conn, tx, "DELETE FROM MaDe_CauHoi WHERE MaDeID IN (SELECT MaDeID FROM MaDeThi WHERE DeThiID=@id)", ("@id", exam.Id));
    Execute(conn, tx, "DELETE FROM MaDeThi WHERE DeThiID=@id", ("@id", exam.Id));
    Execute(conn, tx, "DELETE FROM DeThi_CauHoi WHERE DeThiID=@id", ("@id", exam.Id));
    InsertQuestions(conn, tx, exam.Id, questionIds);
    Execute(conn, tx, "UPDATE DeThi SET SoLuongCauHoi=@count,DaDuyet=0,NguoiDuyetID=NULL,NgayDuyet=NULL WHERE DeThiID=@id",
        ("@count", questionIds.Count), ("@id", exam.Id));
    tx.Commit();
    Shuffle(exam.Id, exam.Variants);
    return exam.Id;
}

int ArchiveAndCreateReplacement(ExamInfo exam, IReadOnlyList<int> questionIds)
{
    using var conn = Database.Open();
    using var tx = conn.BeginTransaction();
    Execute(conn, tx, @"UPDATE DeThi SET HinhThucTao=N'ThuCong',
        TenDe=CASE WHEN CHARINDEX(N'[Lưu lịch sử]',TenDe)>0 THEN TenDe ELSE LEFT(TenDe,180)+N' [Lưu lịch sử]' END
        WHERE DeThiID=@id", ("@id", exam.Id));

    using var insert = new SqlCommand(@"
        INSERT DeThi(MonHocID,TenDe,LoaiDe,SoLuongCauHoi,HinhThucTao,LaMienPhi,DaDuyet,NguoiTaoID,NguoiDuyetID,NgayDuyet)
        OUTPUT inserted.DeThiID
        VALUES(@mon,@name,@type,@count,N'MaTranTHPTQG',@free,0,@creator,NULL,NULL)", conn, tx);
    insert.Parameters.AddWithValue("@mon", exam.SubjectId);
    insert.Parameters.AddWithValue("@name", exam.Name);
    insert.Parameters.AddWithValue("@type", exam.ExamType);
    insert.Parameters.AddWithValue("@count", questionIds.Count);
    insert.Parameters.AddWithValue("@free", exam.Free);
    insert.Parameters.AddWithValue("@creator", exam.CreatorId);
    var newId = Convert.ToInt32(insert.ExecuteScalar());
    InsertQuestions(conn, tx, newId, questionIds);
    tx.Commit();

    Shuffle(newId, Math.Max(1, exam.Variants));
    CloneOpenSchedules(exam.Id, newId);
    return newId;
}

void Shuffle(int examId, int variants)
{
    var result = service.TronDeThi(examId, Math.Max(1, variants), 101, true, false);
    if (!result.Success) throw new InvalidOperationException($"Không trộn được đề #{examId}: {result.Message}");
}

void CloneOpenSchedules(int oldExamId, int newExamId)
{
    using var conn = Database.Open();
    using var tx = conn.BeginTransaction();
    var schedules = new List<int>();
    using (var read = new SqlCommand(@"SELECT DotThiID FROM DotThi WHERE DeThiID=@id AND TrangThai<>N'DaDong' AND ThoiGianDongCong>SYSDATETIME()", conn, tx))
    {
        read.Parameters.AddWithValue("@id", oldExamId);
        using var reader = read.ExecuteReader();
        while (reader.Read()) schedules.Add(reader.GetInt32(0));
    }
    foreach (var oldScheduleId in schedules)
    {
        using var insert = new SqlCommand(@"
            INSERT DotThi(DeThiID,TenDotThi,ThoiGianMoCong,ThoiGianDongCong,ThoiLuongLamBai,GioiHanSoLuong,CongBoDiemSom,TrangThai,PhamVi,SoLanThiToiDa)
            OUTPUT inserted.DotThiID
            SELECT @newExam,TenDotThi,ThoiGianMoCong,ThoiGianDongCong,ThoiLuongLamBai,GioiHanSoLuong,CongBoDiemSom,TrangThai,PhamVi,SoLanThiToiDa
            FROM DotThi WHERE DotThiID=@old", conn, tx);
        insert.Parameters.AddWithValue("@newExam", newExamId);
        insert.Parameters.AddWithValue("@old", oldScheduleId);
        var newScheduleId = Convert.ToInt32(insert.ExecuteScalar());
        Execute(conn, tx, @"INSERT DangKyDotThi(HocVienID,DotThiID,NgayDangKy,TrangThai)
            SELECT HocVienID,@new,NgayDangKy,TrangThai FROM DangKyDotThi WHERE DotThiID=@old",
            ("@new", newScheduleId), ("@old", oldScheduleId));
        Execute(conn, tx, @"INSERT DotThi_LopHoc(DotThiID,LopHocID)
            SELECT @new,LopHocID FROM DotThi_LopHoc WHERE DotThiID=@old",
            ("@new", newScheduleId), ("@old", oldScheduleId));
        Execute(conn, tx, "UPDATE DotThi SET TrangThai=N'DaDong' WHERE DotThiID=@old", ("@old", oldScheduleId));
    }
    tx.Commit();
}

void EnsureTinHocTracks()
{
    var valid = Database.ReadList(@"
        SELECT c.CauHoiID,c.NhomTHPT
        FROM CauHoi c JOIN ChuDe cd ON cd.ChuDeID=c.ChuDeID JOIN MonHoc m ON m.MonHocID=cd.MonHocID
        JOIN DapAn a ON a.CauHoiID=c.CauHoiID
        LEFT JOIN DeThi_CauHoi oldQuestion ON oldQuestion.DeThiID=93 AND oldQuestion.CauHoiID=c.CauHoiID
        WHERE m.TenMon=N'Tin học' AND c.LoaiCauHoi=N'DungSai' AND c.TrangThai=N'HoatDong'
        GROUP BY c.CauHoiID,c.NhomTHPT,oldQuestion.ThuTu
        HAVING COUNT(*)=4
        ORDER BY CASE WHEN oldQuestion.ThuTu IS NULL THEN 1 ELSE 0 END,oldQuestion.ThuTu,c.CauHoiID",
        r => (Id: r.GetInt32(0), Track: r.GetString(1)));
    Assign("Chung", Math.Max(0, 2 - valid.Count(x => x.Track == "Chung")));
    Assign("KhoaHocMayTinh", Math.Max(0, 2 - valid.Count(x => x.Track == "KhoaHocMayTinh")));

    void Assign(string track, int needed)
    {
        foreach (var row in valid.Where(x => x.Track == "ChuaPhanLoai").Take(needed).ToList())
        {
            Database.Execute("UPDATE CauHoi SET NhomTHPT=@track WHERE CauHoiID=@id", p =>
            {
                p.AddWithValue("@track", track);
                p.AddWithValue("@id", row.Id);
            });
            var index = valid.FindIndex(x => x.Id == row.Id);
            valid[index] = (row.Id, track);
        }
    }
}

static void InsertQuestions(SqlConnection conn, SqlTransaction tx, int examId, IReadOnlyList<int> ids)
{
    using var cmd = new SqlCommand("INSERT DeThi_CauHoi(DeThiID,CauHoiID,ThuTu) VALUES(@exam,@question,@order)", conn, tx);
    cmd.Parameters.Add("@exam", System.Data.SqlDbType.Int);
    cmd.Parameters.Add("@question", System.Data.SqlDbType.Int);
    cmd.Parameters.Add("@order", System.Data.SqlDbType.Int);
    for (var index = 0; index < ids.Count; index++)
    {
        cmd.Parameters["@exam"].Value = examId;
        cmd.Parameters["@question"].Value = ids[index];
        cmd.Parameters["@order"].Value = index + 1;
        cmd.ExecuteNonQuery();
    }
}

static int Execute(SqlConnection conn, SqlTransaction tx, string sql, params (string Name, object Value)[] values)
{
    using var cmd = new SqlCommand(sql, conn, tx);
    foreach (var value in values) cmd.Parameters.AddWithValue(value.Name, value.Value);
    return cmd.ExecuteNonQuery();
}

internal sealed record ExamInfo(int Id, string Name, int SubjectId, string Subject, string ExamType,
    bool Free, int CreatorId, int Variants, int Attempts);
